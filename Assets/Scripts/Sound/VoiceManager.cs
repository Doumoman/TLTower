using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
public class VoiceManager : Singleton<VoiceManager>
{
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        StartCoroutine(CheckIdleAndSpeak());
    }
    /*------------------------ 보살매니저에서 긴빠이쳐온 말하기 기능 ---------------------------
    텍스트 -> 대사 대신 대사 -> 텍스트로 종속관계 변경
    */
    private bool dontSpeakTwice = false;
    private bool isSpeaking = false; //지금 말하고 있는지
    private bool isStopped = false; //forcestop 1번만 재생용.
    public bool forceStop = false; //Guide에서 대사 강제 일시정지 (재생중인 대사까지 모두 일시정지)
    public bool pauseVoice = false; //Pause시 큐잉된 대사 재생 중단
    public void Speak(string script, int idx, bool del)
    {
        if (dontSpeakTwice)
        {
            if (!del) dontSpeakTwice = false;
            Debug.Log($"보살 대사 \"{script}\" 취소됨");
            return;
        }
        if (del) dontSpeakTwice = true;

        int useIdx = (idx < 0)
        ? ScriptDataLoader.Instance.GetNext(script)
        : idx;

        PlayVoice(script, useIdx);
    }

    /*------------------------ 보이스 큐잉 및 출력 ---------------------------*/
    private Queue<KeyValuePair<string, int>> voiceQueue = new();
    EventInstance instance;
    PLAYBACK_STATE state;
    private bool tempBool = false;
    public void PlayVoice(string path, int idx) //보이스 단일 출력 시 사용
    {
        var voiceData = new KeyValuePair<string, int>(path, idx == -1 ? ScriptDataLoader.Instance.GetNext(path) : idx);
        Debug.Log($"Current Cue : {voiceQueue.Count}, Enqueue Voice {voiceData.Key}{voiceData.Value}");
        voiceQueue.Enqueue(voiceData);
    }
    void Update()
    {
        if (forceStop) //PlayGuide에서 true, HandleEsc에서 false
        {
            if (!isStopped)
            {
                instance.setPaused(true);
                isStopped = true;
                if(forceStop) Debug.Log("forceStop true! Voice Paused");
            }
            return;
        }
        else if (isStopped)
        {
            instance.setPaused(false);
            isStopped = false;
            Debug.Log("forceStop false!");
            Debug.Log($"forceStop == {forceStop}, pauceVoice = {pauseVoice}, isSpeaking == {isSpeaking}");
        }
        if (!isSpeaking && voiceQueue.Count > 0 && !pauseVoice)
        {
            isSpeaking = true;
            var pair = voiceQueue.Dequeue();
            string path = $"event:/Voice/{pair.Key}{pair.Value}";
            instance = RuntimeManager.CreateInstance(path);
            instance.start();
            BosalManager.Instance.SpitText(pair.Key, pair.Value); //코루틴을 이용한 텍스트 출력
            Debug.Log($"Playing Voice {path}");
            tempBool = true;
        }

        if (instance.handle != null)
        {
            instance.getPlaybackState(out state);
            if (state == PLAYBACK_STATE.STOPPED && !forceStop) //forceStop 상태를 제외한 모든 소리 종료 상태에서
            {
                if (tempBool == true)
                {
                    instance.release();
                    Debug.Log($"Voice Instance{instance.handle} released");
                    instance = default;
                    isSpeaking = false;
                    tempBool = false;
                }
            } //이것저것 초기화
        }
    }
    /*------------------------ 무대사 감지 ---------------------------*/
    [SerializeField] private float IdleChecker = 30f;
    private IEnumerator CheckIdleAndSpeak()
    {
        float t = 0f;

        while (true)
        {
            if (!isSpeaking) //Guide나 Pause panel이 열린 상태에서는 어차피 시간 0이므로 forceStop, pauseVoice와는 관계 X
            {
                t += Time.deltaTime;
                if (t >= IdleChecker)
                {
                    string script = ChapterManager.Instance.idleScript;
                    if (script != null && script != "")
                        BosalManager.Instance.Speak(script);
                    t = 0f;
                }
            }
            else t = 0f;
            yield return null;
        }
    }
}
