using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FMODUnity;
using FMOD.Studio;
using System.Linq;
using UnityEngine.Playables;

public class SoundManager : Singleton<SoundManager>
{
    protected override void Awake()
    {
        base.Awake();
        StopBGM();
        StopPauseBGM();
    }
    public void Play(string str, int state)
    {
        string path = $"event:/BGM/{str}";
        BGM = RuntimeManager.CreateInstance(path);
        BGM.start();

        string paramName2 = "parameter:/" + str + "State";
        var result = BGM.setParameterByName(paramName2, state);
        Debug.Log($"Parameter {paramName2} set to {state}, {path} {result}!!");
    }

    public void TickPlay(string path, int state)
    {
        // 이벤트 핸들러 등록
        void OnTickHandler(object sender, EventArgs e)
        {
            // 재생
            Play(path, state);

            // 이벤트 핸들러 제거 (한 번만 실행되도록)
            TickManager.Instance.OnTickEvent -= OnTickHandler;
        }

        // 다음 tick에 실행
        TickManager.Instance.OnTickEvent += OnTickHandler;
    }
    public void PlaySFX(string path)
    {
        var instance = RuntimeManager.CreateInstance("event:/SFX/" + path);
        instance.setVolume(PlayerPrefs.GetFloat("sfxVolume"));
        instance.start();
        instance.release();
        Debug.Log(path + " Playing!");
    }
    public void PlayVoice(string path)
    {
        voiceQueue.Enqueue(path);
        StartCoroutine(QueueVoice());
    }

    private Dictionary<string, EventInstance> loopedSFX = new Dictionary<string, EventInstance>();
    public void PlayLoop(string path)
    {
        if (loopedSFX.ContainsKey(path))
        {
            return;
        }
        EventInstance instance = RuntimeManager.CreateInstance(path);
        instance.start();
        Debug.Log(path + " Playing!");
        loopedSFX[path] = instance;
    }

    public void StopLoop(string path)
    {
        if (!loopedSFX.ContainsKey(path)) return;
        EventInstance instance = loopedSFX[path];
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
        loopedSFX.Remove(path);
    }

    private Queue<string> voiceQueue = new Queue<string>();
    private bool isVoicePlaying = false;
    private IEnumerator QueueVoice()
    {
        isVoicePlaying = true;

        while (voiceQueue.Count > 0)
        {
            string nextPath = "event:/Voice/" + voiceQueue.Dequeue();
            EventInstance instance = RuntimeManager.CreateInstance(nextPath);
            instance.start();
            Debug.Log($"playing Voice {nextPath}");

            PLAYBACK_STATE state;
            do
            {
                instance.getPlaybackState(out state);
                yield return null;
            }
            while (state != PLAYBACK_STATE.STOPPED);

            instance.release();
        }

        isVoicePlaying = false;
    }

    private EventInstance BGM;
    private EventInstance Pause;
    private string current = "";

    public void PlayBGM(string name, int state)
    {
        //이미 재생중이면 parameter만 바꾸기
        if (BGM.isValid() && current == name)
        {
            string paramName = "parameter:/" + name + "State";
            // parameter:/SpringState
            var result = BGM.setParameterByName(paramName, state);
            Debug.Log($"Parameter {paramName} set to {state}, {result}!!");
            return;
        }

        //기존 BGM 정지
        if (BGM.isValid())
        {
            BGM.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            BGM.release();
        }

        //새 BGM 재생
        TickPlay(name,state);
        current = name;
    }

    public void StopBGM()
    {
        if (BGM.isValid())
        {
            BGM.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            BGM.release();
            BGM.clearHandle();
            current = "";
        }
    }

    public void PauseBGM()
    {
        BGM.setPaused(true);
        /*string path = "event:/pause";
        Pause = RuntimeManager.CreateInstance(path);
        Pause.start();*/
        Debug.Log($"BGM {BGM} Paused");
    }

    public void Resume()
    {
        //Pause.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        BGM.setPaused(false);
        Debug.Log($"BGM {BGM} Resumed");
    }

    public void PlayPauseBGM()
    {
        if (Pause.isValid()) return;
        string path = "event:/pause";
        Pause = RuntimeManager.CreateInstance(path);
        Pause.start();
        Debug.Log($"Pause BGM Playing!");
    }

    public void StopPauseBGM()
    {
        if (!Pause.isValid()) return;
        Pause.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        Pause.release();
        Debug.Log($"Pause BGM Stopped!");
    }
}