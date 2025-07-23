using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowStage : MonoBehaviour
{
    [Header("활성 순서대로 늘어놓기")]
    public GameObject[] persistHalos;   // 은은히 남을 후광
    public GameObject[] flashHalos;     // 번쩍 후 사라질 후광

    [Header("타이밍")]
    public float delayBetweenHalos = 0.2f;
    public float flashDuration = 0.15f;  // flashHalo가 켜져있는 시간
    public float fadeInTime = 0.1f;      // 페이드인 시간
    public float fadeOutTime = 0.1f;     // 페이드아웃 시간

    /* TransitionStage가 접근할 일회형 목록 */
    [HideInInspector] public List<GameObject> persistList = new();
    
    void Awake()                      // 시작할 땐 전부 꺼 두기
    {
        foreach (var h in persistHalos) 
        {
            if (h) 
            {
                h.SetActive(false);
                SetAlpha(h, 0f);
            }
        }
        foreach (var h in flashHalos)  
        {
            if (h) 
            {
                h.SetActive(false);
                SetAlpha(h, 0f);
            }
        }
    }

    public IEnumerator Run()
    {
        int count = Mathf.Max(persistHalos.Length, flashHalos.Length);

        for (int i = 0; i < count; i++)
        {
            // ① 지속형 Halo 서서히 켜기 (유지됨)
            if (i < persistHalos.Length && persistHalos[i])
            {
                persistHalos[i].SetActive(true);
                persistList.Add(persistHalos[i]);
                StartCoroutine(FadeInHalo(persistHalos[i], fadeInTime));
            }
                
            // ② 일회형 Halo 서서히 켜고 일정 시간 후 서서히 꺼기
            if (i < flashHalos.Length && flashHalos[i])
            {
                flashHalos[i].SetActive(true);
                StartCoroutine(FlashHaloRoutine(flashHalos[i], flashDuration));
            }

            yield return new WaitForSeconds(delayBetweenHalos);
        }
    }

    /// <summary>
    /// flashHalo를 서서히 켜고 일정 시간 후 서서히 꺼는 코루틴
    /// </summary>
    IEnumerator FlashHaloRoutine(GameObject flashHalo, float duration)
    {
        // 서서히 켜기
        yield return StartCoroutine(FadeInHalo(flashHalo, fadeInTime));
        
        // 유지 시간
        yield return new WaitForSeconds(duration - fadeInTime - fadeOutTime);
        
        // 서서히 꺼기
        yield return StartCoroutine(FadeOutHalo(flashHalo, fadeOutTime));
    }

    /// <summary>
    /// Halo를 서서히 켜는 코루틴
    /// </summary>
    IEnumerator FadeInHalo(GameObject halo, float duration)
    {
        var sr = halo.GetComponent<SpriteRenderer>();
        if (!sr) yield break;

        Color color = sr.color;
        color.a = 0f;
        sr.color = color;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            color.a = Mathf.Lerp(0f, 1f, t / duration);
            sr.color = color;
            yield return null;
        }
        
        color.a = 1f;
        sr.color = color;
    }

    /// <summary>
    /// Halo를 서서히 꺼는 코루틴
    /// </summary>
    IEnumerator FadeOutHalo(GameObject halo, float duration)
    {
        var sr = halo.GetComponent<SpriteRenderer>();
        if (!sr) yield break;

        Color color = sr.color;
        float startAlpha = color.a;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            color.a = Mathf.Lerp(startAlpha, 0f, t / duration);
            sr.color = color;
            yield return null;
        }
        
        color.a = 0f;
        sr.color = color;
        halo.SetActive(false);
    }

    /// <summary>
    /// Halo의 알파값을 즉시 설정
    /// </summary>
    void SetAlpha(GameObject halo, float alpha)
    {
        var sr = halo.GetComponent<SpriteRenderer>();
        if (sr)
        {
            Color color = sr.color;
            color.a = alpha;
            sr.color = color;
        }
    }
}


