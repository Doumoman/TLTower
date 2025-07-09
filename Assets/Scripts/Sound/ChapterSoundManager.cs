using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChapterSoundManager : MonoBehaviour
{
    [SerializeField] private SoundStateDB soundStateDB;
    [SerializeField] private SoundStateData.Stage currentStage = SoundStateData.Stage.Ground;
    [SerializeField] private int currentStateIndex = 0;
    [SerializeField] private float ambienceFadeDuration = 4f;

    private List<QueuedSound> playQueue = new();
    private List<LoopedSound> loopedSounds = new();
    private int tickCount = 0;

    public static ChapterSoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetStage(currentStage);
        if (TickManager.Instance != null)
            TickManager.Instance.OnTickEvent += OnTick;
        else
            StartCoroutine(WaitForTickManager());
    }

    private IEnumerator WaitForTickManager()
    {
        while (TickManager.Instance == null) yield return null;
        TickManager.Instance.OnTickEvent += OnTick;
    }

    private void OnDisable()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnTickEvent -= OnTick;
    }

    private void OnTick(object sender, EventArgs e)
    {
        tickCount++;

        foreach (var loop in loopedSounds)
        {
            if (loop.cycle <= 0) continue;
            if (tickCount >= loop.delay && (tickCount - loop.delay) % loop.cycle == 0)
                TickPlay(loop.clipName, "Bgm");
        }

        foreach (var sound in playQueue)
            SoundManager.Instance.Play(sound.name, sound.channel);

        playQueue.Clear();
    }

    public void TickPlay(string name, string channel)
    {
        if (Enum.TryParse(channel, true, out Sound parsedChannel))
            playQueue.Add(new QueuedSound { name = name, channel = parsedChannel });
        else
            Debug.LogWarning($"Invalid sound channel: {channel}");
    }

    public void PlayLooped(string name, string channel, int cycle, int delay)
    {
        if (cycle <= 0)
        {
            PlayAmbienceIfChanged(name);
            return;
        }

        if (Enum.TryParse(channel, true, out Sound parsedChannel))
        {
            loopedSounds.Add(new LoopedSound
            {
                clipName = name,
                cycle = cycle,
                delay = delay
            });
        }
        else Debug.LogWarning($"Invalid sound channel: {channel}");
    }

    public void PlayAmbienceIfChanged(string newClipName)
    {
        if (string.IsNullOrEmpty(newClipName)) return;

        string currentClip = SoundManager.Instance.CurrentAmbience;
        if (currentClip == newClipName) return;

        int previousIndex = SoundManager.Instance.CurrentAmbienceIndex;

        SoundManager.Instance.FadeInAmbience(newClipName, ambienceFadeDuration);
    }

    public void LoadLoopedFromDB(SoundStateData.Stage stage, int stateIndex)
    {
        if (soundStateDB == null) { Debug.LogError("SoundStateDB is null"); return; }
        if (stateIndex >= soundStateDB.stageData[(int)stage].States.Count) { Debug.LogError("Invalid State Index"); return; }

        var clips = soundStateDB.stageData[(int)stage].States[stateIndex].clipDataList;
        foreach (var clip in clips)
        {
            if (!string.IsNullOrEmpty(clip.clipName))
                PlayLooped(clip.clipName, "Bgm", clip.cycle, clip.delay);
        }
    }

    public void SetStage(SoundStateData.Stage stage)
    {
        currentStage = stage;
        currentStateIndex = 0;
        loopedSounds.Clear();
        LoadLoopedFromDB(currentStage, currentStateIndex);
    }

    public void NextState()
    {
        var stageData = soundStateDB.stageData[(int)currentStage];
        if (currentStateIndex + 1 >= stageData.States.Count)
        {
            int nextStageIndex = ((int)currentStage + 1) % ((int)SoundStateData.Stage.Count - 1);
            SetStage((SoundStateData.Stage)nextStageIndex);
            return;
        }

        var prevState = stageData.States[currentStateIndex];
        var nextState = stageData.States[currentStateIndex + 1];

        var prevClips = new HashSet<string>();
        foreach (var clip in prevState.clipDataList) prevClips.Add(clip.clipName);

        var nextClips = new HashSet<string>();
        foreach (var clip in nextState.clipDataList) nextClips.Add(clip.clipName);

        loopedSounds.RemoveAll(loop => !nextClips.Contains(loop.clipName));

        int latestTick = tickCount;
        foreach (var loop in loopedSounds)
        {
            int last = loop.delay;
            while (last + loop.cycle <= tickCount)
                last += loop.cycle;
            latestTick = Mathf.Max(latestTick, last + loop.cycle);
        }

        foreach (var newClip in nextState.clipDataList)
        {
            if (!prevClips.Contains(newClip.clipName))
            {
                // Ambience는 여기서 제외하고 HandleAmbienceTransition에서 따로 다루도록
                if (newClip.cycle <= 0) continue;

                PlayLooped(newClip.clipName, "Bgm", newClip.cycle, latestTick);
            }
        }
        var newAmbience = nextState.clipDataList.Find(c => c.cycle <= 0);
        if (newAmbience != null)
        {
            PlayAmbienceIfChanged(newAmbience.clipName);
        }

        currentStateIndex++;
    }

    private class QueuedSound
    {
        public string name;
        public Sound channel;
    }

    private class LoopedSound
    {
        public string clipName;
        public int cycle;
        public int delay;
    }
}
