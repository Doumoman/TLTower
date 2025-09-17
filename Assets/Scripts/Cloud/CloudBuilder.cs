using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 1개의 CloudBuilder(=Anchor) 오브젝트로
///  • 자식 구름 자동 스폰
///  • Anchor 드래그
///  • 옵션: FollowOnly / PhysicsJoint
/// 모두 처리
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CloudBuilder : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    enum CloudState { Idle, Dragging, Sticky }
    CloudState curState = CloudState.Idle;
    //인스펙터 조정값
    public enum Mode { FollowOnly, PhysicsJoint }

    [SerializeField] string stickyCloudLayerName = "StickyCloud";
    [SerializeField] string draggingCloudLayerName = "Dragging";

    int stickyLayer;
    int draggingLayer;

    [Header("Spawn Prefabs")]

    [Tooltip("여러 구름 프리팹을 넣어 두면 랜덤으로 뽑기.")]
    [SerializeField] GameObject[] cloudPrefabs;

    [Header("Spawn Config")]
    [Range(1, 30)] public int numberOfChildren = 7;
    public float spawnRadius = 2.5f;
    public float minDistance = 0f;

    [Header("Mode & Physics")]
    public Mode mode = Mode.FollowOnly;
    public float childMass = 1f;
    public float childGravity = 0.3f;
    public float springFrequency = 6f;
    public float springDamping = 0.5f;
    public float springBreakForce = 0f;

    [Header("Gravity Zone (Sticky 전용)")]
    [SerializeField] GameObject gravityZone;

    [Header("Drag / Release Gravity")]
    [SerializeField] float dragGravity = 0f;    // 끌 때
    [SerializeField] float releasedGravity = 0.02f;
    Rigidbody2D anchorRb;
    readonly List<Rigidbody2D> childRbs = new();

    void Awake()
    {
        stickyLayer = LayerMask.NameToLayer(stickyCloudLayerName);
        draggingLayer = LayerMask.NameToLayer(draggingCloudLayerName);

        // Anchor 의 Rigidbody2D 세팅 (드래그 대상)
        anchorRb = GetComponent<Rigidbody2D>();
        anchorRb.bodyType = RigidbodyType2D.Kinematic;
        anchorRb.gravityScale = 0f;
        anchorRb.simulated = true;
        if (gravityZone) gravityZone.SetActive(false);
    }

    void Start()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0)
        {
            return;
        }

        // 자식 위치 계산 & Instantiate
        var used = new List<Vector2>();

        for (int i = 0; i < numberOfChildren; ++i)
        {
            for (int attempt = 0; attempt < 100; ++attempt)
            {
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
                if (IsFarEnough(pos, used))
                {
                    used.Add(pos);

                    GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
                    GameObject child = Instantiate(prefab, pos, Quaternion.identity, transform);

                    if (mode == Mode.FollowOnly)
                    {
                        // Collider만 남기고 Rigidbody2D 제거
                        var rb = child.GetComponent<Rigidbody2D>();
                        if (rb) Destroy(rb);
                    }
                    else // PhysicsJoint
                    {
                        Rigidbody2D rb = child.GetComponent<Rigidbody2D>()
                                         ?? child.AddComponent<Rigidbody2D>();
                        rb.mass = childMass;
                        rb.gravityScale = childGravity;

                        SpringJoint2D joint = child.AddComponent<SpringJoint2D>();
                        joint.connectedBody = anchorRb;
                        joint.autoConfigureDistance = true;  // 현재 거리 유지
                        joint.frequency = springFrequency;
                        joint.dampingRatio = springDamping;
                        joint.breakForce = springBreakForce;

                        childRbs.Add(rb);
                    }
                    break;
                }
            }
        }
    }
    void SetChildrenCollision(bool enablePhysics)
    {
        foreach (Transform c in transform)
        {
            var col = c.GetComponent<Collider2D>();
            if (col) col.isTrigger = !enablePhysics;

            if (mode == Mode.PhysicsJoint) continue; // 이미 RigidbodyO
            if (enablePhysics)
            {
                var rb = c.GetComponent<Rigidbody2D>() ?? c.gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0;          // 중력 필요 없으면 0
            }
        }
    }

    bool IsFarEnough(Vector2 p, List<Vector2> list)
    {
        foreach (var q in list)
            if (Vector2.Distance(p, q) < minDistance) return false;
        return true;
    }

    //드래그 인터페이스
    public void OnPointerDown(PointerEventData e)
    {
        curState = CloudState.Dragging;
        anchorRb.bodyType = RigidbodyType2D.Kinematic;
        anchorRb.gravityScale = dragGravity;        // 중력 OFF
        anchorRb.WakeUp();

        anchorRb.linearVelocity = Vector2.zero;
        anchorRb.angularVelocity = 0f;
        ZeroChildrenVelocity();

        SetChildrenGravity(dragGravity);            // 자식도 중력 OFF
        SetLayerRecursively(transform, draggingLayer);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(eventData.position);
        Vector2 target = new(world.x, world.y);
        anchorRb.MovePosition(target);
    }
    public void OnPointerUp(PointerEventData e)
    {
        curState = CloudState.Sticky;

        anchorRb.bodyType = RigidbodyType2D.Dynamic;   // 다시 못 움직이게 하려면 그대로
        anchorRb.gravityScale = releasedGravity;
        SetChildrenGravity(releasedGravity);
        // StickyCloud 레이어로 전환
        SetLayerRecursively(transform, stickyLayer);
        if (gravityZone) gravityZone.SetActive(true);
    }
    void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform c in root)
            SetLayerRecursively(c, layer);
    }
    void SetChildrenGravity(float g)
    {
        foreach (Transform c in transform)
        {
            if (gravityZone && c == gravityZone.transform) continue; // 블랙홀 제외
            var rb = c.GetComponent<Rigidbody2D>();
            if (rb) rb.gravityScale = g;
        }
    }
    void ZeroChildrenVelocity()
    {
        foreach (Transform c in transform)
        {
            if (gravityZone && c == gravityZone.transform) continue; // 블랙홀 제외
            var rb = c.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }
}