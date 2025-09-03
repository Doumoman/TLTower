using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Credits : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private int nameSize = 30;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private int roleSize = 15;

    [Header("타이밍")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float quickFadeDuration = 0.2f; // 터치 시 빠르게 넘기기
    [SerializeField] private float waitDuration = 2f;

    [Header("데이터")]
    [SerializeField] private List<string> names = new List<string>();
    [SerializeField] private List<string> roles = new List<string>();

    private KeyValuePair<string, string>[] credits = new KeyValuePair<string, string>[0];

    private int currentCreditIndex = 0;
    private bool fadingIn = false;
    private bool fadingOut = false;
    private bool skippable = false;

    // 마스터/스텝 코루틴 핸들
    private Coroutine sequenceCo = null;
    private Coroutine stepCo = null;
    private Coroutine skipCo = null;
    public bool end = false;

    void Start()
    {
        nameText.text = "";
        nameText.fontSize = nameSize;
        roleText.text = "";
        roleText.fontSize = roleSize;

        // 투명으로 초기화
        SetAlpha(nameText, 0f);
        SetAlpha(roleText, 0f);

        currentCreditIndex = 0;

        // credits 배열 초기화
        int count = Mathf.Min(names.Count, roles.Count);
        credits = new KeyValuePair<string, string>[count];
        for (int i = 0; i < count; i++)
        {
            credits[i] = new KeyValuePair<string, string>(names[i], roles[i]);
        }
    }

    // 외부에서 호출
    public void Play()
    {
        skippable = true;
        if (sequenceCo != null)
            StopCoroutine(sequenceCo);

        sequenceCo = StartCoroutine(Sequence());
    }

    // 마스터 시퀀스: 각 항목을 순차 실행
    IEnumerator Sequence()
    {
        currentCreditIndex = 0;

        while (currentCreditIndex < credits.Length)
        {
            if (skipRequested) { skipRequested = false; continue; }

            var pair = credits[currentCreditIndex];
            stepCo = StartCoroutine(ShowAndFade(pair.Key, pair.Value));
            yield return stepCo;
            stepCo = null;
            currentCreditIndex++;

            yield return WaitNextTick();
            if (skipRequested) { skipRequested = false; }
        }
        end = true;
    }

    private IEnumerator ShowAndFade(string key, string value)
    {
        nameText.text = key;
        roleText.text = value;

        // 동시에 페이드인
        yield return FadeBoth(FadeIn(fadeDuration, nameText),
                              FadeIn(fadeDuration, roleText));

        // 대기
        yield return WaitCoroutine(waitDuration);

        // 동시에 페이드아웃
        yield return FadeBoth(FadeOut(fadeDuration, nameText),
                              FadeOut(fadeDuration, roleText));
    }

    private IEnumerator WaitCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    private IEnumerator FadeIn(float duration, TextMeshProUGUI text)
    {
        Color original = text.color;
        Color target = new Color(original.r, original.g, original.b, 1f);
        float elapsed = 0f;

        fadingIn = true;
        while (elapsed < duration)
        {
            text.color = Color.Lerp(original, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        text.color = target;
        fadingIn = false;
    }

    private IEnumerator FadeOut(float duration, TextMeshProUGUI text)
    {
        Color original = text.color;
        Color target = new Color(original.r, original.g, original.b, 0f);
        float elapsed = 0f;

        fadingOut = true;
        while (elapsed < duration)
        {
            text.color = Color.Lerp(original, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        text.color = target;
        fadingOut = false;
    }

    private void SetAlpha(TextMeshProUGUI text, float a)
    {
        var c = text.color;
        c.a = a;
        text.color = c;
    }

    /*
     Skip
      - text가 없을 때(둘 다 알파 0): QuickFadeIn -> Wait -> FadeOut
      - 둘 다 불투명(알파 1): QuickFadeOut
      - FadeIn 중: FadeIn 중단, 즉시 알파 1로 세팅 -> Wait -> FadeOut
      - FadeOut 중: FadeOut 중단, 즉시 알파 0으로 세팅 -> (다음 틱에서 그냥 FadeIn)
     */

    private bool skipRequested = false; // 스킵 요청 신호
    public void Skip()
    {
        if (!skippable) return; // 스킵 불가 상태면 무시
        skipRequested = true;

        if (stepCo != null)
        {
            StopCoroutine(stepCo);
            stepCo = null;
        }

        if (skipCo != null)
        {
            StopCoroutine(skipCo);
        }

        skipCo = StartCoroutine(HandleSkip());
    }
    private IEnumerator HandleSkip()
    {
        int tickAtSkip = TickManager.Instance.tickCount;
        float nameA = nameText.color.a;
        float roleA = roleText.color.a;
        bool bothClear = nameA <= 0.001f && roleA <= 0.001f;
        bool bothOpaque = nameA >= 0.999f && roleA >= 0.999f;

        if (bothClear)
        {
            // text가 없을 때 : QuickFadeIn -> Wait -> 정상 FadeOut

            if (end) yield break; // 이미 끝났으면 무시
            var pair = SafeCurrentPair(); // 현재 인덱스가 범위를 벗어났을 수 있으니 안전 접근
            if (pair.HasValue)
            {
                nameText.text = pair.Value.Key;
                roleText.text = pair.Value.Value;

                yield return FadeBoth(FadeIn(quickFadeDuration, nameText),
                              FadeIn(quickFadeDuration, roleText));

                yield return WaitCoroutine(waitDuration);

                yield return FadeBoth(FadeOut(fadeDuration, nameText),
                              FadeOut(fadeDuration, roleText));
            }
        }
        else if (bothOpaque)
        {
            // 둘 다 1: 빠른 페이드 아웃
            yield return FadeBoth(FadeOut(quickFadeDuration, nameText),
                              FadeOut(quickFadeDuration, roleText));
        }
        else if (fadingIn)
        {
            // FadeIn 중 : 즉시 1로 고정 -> Wait -> 정상 FadeOut
            SetAlpha(nameText, 1f);
            SetAlpha(roleText, 1f);

            yield return WaitCoroutine(waitDuration);

            yield return FadeOut(fadeDuration, nameText);
            yield return FadeOut(fadeDuration, roleText);
            fadingIn = false; // 상태 정리
        }
        else if (fadingOut)
        {
            // FadeOut 중 : 즉시 0으로 고정 (다음 틱에서 그냥 FadeIn)
            SetAlpha(nameText, 0f);
            SetAlpha(roleText, 0f);
            fadingOut = false;
        }

        if (currentCreditIndex < credits.Length - 1)
            currentCreditIndex++; // 다음 항목으로 넘어감
        else end = true;
        skipRequested = false;
        if (tickAtSkip == TickManager.Instance.tickCount)
            yield return WaitNextTick(); // 다음 틱까지 대기
        skipCo = null;
    }

    // 현재 인덱스가 범위 내면 KeyValuePair 반환
    private KeyValuePair<string, string>? SafeCurrentPair()
    {
        if (currentCreditIndex >= 0 && currentCreditIndex < credits.Length)
            return credits[currentCreditIndex];
        return null;
    }

    private IEnumerator WaitNextTick()
    {
        int start = TickManager.Instance.tickCount;           // 현재까지 온 틱 스냅샷
        while (!skipRequested && TickManager.Instance.tickCount == start)
            yield return null;             // 다음 틱 오거나 스킵될 때까지 대기
    }

    private IEnumerator FadeBoth(IEnumerator a, IEnumerator b)
    {
        bool aDone = false, bDone = false;

        StartCoroutine(Wrap(a, () => aDone = true));
        StartCoroutine(Wrap(b, () => bDone = true));

        yield return new WaitUntil(() => aDone && bDone);
    }

    private IEnumerator Wrap(IEnumerator routine, System.Action onDone)
    {
        yield return routine;
        onDone?.Invoke();
    }
}
