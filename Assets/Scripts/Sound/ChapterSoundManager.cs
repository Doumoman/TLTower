using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class ChapterSoundManager : MonoBehaviour
{
    [SerializeField] private SoundStateDB soundStateDB;
    [SerializeField] private SoundStateData.Stage currentStage = SoundStateData.Stage.Ground;
    [SerializeField] private int currentStateIndex = 0;
    [SerializeField] private float ambienceFadeDuration = 4f;
    private string currentAmbienceClip = null;

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
    private List<QueuedSound> playQueue = new();
    private List<LoopedSound> loopedSounds = new();
    private int tickCount = 0;

    private void OnEnable()
    {
        TickManager.Instance.OnTickEvent += OnTick;
    }
    private void OnDisable()
    {
        TickManager.Instance.OnTickEvent -= OnTick;
    }
    private void OnTick(object sender, EventArgs e)
    {
        tickCount++;
        foreach (var loop in loopedSounds)
            if (tickCount - loop.delay >= 0 && (tickCount - loop.delay) % loop.cycle == 0)
                TickPlay(loop.clipName, "Bgm");
        foreach (var sound in playQueue)
            SoundManager.Instance.Play(sound.name, sound.channel);

        playQueue.Clear();
    }
    public void TickPlay(string name, string channel)
    {
        if (Enum.TryParse(channel, true, out Sound parsedChannel))
            playQueue.Add(new QueuedSound { name = name, channel = parsedChannel });
        else Debug.Log($"no sound channel: {channel}");
    }

    public void PlayLooped(string name, string channel, int c, int d)
    {
        if (Enum.TryParse(channel, true, out Sound parsedChannel))
            loopedSounds.Add(new LoopedSound
            {
                clipName = name,
                cycle = c,
                delay = d
            });
        else Debug.Log($"no sound channel: {channel}");
    }

    public void LoadLoopedFromDB(SoundStateData.Stage stage, int stateIndex)
    {
        if (soundStateDB == null)
        {
            Debug.Log("no DB");
            return;
        }
        var clipDataList = soundStateDB.stageData[(int)stage].States[stateIndex].clipDataList;
        foreach (var clipData in clipDataList)
            if (!string.IsNullOrEmpty(clipData.clipName))
                PlayLooped(clipData.clipName, "Bgm", clipData.cycle, clipData.delay);
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
            int nextStageIndex = ((int)currentStage + 1) % ((int)SoundStateData.Stage.Count - 1); // Count 제외
            currentStage = (SoundStateData.Stage)nextStageIndex;
            currentStateIndex = 0;
            loopedSounds.Clear();
            LoadLoopedFromDB(currentStage, currentStateIndex);
            return;
        }

        var prevState = stageData.States[currentStateIndex];
        var nextState = stageData.States[currentStateIndex + 1];

        // 클립 이름 기준으로 set 생성
        var prevClips = new HashSet<string>();
        foreach (var clip in prevState.clipDataList)
            prevClips.Add(clip.clipName);

        var nextClips = new HashSet<string>();
        foreach (var clip in nextState.clipDataList)
            nextClips.Add(clip.clipName);

        // 1. 이전 state에 없는 클립은 제거
        loopedSounds.RemoveAll(loop => !nextClips.Contains(loop.clipName));

        // 2. 다음 state에 새롭게 추가된 클립은 예약 (현재 틱 기준으로 예약)
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
                // 새로 들어온 클립만 추가 (가장 긴 클립이 끝난 뒤에 시작)
                PlayLooped(newClip.clipName, "Bgm", newClip.cycle, latestTick);
            }
        }

        string prevAmb = null, nextAmb = null;
        foreach (var clip in prevState.clipDataList)
            if (clip.cycle == 0) prevAmb = clip.clipName;
        foreach (var clip in nextState.clipDataList)
            if (clip.cycle == 0) nextAmb = clip.clipName;
        if (!string.IsNullOrEmpty(nextAmb) && nextAmb != currentAmbienceClip)
        {
            if (!string.IsNullOrEmpty(currentAmbienceClip)) SoundManager.Instance.FadeOutAmbience(ambienceFadeDuration);

            SoundManager.Instance.FadeInAmbience(nextAmb, ambienceFadeDuration);
            currentAmbienceClip = nextAmb;
        }
        currentStateIndex++;
    }
}
