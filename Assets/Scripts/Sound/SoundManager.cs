using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FMODUnity;
using FMOD.Studio;
using System.Linq;

public class SoundManager : Singleton<SoundManager>
{
    public void Play(string str)
    {
        BGM = RuntimeManager.CreateInstance(str);

        BGM.start();
    }

    public void TickPlay(string path)
    {
        // 이벤트 핸들러 등록
        void OnTickHandler(object sender, EventArgs e)
        {
            // 재생
            Play(path);

            // 이벤트 핸들러 제거 (한 번만 실행되도록)
            TickManager.Instance.OnTickEvent -= OnTickHandler;
        }

        // 다음 tick에 실행
        TickManager.Instance.OnTickEvent += OnTickHandler;
    }
    public void PlaySFX(string path)
    {
        RuntimeManager.PlayOneShot(path);
    }
    public void PlayVoice(string path)
    {
        StartCoroutine(QueueVoice(path));
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

        Voice = RuntimeManager.CreateInstance(path);
        Voice.start();
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
    private string current = "";

    public void PlayBGM(string name, int state)
    {
        //이미 재생중이면 parameter만 바꾸기
        if (BGM.isValid() && current == name)
        {
            string paramName = name + "State";
            BGM.setParameterByName(name, state);
            return;
        }

        //기존 BGM 정지
        if (BGM.isValid())
        {
            BGM.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            BGM.release();
        }

        //새 BGM 재생
        string path = $"event:/{name}";
        TickPlay(path);

        string paramName2 = name + "State";
        BGM.setParameterByName(paramName2, state);
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
}