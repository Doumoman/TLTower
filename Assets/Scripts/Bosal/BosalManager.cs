using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;
using UnityEngine;
using System;

public class BosalManager : Singleton<BosalManager>
{
    [Header("text")]
    TextMeshProUGUI bosalText;

    [Header("properties")]


    /*[SerializeField] private int NoInputTime = 15; // 15초 동안 무입력이면 무입력 대사 출력
    [SerializeField] private int NoSpeakTime = 30; // 무대사면 30초마다 무입력 대사 출력*/
    [SerializeField] private int waitTicks = 3; // 보살 말하는 시간
    [SerializeField] private float fadeSpeed = 0.5f;
    [SerializeField] private float FontSize = 50;
    public bool NoIdle = true; // 무입력, 무대사 대사 끄기 (로비, 우주 연출 등)

    private bool DontSpeakTwice = false; // 다음 대사 출력하지 않음
    /*private float lastInputTime;
    private float lastSpeaktime;
    private bool NoScriptOnce = false;*/
    public List<System.Action> Actions = new(); // Tick에 등록할 액션들
    void Start()
    {
        bosalText = GetComponentInChildren<TextMeshProUGUI>();
        bosalText.fontSize = FontSize;
        bosalText.text = "";
        bosalText.color = Color.white;
        bosalText.alignment = TextAlignmentOptions.MidlineLeft;

        Color c = bosalText.color;
        c.a = 0f;
        bosalText.color = c;

        TickManager.Instance.OnTickEvent += (sender, eventArgs) =>
        {
            foreach (var action in Actions)
                action.Invoke();

            Actions.Clear();
        }; // Tick에 액션 등록 후 실행
    }
    IEnumerator FadeIn()
    {
        Debug.Log("FadeIn Start");
        while (bosalText.color.a < 1f)
        {
            Color c = bosalText.color;
            c.a += fadeSpeed * Time.deltaTime;
            bosalText.color = c;
            yield return null;
        }
        Debug.Log("FadeIn End");
    }

    IEnumerator FadeOut()
    {
        Debug.Log("FadeOut Start");
        while (bosalText.color.a > 0f)
        {
            Color c = bosalText.color;
            c.a -= fadeSpeed * Time.deltaTime;
            bosalText.color = c;
            yield return null;
        }
        StopAllCoroutines(); // 대사 중지
        bosalText.text = ""; // 대사 내용 초기화
        Debug.Log("FadeOut End");
    }

    IEnumerator WaitUntilFadeOut(int ticks)
    {
        yield return TickManager.Instance.TickWait(ticks);
        StartCoroutine(FadeOut());
    }

    public void ChangeFontSize(float times)
    {
        bosalText.fontSize = times * FontSize;
    }
    public void Speak(string script, int idx = -1, bool del = false)
    {
        // 외부 요인으로 장애물이 소환되는 경우 외부 요인 대사가 먼저이므로
        // bl = true로 뒤 대사를 취소
        // ex) 장마 + 비 내리기 = 장마 대사만 출력

        if (DontSpeakTwice)
        {
            if (!del) DontSpeakTwice = false;
            Debug.Log($"보살 대사 \"{script}\" 취소됨");
            return;
        }

        if (del) DontSpeakTwice = true;

        string selectScript; //출력할 대사

        //idx가 있다면 scriptMap을 직접 탐색
        if (idx > 0) selectScript = ScriptDataLoader.Instance.FindData(script, idx);

        //아니면 대사를 scriptIdx 순서대로 출력 (google sheet 참고)
        else selectScript = ScriptDataLoader.Instance.GetNext(script);

        //커스텀 대사
        if (script == "") selectScript = script;

        StartCoroutine(FadeIn());

        bosalText.text = selectScript;

        StartCoroutine(WaitUntilFadeOut(waitTicks)); // 대사 유지

        //SoundManager.Instance.Play("test", Sound.Bgm);
        Debug.Log("보살 대사: " + selectScript);
    }

    public void ManualSpeakStop()
    {
        bosalText.text = "";
        StartCoroutine(FadeOut());
    }

    /*
    NoInputTime 동안 Input이 없으면 무입력 상황 처리
    무대사면 NoSpeakTime마다 무입력 상황인지 확인
    무입력 상황이면 무입력 대사 출력
    아니면 무대사 대사 출력

    public void OnClick() { lastInputTime = Time.time; }
    public void OnSpeak()
    {
        lastSpeaktime = Time.time;
        NoScriptOnce = false; // 없다면 Update에서 계속 호출
    }

    void Update()
    {
        float now = Time.time;

        bool noInput = now - lastInputTime >= NoInputTime;
        bool noScript = now - lastSpeaktime >= NoSpeakTime;

        if (noScript && !NoScriptOnce && !NoIdle)
        {
            NoScriptOnce = true;
            if (noInput)
            {
                Speak("ZeroState");
                Debug.Log("무입력");
            }
            else
            {
                Speak("idle");
                Debug.Log("무대사");
            }
        }
    }
    */
}
