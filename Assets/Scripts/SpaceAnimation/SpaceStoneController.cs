using UnityEngine;
using UnityEngine.EventSystems;

/// Floating ↔ Dragging ↔ Snapped 세 상태
public enum SpaceStoneState { Floating, Dragging, Snapped }

[RequireComponent(typeof(SpriteRenderer))]
public class SpaceStoneController : MonoBehaviour,
                                   IPointerDownHandler,
                                   IPointerUpHandler,
                                   IDragHandler
{
    /* ────────── 공개 필드 ────────── */
    [Header("Drag & Rotate")]
    public float holdToRotate = 0.75f;
    public float rotateSpeed = -90f;
    public float moveDeadZone = 0.4f;

    [Header("Visual")]
    public float dragAlpha = 0.5f;
    public float clickScaleUp = 1.05f;

    /* ────────── 내부 상태 ────────── */
    public SpaceStoneState State { get; private set; } = SpaceStoneState.Floating;
    public int presetIndex;           // AnimationManager 에서 지정

    Rigidbody2D _rb;
    SpriteRenderer _sr;
    PolygonCollider2D _phys, _click;
    PolygonCollider2D poly;

    int activePointer = -1;
    Vector2 dragOffset, lastPointerWorld, holdStartPos;
    float holdTimer;
    bool isRotating;

    /* ================================================================= */
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        BuildColliders();

        poly = GetComponent<PolygonCollider2D>();
        _rb.gravityScale = 0f;     // ★ 무중력
        _rb.angularDrag = 0.05f;
        _rb.mass = 1f;
        _sr = GetComponent<SpriteRenderer>();

        SetFloatingVisual();
    }
   
    public void Init(Sprite spr, int index, float mass = 1f, float angDrag = 0.05f)
    {
        // ① 스프라이트‧콜라이더 갱신
        _sr.sprite = spr;
        BuildColliders();          // 새 스프라이트 모양으로 리빌드

        // ② 물리 파라미터
        _rb.mass = mass;
        _rb.angularDrag = angDrag;

        // ③ 프리셋 인덱스 저장(스냅 검사용)
        presetIndex = index;
    }
    /* ================================================================= */
    #region EventSystem 콜백
    public void OnPointerDown(PointerEventData e)
    {
        if (activePointer != -1 || State != SpaceStoneState.Floating) return;
        activePointer = e.pointerId;
        BeginDrag(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != activePointer || State != SpaceStoneState.Dragging) return;

        Vector2 curWorld = ScreenToWorld(e.position);

        /* 회전 모드 중 */
        if (isRotating)
        {
            if (Vector2.Distance(curWorld, holdStartPos) >= moveDeadZone)
            {
                isRotating = false;
                _rb.angularVelocity = 0f;
                dragOffset = (Vector2)transform.position - curWorld;
            }
            return;
        }

        /* 일반 드래그 */
        _rb.MovePosition(curWorld + dragOffset);

        holdTimer += Time.deltaTime;
        if (holdTimer >= holdToRotate)
        {
            isRotating = true;
            holdTimer = 0f;
            holdStartPos = curWorld;
            _rb.angularVelocity = rotateSpeed;
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != activePointer || State != SpaceStoneState.Dragging) return;
        activePointer = -1;
        EndDrag();
    }
    #endregion
    /* ================================================================= */

    void BeginDrag(Vector2 pointerScreenPos)
    {
        if (poly) { Destroy(poly); poly = null; }
        State = SpaceStoneState.Dragging;
        tag = "DraggingStone";

        _rb.isKinematic = true;
        _rb.angularVelocity = 0f;
        _rb.velocity = Vector2.zero;
        _sr.color = new Color(1, 1, 1, dragAlpha);
        _sr.sortingOrder += 10; 
        
        _phys.enabled = false;

        lastPointerWorld = ScreenToWorld(pointerScreenPos);
        dragOffset = (Vector2)transform.position - lastPointerWorld;

        isRotating = false;
        holdTimer = 0f;
    }

    void EndDrag()
    {
        if (poly == null)
            poly = gameObject.AddComponent<PolygonCollider2D>();
        State = SpaceStoneState.Floating;
        tag = "FloatingStone";

        _rb.isKinematic = false;
        _phys.enabled = true;
        isRotating = false;
        holdTimer = 0f;

        SetFloatingVisual();
    }

    void SetFloatingVisual()
    {
        _sr.color = Color.white;
        _sr.sortingOrder = 0;
    }

    /* ================================================================= */
    #region 스냅(맞추기) 로직

    void OnTriggerStay2D(Collider2D col)
    {
        var target = col.GetComponent<SpaceStoneTarget>();
        if (!target) return;

        /* 인덱스가 다르면 무시 */
        if (target.expectedIndex != presetIndex) return;

        /* 아직 뜬 상태이고 스냅 범위 안이면 고정 */
        if (State == SpaceStoneState.Floating &&
            Vector2.Distance(transform.position, target.snapPoint.position) < target.snapRange)
        {
            SnapToTarget(target);
        }
    }

    void SnapToTarget(SpaceStoneTarget t)
    {
        transform.position = t.snapPoint.position;
        transform.rotation = Quaternion.Euler(0, 0, t.snapRotationZ);

        _rb.velocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.isKinematic = true;   // 더 이상 움직이지 않음

        State = SpaceStoneState.Snapped;
        tag = "PlacedStone";
        AnimationManager.Instance?.NotifyStoneSnapped();
    }
    #endregion
    /* ================================================================= */

    #region 콜라이더 생성
    void BuildColliders()
    {
        foreach (var c in GetComponents<PolygonCollider2D>()) Destroy(c);

        _phys = gameObject.AddComponent<PolygonCollider2D>();
        _phys.isTrigger = false;

        _click = gameObject.AddComponent<PolygonCollider2D>();
        _click.isTrigger = true;
        _click.pathCount = _phys.pathCount;

        for (int i = 0; i < _phys.pathCount; ++i)
        {
            var p = _phys.GetPath(i);
            for (int j = 0; j < p.Length; ++j) p[j] *= clickScaleUp;
            _click.SetPath(i, p);
        }
    }
    #endregion
    /* ================================================================= */

    /* 유틸리티 */
    Vector2 ScreenToWorld(Vector2 p) =>
        Camera.main.ScreenToWorldPoint(p);
}
