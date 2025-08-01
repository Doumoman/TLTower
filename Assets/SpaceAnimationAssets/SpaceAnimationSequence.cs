using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceAnimationSequence : MonoBehaviour
{
    [Header("Stage References")]
    public RiseStage riseStage;
    public TransitionStage transitionStage;

    [Header("Optional Extras")]
    public ParticleSystem spaceParticles;

    [Header("Screen Fade-In")]
    public float delay = 1f;
    public SpriteRenderer blackScreen;   // 전체 화면을 덮는 검은 SpriteRenderer
    public float fadeInTime = 1f;        // 알파 1 → 0 으로 줄이는 시간
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
        Coroutine fadeCo = StartCoroutine(FadeInFromBlack());
        Coroutine riseCo = StartCoroutine(riseStage.Run());

        // ② 둘 다 끝날 때까지 대기
        yield return fadeCo;   // Fade-in 끝날 때까지
        yield return riseCo;   // riseStage.Run() 끝날 때까지
                               // (이미 끝났다면 즉시 통과)

        // ③ 이후 단계 계속
        //spaceParticles?.Play();
        yield return transitionStage.Run();

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
}
