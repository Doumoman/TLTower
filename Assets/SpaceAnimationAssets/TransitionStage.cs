using System.Collections;
using UnityEngine;

public class TransitionStage : MonoBehaviour
{
    /* ────────── 페이드-아웃 대상 ────────── */
    [Header("▼ 페이드아웃할 오브젝트(최대 5개)")]
    public SpriteRenderer[] fadeOutTargets = new SpriteRenderer[5];   // Inspector에서 할당

    /* ────────── 페이드-인 대상 ────────── */
    [Header("▼ 페이드인할 이미지들")]
    public SpriteRenderer bodhisattva;        // 보살 이미지
    [Tooltip("보살과 함께 페이드인될 추가 이미지들")]
    public SpriteRenderer[] additionalImages; // 추가 이미지들

    /* ────────── 공통 설정 ────────── */
    [Header("▼ 페이드 설정")]
    public float fadeTime = 1f;

    void Awake()
    {
        // 보살 & 추가 이미지들을 처음엔 보이지 않도록 세팅
        SetAlpha(bodhisattva, 0f);
        if (additionalImages != null)
            foreach (var img in additionalImages)
                SetAlpha(img, 0f);
    }

    /* ────────── 메인 시퀀스 ────────── */
    public IEnumerator Run()
    {
        // 동시에 실행할 코루틴 목록
        var fades = new System.Collections.Generic.List<IEnumerator>();

        /* ── (1) 페이드-아웃 ── */
        foreach (var target in fadeOutTargets)
            if (target) fades.Add(FadeSprite(target, 1f, 0f, fadeTime));

        /* ── (2) 페이드-인 ── */
        if (bodhisattva) fades.Add(FadeSprite(bodhisattva, 0f, 1f, fadeTime));
        if (additionalImages != null)
            foreach (var img in additionalImages)
                if (img) fades.Add(FadeSprite(img, 0f, 1f, fadeTime));

        // 모든 페이드 코루틴을 시작
        foreach (var f in fades) StartCoroutine(f);

        // 지정된 시간만큼 기다리면 모든 페이드가 완료됨
        yield return new WaitForSeconds(fadeTime);
        for (int i = 0; i < 6; i++) BosalManager.Instance.Speak("SpaceEnding");
    }

    /* ────────── 유틸 ────────── */
    void SetAlpha(SpriteRenderer sr, float a)
    {
        if (!sr) return;
        var c = sr.color; c.a = a; sr.color = c;
    }

    IEnumerator FadeSprite(SpriteRenderer sr, float from, float to, float t)
    {
        if (!sr) yield break;

        Color c = sr.color;
        for (float e = 0; e < t; e += Time.deltaTime)
        {
            c.a = Mathf.Lerp(from, to, e / t);
            sr.color = c;
            yield return null;
        }
        c.a = to; sr.color = c;
    }
}