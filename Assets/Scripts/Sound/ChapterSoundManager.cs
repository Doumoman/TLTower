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
    private int baseTick = 0;

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

    private IEnumerator PlayOnceAfterTicks(string clipName, int delayTicks)
    {
        int targetTick = tickCount + delayTicks;
        while (tickCount < targetTick)
            yield return null;

        TickPlay(clipName, "Bgm");
    }
    public void NextState()
    {
        var stageData = soundStateDB.stageData[(int)currentStage];
        bool isFinalStage = currentStage == SoundStateData.Stage.Count - 1;
        bool isLastStateInStage = currentStateIndex + 1 >= stageData.States.Count;

        if (isFinalStage && isLastStateInStage)
        {
            int finalEndTick = tickCount;

            foreach (var loop in loopedSounds)
            {
                if (tickCount >= loop.delay)
                {
                    int last = loop.delay;
                    while (last + loop.cycle <= tickCount)
                        last += loop.cycle;

                    finalEndTick = Mathf.Max(finalEndTick, last + loop.cycle);
                }
            }

            loopedSounds.Clear();
            PlayLooped("space_highlight", "Bgm", 1, finalEndTick);
            return;
        }
        else if (isLastStateInStage)
        {
            var nextStage = currentStage + 1;
            if (nextStage < SoundStateData.Stage.Count)
            {
                Transition(nextStage, 0);  // <- 여기에만 Transition 사용
            }
        }
        else
        {
            Transition(currentStage, currentStateIndex + 1); // <- 여기에도 사용!
        }
    }

    private void Transition(SoundStateData.Stage stage, int nextStateIndex)
    {
        Debug.Log($"[Transition] Transitioning to Stage: {stage}, StateIndex: {nextStateIndex}");

        // tickCount 초기화
        tickCount = 0;

        // 현재 스테이지와 인덱스 갱신
        currentStage = stage;
        currentStateIndex = nextStateIndex;

        // 기존 루프 제거
        loopedSounds.Clear();

        // 다음 state에 있는 클립 예약
        var nextState = soundStateDB.stageData[(int)stage].States[nextStateIndex];
        foreach (var clip in nextState.clipDataList)
        {
            if (!string.IsNullOrEmpty(clip.clipName))
            {
                PlayLooped(clip.clipName, "Bgm", clip.cycle, clip.delay);
            }
        }

        // ambience 처리
        string newAmbience = GetAmbienceFromState(nextState);
        if (!string.IsNullOrEmpty(newAmbience))
        {
            PlayAmbienceIfChanged(newAmbience);
        }
    }

    private string GetAmbienceFromState(SoundStateData.State state)
    {
        foreach (var clip in state.clipDataList)
        {
            if (clip.cycle == 0) // ambience는 cycle이 0이라고 가정
                return clip.clipName;
        }
        return null;
    }
    public void NextState(string expectedNextStage)
    {
        var stageData = soundStateDB.stageData[(int)currentStage];

        // 다음 State가 없으면 Stage 전환
        if (currentStateIndex + 1 >= stageData.States.Count)
        {
            int nextStageIndex = ((int)currentStage + 1) % ((int)SoundStateData.Stage.Count - 1);
            string actualNextStageName = ((SoundStateData.Stage)nextStageIndex).ToString();

            if (actualNextStageName != expectedNextStage)
                return;

            SetStage((SoundStateData.Stage)nextStageIndex);
            return;
        }

        // 다음 State는 같은 Stage 내에 있으므로, Stage는 변하지 않음
        if (currentStage.ToString() != expectedNextStage)
            return;

        NextState(); // 기존 함수 호출
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
