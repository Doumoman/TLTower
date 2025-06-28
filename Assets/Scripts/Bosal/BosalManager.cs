using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;
using UnityEngine;

public class BosalManager : MonoBehaviour
{
    TextMeshProUGUI bosalText;

    [Header("properties")]
    [SerializeField] private int waitTicks = 3; // 보살 말하는 시간
    [SerializeField] private float fadeSpeed = 0.5f;

    public List<System.Action> Actions = new(); // Tick에 등록할 액션들
    void Start()
    {
        bosalText = GetComponentInChildren<TextMeshProUGUI>();
        bosalText.text = "";
        bosalText.fontSize = 30;
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

    // Update is called once per frame
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

    public void Speak(string script, int ticks = -1)
    {
        StopAllCoroutines(); // 이전 대사 중지
        bosalText.text = script;
        StartCoroutine(FadeIn());
        if (ticks < 0) ticks = waitTicks; // 기본 대기 시간 설정
        StartCoroutine(WaitUntilFadeOut(ticks));
        Debug.Log("보살 대사: " + script);
    }
    public void SpeakFromData(string str, int idx, int ticks = -1)
    {
        string selectScript = ScriptDataLoader.Instance.FindScriptData(str, idx);
        StartCoroutine(FadeIn());
        bosalText.text = selectScript;
        if (ticks < 0) ticks = waitTicks; // 기본 대기 시간 설정
        StartCoroutine(WaitUntilFadeOut(ticks));
        Debug.Log("보살 대사: " + selectScript);
    }
    public void ManualSpeak(string script)
    {
        StartCoroutine(FadeIn());
        bosalText.text = script;
        Debug.Log("보살 대사: " + script);
    }
    public void ManualSpeakFromData(string situation, int idx)
    {
        string selectScript = ScriptDataLoader.Instance.FindScriptData(situation, idx);
        bosalText.text = selectScript;
        Debug.Log("보살 대사: " + selectScript);
        StartCoroutine(FadeIn());
    }

    public void ManualSpeakStop()
    {
        bosalText.text = "";
        StartCoroutine(FadeOut());
    }
}
