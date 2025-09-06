using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpaceAnimationSequence : MonoBehaviour
{
    [Header("Stage References")]
    public RiseStage riseStage;
    public TransitionStage transitionStage;

    [Header("Screen Fade-In")]
    public float delay = 1f;
    public SpriteRenderer blackScreen;   // 전체 화면을 덮는 검은 SpriteRenderer
    public float fadeInTime = 1f;        // 알파 1 → 0 으로 줄이는 시간

    [Header("Particles")]
    [SerializeField] GameObject particleRoot;

    [Header("Bosal Speak Timing")]
    [Tooltip("보살 대사를 몇 번 말할지")]
    [SerializeField] int speakRepeat = 7;
    [Header("Fade Targets (Inspector에서 할당)")]
    [Tooltip("4번째 동작 시 가장 먼저 사라질 스프라이트")]
    [SerializeField] SpriteRenderer firstFadeSprite;

    [Tooltip("firstFadeSprite 다음에 순차적으로 사라질 스프라이트들")]
    [SerializeField] List<SpriteRenderer> subsequentFadeSprites = new List<SpriteRenderer>();
    [SerializeField] List<ParticleSystem> fadeParticleSystems = new();
    [SerializeField] float firstFadeDuration = 4f;
    [SerializeField] float subsequentFadeDuration = 6f;

    [Tooltip("대사 간격(초). TickManager.Tick을 기준으로 가장 가까운 틱 수로 변환됨")]
    [SerializeField] float speakIntervalSeconds = 8f;
    [Tooltip("크레딧 호출하기.")]
    [SerializeField] Credits credits;
    [SerializeField] int creditsDelay = 3;
    void Start()
    {
        // 필요하면 자동 Play
        //StartCoroutine(Play());
        StopAllCoroutines();
        StartCoroutine(Play());
    }
    public void PlaySequence()
    {
        StopAllCoroutines();
        StartCoroutine(Play());  // ① 상승 → ② 글로우 → ③ 전환 순으로 진행
    }

    public IEnumerator Play()
    {
        PlayerPrefs.SetInt("CurrentChapter", (int)chapter.space);
        Coroutine fadeCo = StartCoroutine(FadeInFromBlack());
        Coroutine riseCo = StartCoroutine(riseStage.Run());

        // ② 둘 다 끝날 때까지 대기
        yield return fadeCo;   // Fade-in 끝날 때까지
        yield return riseCo;   // riseStage.Run() 끝날 때까지
                               // (이미 끝났다면 즉시 통과)

        // ③ 이후 단계 계속
        //spaceParticles?.Play();
        yield return transitionStage.Run();

        if (particleRoot)
            ActivateWithParents(particleRoot);


        for (int i = 0; i < 7; i++)
        {
            BosalManager.Instance.Speak("SpaceEnding");
        }
        yield return new WaitForSeconds(27f);
        yield return StartCoroutine(FadeSpritesSequence());
        yield return new WaitForSeconds(5f); //대충 5초쯤 기다려 놓고
        yield return TickManager.Instance.TickWait(creditsDelay);
        SoundManager.Instance.StopBGM();
        credits.Play();
    }
    IEnumerator EndCheck()
    {
        while (!credits.end)
        {
            yield return null;
        }
        EndSpace.Instance.EndOfSpace();
    }
    IEnumerator FadeInFromBlack()
    {
        yield return new WaitForSeconds(delay);
        if (!blackScreen) yield break;

        // 안전하게 알파 1로 초기화
        Color c = blackScreen.color;
        c.a = 1f;
        blackScreen.color = c;

        for (float t = 0; t < fadeInTime; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(1f, 0f, t / fadeInTime);
            blackScreen.color = c;
            yield return null;
        }
        c.a = 0f;
        blackScreen.color = c;
    }
    static void ActivateWithParents(GameObject go)
    {
        var t = go.transform;
        var stack = new Stack<Transform>();
        while (t != null) { stack.Push(t); t = t.parent; }
        while (stack.Count > 0)
        {
            var tr = stack.Pop();
            if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
        }
    }
    IEnumerator FadeSpritesSequence()
    {
        // 동시에 시작: 첫 스프라이트(4초), 나머지 스프라이트들(6초), 파티클(6초)
        if (firstFadeSprite)
            StartCoroutine(FadeToAlpha(firstFadeSprite, 0f, firstFadeDuration)); // 4초

        if (subsequentFadeSprites != null && subsequentFadeSprites.Count > 0)
            foreach (var sr in subsequentFadeSprites)
                if (sr) StartCoroutine(FadeToAlpha(sr, 0f, subsequentFadeDuration)); // 6초

        if (fadeParticleSystems != null && fadeParticleSystems.Count > 0)
            foreach (var ps in fadeParticleSystems)
                if (ps) StartCoroutine(FadeParticleSystems(ps, 0f, subsequentFadeDuration)); // 6초

        // 가장 긴 지속시간만큼 대기
        float maxDur = Mathf.Max(firstFadeDuration, subsequentFadeDuration);
        if (maxDur > 0f) yield return new WaitForSeconds(maxDur);
    }

    IEnumerator FadeToAlpha(SpriteRenderer sr, float targetAlpha, float duration)
    {
        // 현재 알파에서 targetAlpha까지 선형 보간
        Color start = sr.color;
        float startA = start.a;
        float t = 0f;

        if (!sr.gameObject.activeSelf)
            sr.gameObject.SetActive(true);

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, targetAlpha, duration > 0f ? t / duration : 1f);
            sr.color = new Color(start.r, start.g, start.b, a);
            yield return null;
        }
        // 최종 보정
        sr.color = new Color(start.r, start.g, start.b, targetAlpha);
    }
    IEnumerator FadeParticleSystems(ParticleSystem ps, float targetAlpha, float duration)
    {
        if (!ps) yield break;

        // Emission 서서히 0으로
        var em = ps.emission;
        float startRate = em.rateOverTimeMultiplier;

        // 렌더러들 수집(자식 포함)
        var renderers = ps.GetComponentsInChildren<ParticleSystemRenderer>(true);

        // 각 렌더러의 시작 색상 보관
        var baseColors = new Dictionary<Renderer, Color>();
        foreach (var r in renderers)
        {
            if (!r) continue;
            Color c = Color.white;
            var mat = r.sharedMaterial;
            if (mat)
            {
                if (mat.HasProperty("_Color")) c = mat.color;
                else if (mat.HasProperty("_TintColor")) c = mat.GetColor("_TintColor");
            }
            baseColors[r] = c;
        }

        float t = 0f;
        var mpb = new MaterialPropertyBlock();
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = duration > 0f ? t / duration : 1f;

            // emission 감소
            em.rateOverTimeMultiplier = Mathf.Lerp(startRate, 0f, u);

            // 알파 페이드
            foreach (var r in renderers)
            {
                if (!r) continue;
                var baseColor = baseColors[r];
                float a = Mathf.Lerp(baseColor.a, targetAlpha, u);

                r.GetPropertyBlock(mpb);
                // 대표적으로 쓰이는 키들 세트 (셰이더에 따라 다를 수 있음)
                mpb.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, a));
                mpb.SetColor("_TintColor", new Color(baseColor.r, baseColor.g, baseColor.b, a));
                r.SetPropertyBlock(mpb);
            }

            yield return null;
        }

        // 최종 보정 + 정리
        em.rateOverTimeMultiplier = 0f;
        foreach (var r in renderers)
        {
            if (!r) continue;
            var baseColor = baseColors[r];
            var mpb2 = new MaterialPropertyBlock();
            mpb2.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, targetAlpha));
            mpb2.SetColor("_TintColor", new Color(baseColor.r, baseColor.g, baseColor.b, targetAlpha));
            r.SetPropertyBlock(mpb2);
        }

        // 기존 파티클 제거
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
