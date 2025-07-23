using System.Collections;
using UnityEngine;

public class TransitionStage : MonoBehaviour
{
    [Header("외부 참조")]
    public GlowStage glowStage;              // GlowStage를 참조해야 flashHalos에 접근
    public SpriteRenderer stone;             // 돌 이미지 (페이드아웃할 대상)
    
    [Header("페이드인할 이미지들")]
    public SpriteRenderer bodhisattva;       // 보살 이미지
    [Tooltip("보살과 함께 페이드인될 추가 이미지들")]
    public SpriteRenderer[] additionalImages; // 추가 이미지들
    
    [Header("페이드 설정")]
    public float fadeTime = 1f;

    void Awake()
    {
        // 보살 이미지가 처음에는 안 보이도록 설정
        if (bodhisattva)
        {
            Color color = bodhisattva.color;
            color.a = 0f;
            bodhisattva.color = color;
        }
        
        // 추가 이미지들도 처음에는 안 보이도록 설정
        if (additionalImages != null)
        {
            foreach (var img in additionalImages)
            {
                if (img)
                {
                    Color color = img.color;
                    color.a = 0f;
                    img.color = color;
                }
            }
        }
    }

    public IEnumerator Run()
    {
        // 돌과 halo 페이드아웃과 보살+추가이미지들 페이드인이 동시에 진행
        var fadeOutTasks = new System.Collections.Generic.List<IEnumerator>();
        var fadeInTasks = new System.Collections.Generic.List<IEnumerator>();
        
        // persistHalo들 페이드아웃
        foreach (var persist in glowStage.persistList)
        {
            if (persist) fadeOutTasks.Add(FadeOutSprite(persist, fadeTime));
        }
        
        // 돌 페이드아웃
        if (stone) fadeOutTasks.Add(FadeOutSprite(stone.gameObject, fadeTime));
        
        // 보살 페이드인
        if (bodhisattva) fadeInTasks.Add(FadeInSprite(bodhisattva.gameObject, fadeTime));
        
        // 추가 이미지들 페이드인
        if (additionalImages != null)
        {
            foreach (var img in additionalImages)
            {
                if (img) fadeInTasks.Add(FadeInSprite(img.gameObject, fadeTime));
            }
        }
        
        // 모든 페이드 효과를 동시에 시작
        foreach (var task in fadeOutTasks)
        {
            StartCoroutine(task);
        }
        foreach (var task in fadeInTasks)
        {
            StartCoroutine(task);
        }
        
        // 모든 페이드 효과가 완료될 때까지 대기
        yield return new WaitForSeconds(fadeTime);
    }

    IEnumerator FadeOutSprite(GameObject obj, float t)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (!sr) yield break;

        Color c = sr.color;
        for (float e = 0; e < t; e += Time.deltaTime)
        {
            c.a = Mathf.Lerp(1f, 0f, e / t);
            sr.color = c;
            yield return null;
        }
        c.a = 0f; sr.color = c;
    }

    IEnumerator FadeInSprite(GameObject obj, float t)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (!sr) yield break;

        Color c = sr.color; c.a = 0f; sr.color = c;
        for (float e = 0; e < t; e += Time.deltaTime)
        {
            c.a = Mathf.Lerp(0f, 1f, e / t);
            sr.color = c;
            yield return null;
        }
        c.a = 1f; sr.color = c;
    }
}