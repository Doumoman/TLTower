using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// "감속하며 정지" 애니메이션만 담당하는 최소 매니저.
/// 다른 클라우드/스톤/우주 배경 애니메이션 로직은 모두 제거되었습니다.
/// </summary>
public class SpaceAnimationManager : MonoBehaviour
{
    /* ────── Singleton ────── */
    public static SpaceAnimationManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Target Animator")]
    [SerializeField] Animator targetAnimator;     // 느려지게 만들 Animator
    [SerializeField] string animationStateName = "Play"; // 재생할 애니메이션 상태 이름

    [Header("Deceleration Settings")]
    [Tooltip("애니메이션 재생을 시작할 초기 속도")] public float initialSpeed = 1f;
    [Tooltip("감속에 걸리는 시간(초)")]          public float decelerationTime = 2f;

    /// <summary>
    /// 외부에서 호출하여 감속 애니메이션을 시작합니다.
    /// </summary>
    public void PlayDecelerate()
    {
        if (!targetAnimator)
        {
            Debug.LogWarning("SpaceAnimationManager: Target Animator is not assigned!");
            return;
        }

        // 애니메이션 상태가 존재하는지 확인
        if (!targetAnimator.HasState(0, Animator.StringToHash(animationStateName)))
        {
            Debug.LogWarning($"SpaceAnimationManager: Animation state '{animationStateName}' not found!");
            return;
        }

        // 애니메이션 재생 시작
        targetAnimator.speed = initialSpeed;
        targetAnimator.Play(animationStateName, 0, 0f);
        
        StartCoroutine(CoDecelerateToStop(targetAnimator, initialSpeed, decelerationTime));
    }

    /// <summary>
    /// 지정한 Animator를 선형(등가속도)으로 감속 → 정지시키는 코루틴
    /// </summary>
    IEnumerator CoDecelerateToStop(Animator anim, float v0, float tTotal)
    {
        float elapsed = 0f;

        while (elapsed < tTotal)
        {
            elapsed += Time.deltaTime;

            // 등가속도 감속 공식: v = v0 * (1 - t/tTotal)
            float currentSpeed = v0 * (1f - (elapsed / tTotal));
            anim.speed = Mathf.Max(0f, currentSpeed);

            yield return null;
        }

        anim.speed = 0f;           // 완전 정지
        Debug.Log("Animator decelerated to stop.");
    }
}