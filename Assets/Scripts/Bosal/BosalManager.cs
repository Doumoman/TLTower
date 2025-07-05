using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;
using UnityEngine;
using System;

public class BosalManager : MonoBehaviour
{
    //싱글톤 패턴
    public static BosalManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool DontSpeakTwice = false; // 다음 대사 출력하지 않음
    private int InputTimer = 0; // 무입력 시간 카운트
    [SerializeField] private int NoInputTime = 15; // 15초 동안 무입력이면 무입력 대사 출력
    private bool NoInput = false;
    [SerializeField] private int NoSpeakTime = 30; // 무대사면 30초마다 무입력 대사 출력


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

    public void Speak(string script, bool bl = false, int ticks = -1)
    {
        if (bl)
        {
            DontSpeakTwice = true;
        }
        if (DontSpeakTwice)
        {
            DontSpeakTwice = false;
            return;
        }
        StopAllCoroutines(); // 이전 대사 중지
        bosalText.text = script;
        StartCoroutine(FadeIn());
        if (ticks < 0) ticks = waitTicks; // 기본 대기 시간 설정
        StartCoroutine(WaitUntilFadeOut(ticks));
        Debug.Log("보살 대사: " + script);
    }
    public void SpeakFromData(string str, int idx, bool bl = false, int ticks = -1)
    {
        if (bl) DontSpeakTwice = true;
        if (DontSpeakTwice)
        {
            DontSpeakTwice = false;
            return;
        }
        string selectScript = ScriptDataLoader.Instance.FindScriptData(str, idx);
        StartCoroutine(FadeIn());
        bosalText.text = selectScript;
        if (ticks < 0) ticks = waitTicks; // 기본 대기 시간 설정
        StartCoroutine(WaitUntilFadeOut(ticks));
        Debug.Log("보살 대사: " + selectScript);
    }
    public void ManualSpeak(string script, bool bl = false)
    {
        if (bl) DontSpeakTwice = true;
        if (DontSpeakTwice)
        {
            DontSpeakTwice = false;
            return;
        }
        StartCoroutine(FadeIn());
        bosalText.text = script;
        Debug.Log("보살 대사: " + script);
    }
    public void ManualSpeakFromData(string situation, int idx, bool bl = false)
    {
        if (bl) DontSpeakTwice = true;
        if (DontSpeakTwice)
        {
            DontSpeakTwice = false;
            return;
        }
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

    /*
    무대사면 NoSpeakTime마다 무입력 상황인지 확인
    무입력 상황이면 무입력 대사 출력
    아니면 무대사 대사 출력
    */
    private void OnInputReceived()
    {
        InputTimer = 0;
        NoInput = false;
    }

    private int idleIndex = 0;
    private int zeroIndex = 0;
    private int birdIndex = 0;
    private int windIndex = 0;
    private int heavyRainIndex = 0;

    private int quitIndex = 0;
    private int UDIndex = 0;
    private int birdSpeakCount = 0;
    private int birdStoneIndex = 0;
    private int birdPoopIndex = 0;
    private int birdPeaceIndex = 0;
    [DoNotSerialize] public int rainIndex = 0;
    [SerializeField] private float birdSpeakChance = 0.5f;
    [SerializeField] private int birdSpeakInterval = 2;

    public IEnumerator CheckNoSpeak()
    {
        while (true)
        {
            yield return new WaitForSeconds(NoSpeakTime);
            if (NoInput)
            {
                SpeakFromData("idle", idleIndex % 3 + 1);
                idleIndex++;
            }
            else
            {
                SpeakFromData("ZeroState", zeroIndex % 3 + 1);
                zeroIndex++;
            }
        }
    }

    public void BirdSpeak()
    {
        if (birdSpeakCount < birdSpeakInterval)
        {
            float rand = UnityEngine.Random.value;
            if (rand > birdSpeakChance)
            {
                SpeakFromData("Bird", birdIndex % 3 + 1);
                birdIndex++;
                birdSpeakCount++;
            }
        }
        else
        {
            SpeakFromData("Bird", birdIndex % 3 + 1);
            birdIndex++;
            birdSpeakCount = 0;
        }
    }
    public void BirdStoneSpeak()
    {
        SpeakFromData("BirdStone", birdStoneIndex % 3 + 1);
        birdStoneIndex++;
    }

    public void BirdPoopSpeak()
    {
        SpeakFromData("BirdPoop", birdPoopIndex % 3 + 1);
        birdPoopIndex++;
    }

    public void BirdPeaceSpeak()
    {
        SpeakFromData("BirdPeace", birdPeaceIndex % 3 + 1);
        birdPeaceIndex++;
    }

    public void WindSpeak()
    {
        SpeakFromData("WindStart", windIndex % 3 + 1);
        windIndex++;
    }

    public void HeavyRainSpeak()
    {
        SpeakFromData("HeavyRain", heavyRainIndex % 3 + 1);
        heavyRainIndex++;
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            OnInputReceived();
        }
    }
}
