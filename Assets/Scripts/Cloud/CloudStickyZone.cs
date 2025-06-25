using UnityEngine;

/// <summary>
/// • 트리거 안으로 들어온 돌(StoneController)에
///   블랙홀처럼 인력(Force)을 가한다.
/// • Dropping / Settled 상태일 때만 적용.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CloudStickyZone : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("기본 힘의 세기 (G·M 같은 역할). 게임 스케일에 맞춰 조절")]
    public float pullStrength = 15f;

    [Tooltip("힘이 너무 커지는 것 방지용 상한 (0 = 무제한)")]
    public float maxForce = 50f;

    [Tooltip("거리 감쇠 (1 = 역제곱, 0 = 감쇠 없음, 2 = 역세제곱 …)")]
    [Range(0f, 3f)] public float falloffPower = 1f;

    Collider2D triggerCol;

    void Awake()
    {
        triggerCol = GetComponent<Collider2D>();
        triggerCol.isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 돌이 아니면 패스
        var stone = other.GetComponent<StoneController>();
        if (stone == null) return;

        // Dropping / Settled 상태가 아니면 패스
        if (stone.state != StoneState.Dropping &&
            stone.state != StoneState.Settled) return;

        Rigidbody2D rb = stone.GetComponent<Rigidbody2D>();
        if (rb == null || !rb.simulated) return;

        Vector2 dir = (Vector2)transform.position - rb.position;
        float distSq = dir.sqrMagnitude;
        if (distSq < 1e-4f) return;                    // 너무 가까우면 무시

        float forceMag = pullStrength / Mathf.Pow(distSq, falloffPower * 0.5f);
        if (maxForce > 0) forceMag = Mathf.Min(forceMag, maxForce);

        rb.AddForce(dir.normalized * forceMag, ForceMode2D.Force);
    }
}