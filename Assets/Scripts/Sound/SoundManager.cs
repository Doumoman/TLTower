using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FMODUnity;
using FMOD.Studio;
using System.Linq;

public class SoundManager : Singleton<SoundManager>
{
    protected override void Awake()
    {
        base.Awake();
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
        StartCoroutine(QueueVoice("event:/Voice/" + path));
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
    private bool isVoicePlaying = false;
    private EventInstance Voice;
    private IEnumerator QueueVoice(string path)
    {
        while (isVoicePlaying) yield return null;
        StartCoroutine(WaitAndSpeak(path));
        Debug.Log($"보살 음성 : {path}");
        isVoicePlaying = true;

        PLAYBACK_STATE state;
        do
        {
            Voice.getPlaybackState(out state);
            yield return null;
        } while (state != PLAYBACK_STATE.STOPPED);

        Voice.release();
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
    [SerializeField] private float speakTerm = 1f;
    IEnumerator WaitAndSpeak(string path)
    {
        yield return new WaitForSeconds(speakTerm);
        Voice = RuntimeManager.CreateInstance(path);
        Voice.start();
    }
}