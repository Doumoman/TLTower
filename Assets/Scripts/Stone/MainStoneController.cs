using UnityEngine;
using UnityEngine.EventSystems;

/// Settled ↔ Dragging ↔ Dropping 세 상태
public enum MainStoneState { Settled, Dragging, Dropping }   // ★ Dropping 추가

[RequireComponent(typeof(SpriteRenderer))]
public class MainStoneController : MonoBehaviour,
                                   IPointerDownHandler,
                                   IPointerUpHandler,
                                   IDragHandler
{
    /* ───────────── 드래그 / 회전 파라미터 ───────────── */
    [Header("Drag & Rotate")]
    public float holdToRotate = 0.75f;   // 회전 전환 시간
    public float rotateSpeed = -90f;    // 회전 속도(°/s, 음수면 CW)
    public float moveDeadZone = 0.4f;    // ‘가만히 있음’ 허용 거리

    [Header("Dropping → Settled")]
    [SerializeField] float settleCheckTime = 0.3f;  // ★ 정착 판정에 필요한 지속 시간
    [SerializeField] float settleVelocityEps = 0.05f; // ★ ‘거의 정지’ 기준 속도

    [Header("비주얼")]
    public float dragAlpha = 0.5f;
    public float clickScaleUp = 1.05f;

    /* ───────────── 캐시 / 상태 ───────────── */
    public MainStoneState State { get; private set; } = MainStoneState.Settled;

    Rigidbody2D RB => _rb ? _rb : (_rb = GetComponent<Rigidbody2D>());
    SpriteRenderer SR => _sr ? _sr : (_sr = GetComponent<SpriteRenderer>());
    PolygonCollider2D Phys => _phys ? _phys : (_phys = GetComponent<PolygonCollider2D>());
    PolygonCollider2D Click => _click ? _click : (_click = GetComponents<PolygonCollider2D>()[1]);

    Rigidbody2D _rb;
    SpriteRenderer _sr;
    PolygonCollider2D _phys, _click;

    /* Drag / Rotate ------------------------------------------------------ */
    int activePointer = -1;
    Vector2 dragOffset;
    Vector2 lastPointerWorld;
    Vector2 holdStartPos;
    float holdTimer;
    bool isRotating;

    /* Dropping → Settled ------------------------------------------------- */
    float settleTimer;                                    // ★

    /* ==================================================================== */
    #region 초기화
    void Awake()
    {
        if (!TryGetComponent(out Rigidbody2D rb))
            gameObject.AddComponent<Rigidbody2D>();

        BuildColliders();
        SetSettledVisual();
        if (State == MainStoneState.Settled)
            tag = "PlacedStone";
    }
    #endregion
    /* ==================================================================== */

    /* -------- FixedUpdate : 물리 회전 유지 -------- */
    void FixedUpdate()
    {
        if (isRotating && State == MainStoneState.Dragging)
            RB.angularVelocity = rotateSpeed;
    }

    /* -------- Update : 드래그 & Dropping 정착 체크 -------- */
    void Update()
    {
        /* ① 드래그 중 ‘가만히 있음’ → 회전 전환 -------------------------- */
        if (State == MainStoneState.Dragging && !isRotating && activePointer != -1)
        {
            Vector2 cur = GetCurrentPointerWorld();
            if (Vector2.Distance(cur, lastPointerWorld) < moveDeadZone)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdToRotate)
                {
                    SoundManager.Instance.PlayLoop("stone_rotate");
                    isRotating = true;
                    holdTimer = 0f;
                    holdStartPos = cur;
                    RB.angularVelocity = rotateSpeed;
                }
            }
            else
            {
                holdTimer = 0f;
                holdStartPos = cur;
            }
            lastPointerWorld = cur;
        }

        /* ② Dropping 중 ‘거의 정지’ → Settled 전환 ----------------------- */
        if (State == MainStoneState.Dropping)
        {
            bool slow = RB.velocity.magnitude < settleVelocityEps &&
                        Mathf.Abs(RB.angularVelocity) < 5f;

            settleTimer = slow ? settleTimer + Time.deltaTime : 0f;

            if (settleTimer >= settleCheckTime)
                BecomeSettled();                          // ★
        }
    }

    public void Init(Sprite spr, float mass = 1f, float angDrag = 0.05f)
    {
        // 스프라이트 교체
        SR.sprite = spr;

        // 콜라이더 다시 생성 (스프라이트 모양이 바뀌었으므로)
        RebuildColliders();

        // 물리 파라미터 설정
        RB.mass = mass;
        RB.angularDrag = angDrag;
    }

    void RebuildColliders() => BuildColliders();
    /* ==================================================================== */
    #region EventSystem 콜백
    public void OnPointerDown(PointerEventData e)
    {
        if (activePointer != -1 || State != MainStoneState.Settled) return;
        activePointer = e.pointerId;

        SoundManager.Instance.PlaySFX("stone_select");
        BeginDrag(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != activePointer || State != MainStoneState.Dragging) return;

        Vector2 curWorld = ScreenToWorld(e.position);

        /* ── 회전 중 ───────────────────────────────────────── */
        if (isRotating)
        {
            if (Vector2.Distance(curWorld, holdStartPos) >= moveDeadZone)
            {
                isRotating = false;
                RB.angularVelocity = 0f;
                dragOffset = (Vector2)transform.position - curWorld;
                SoundManager.Instance.StopLoop("stone_rotate");
            }
            return;
        }

        /* ── 일반 드래그 ───────────────────────────────────── */
        RB.MovePosition(curWorld + dragOffset);

        holdTimer += Time.deltaTime;
        if (holdTimer >= holdToRotate)
        {
            isRotating = true;
            holdTimer = 0f;
            holdStartPos = curWorld;
            RB.angularVelocity = rotateSpeed;
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != activePointer || State != MainStoneState.Dragging) return;
        activePointer = -1;

        StartDropping();                                  // ★ Settled 대신 Dropping 시작
    }
    #endregion
    /* ==================================================================== */

    #region 상태 전환 메서드
    void BeginDrag(Vector2 pointerScreenPos)
    {
        tag = "DraggingStone";
        State = MainStoneState.Dragging;

        RB.velocity = Vector2.zero;
        RB.angularVelocity = 0f;

        RB.isKinematic = true;
        RB.gravityScale = 0f;
        Phys.enabled = false;

        SR.color = new Color(1, 1, 1, dragAlpha);
        SR.sortingOrder += 10;

        isRotating = false;
        holdTimer = 0f;
        lastPointerWorld = ScreenToWorld(pointerScreenPos);
        dragOffset = (Vector2)transform.position - lastPointerWorld;
    }

    /* 드래그 종료 → Dropping 모드 진입 */
    void StartDropping()
    {
        State = MainStoneState.Dropping;

        // 태그는 아직 달지 않는다 – 정착이 끝난 뒤에 달도록 변경
        // tag = "PlacedStone";   // ← 이 줄 삭제

        RB.isKinematic = false;
        RB.gravityScale = 1f;
        RB.velocity = Vector2.zero;
        RB.angularVelocity = 0f;

        Phys.enabled = true;

        isRotating = false;
        holdTimer = 0f;
        settleTimer = 0f;

        SetSettledVisual();   // 불투명 복구
    }

    /* Dropping → Settled 최종 전환 */
    void BecomeSettled()
    {
        State = MainStoneState.Settled;
        settleTimer = 0f;

        tag = "PlacedStone";               // ★ 여기서 태그 부여

        RB.velocity = Vector2.zero;
        RB.angularVelocity = 0f;

        // 필요 시 정착 이벤트 호출
        // StoneFixer.Instance?.RegisterSettled(this);
    }

    void SetSettledVisual()
    {
        SR.color = Color.white;
        SR.sortingOrder = 0;
    }
    #endregion
    /* ==================================================================== */

    #region 콜라이더 빌드
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
    /* ==================================================================== */

    /* 유틸리티 ----------------------------------------------------------- */
    Vector2 ScreenToWorld(Vector2 screenPos) =>
        Camera.main.ScreenToWorldPoint(screenPos);

    Vector2 GetCurrentPointerWorld()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return ScreenToWorld(Input.mousePosition);
#else
        foreach (var t in Input.touches)
            if (t.fingerId == activePointer)
                return ScreenToWorld(t.position);
        return lastPointerWorld;
#endif
    }
}
