using System.Collections;
using UnityEngine;

public class StoneSequenceManager : MonoBehaviour
{
    [Header("Stage References")]
    public RiseStage       riseStage;
    public GlowStage       glowStage;
    public TransitionStage transitionStage;

    [Header("Optional Extras")]
    public ParticleSystem spaceParticles;

    void Start()
    {
        // 필요하면 자동 Play
        StartCoroutine(Play());
    }
     public void PlaySequence()
    {
        // 이미 만들어 둔 코루틴 Play()를 돌린다고 가정
    StopAllCoroutines();     // 혹시 이전 실행 중이면 정지
    StartCoroutine(Play());  // ① 상승 → ② 글로우 → ③ 전환 순으로 진행
    }

    public IEnumerator Play()
    {
        /* 1) 살짝 올라오는 돌 -------------------------- */
        yield return riseStage.Run();

        // 우주 파티클 켜기, “오…!” 같은 대사 출력 등
        //spaceParticles?.Play();


        /* 2) 돌이 하나씩 빛나는 연출 -------------------- */
        yield return glowStage.Run();

        /* 3) Fade-Out + 보살 등장 ----------------------- */
        yield return transitionStage.Run();


    }

   


}