using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Stone : MonoBehaviour
{
    public StoneData Data { get; private set; }
    public bool FromSideSlot { get; set; }   // 사이드 패널 출신 여부

    Rigidbody2D rb;
    Collider2D col;

    public void Init(StoneData d)
    {
        Data = d;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.mass = d.mass;
        transform.localScale = Vector3.one * d.size;

        var mat = new PhysicsMaterial2D { friction = d.friction };
        col.sharedMaterial = mat;
    }

    // 드래그 중에는 물리 꺼두기
    public void SetGrabbed(bool grabbed)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = grabbed;
        rb.simulated = !grabbed;
    }

    public void SetPhysics(bool active)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = !active;
        rb.gravityScale = active ? 1f : 0f;   // 필요하면 기본값 조정
    }
}