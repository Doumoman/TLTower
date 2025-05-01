using UnityEngine;
using System.Linq;   // FirstOrDefault

public enum StoneState { Background, Dragging, Placed }

[RequireComponent(typeof(SpriteRenderer))]
public class StoneController : MonoBehaviour
{
    /* ---------- 인스펙터 ---------- */
    public StoneState state = StoneState.Background;
    public int stoneTypeIndex = 0;      // 스프라이트/프리팹 결정용
    [Header("Drag & Rotate")]
    public float holdToRotate = 0.75f;           // 길게 눌러 회전까지 걸리는 시간
    public float rotateSpeed = -90f;            // CW
    public float moveDeadZone = 0.4f;            // 회전 모드 중 마우스/손가락 이동 시 임계치

    /* ---------- 내부 필드 ---------- */
    Rigidbody2D rb;
    SpriteRenderer sr;
    Collider2D physCol;                    // 비-Trigger (물리 충돌)
    Collider2D clickCol;                   // Trigger (OnMouse 이벤트용)
    Vector3 dragOffset;
    Vector2 holdStartPos;
    float holdTimer;
    bool isRotating;

    #region — 초기화 / 상태별 세팅 —
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();     // Background 상태이면 null 일 수도 있음
        sr = GetComponent<SpriteRenderer>();

        // Collider 분류
        var cols = GetComponents<Collider2D>();
        physCol = cols.FirstOrDefault(c => !c.isTrigger);
        clickCol = cols.FirstOrDefault(c => c.isTrigger);

        // 안전 장치
        if (clickCol == null)
        {
            clickCol = gameObject.AddComponent<CircleCollider2D>();
            clickCol.isTrigger = true;
        }
    }

    /// <summary>사이드 패널용 ‘배경(stub)’ 돌로 초기화</summary>
    public void InitAsBackground(int typeIdx, Sprite sprite)
    {
        state = StoneState.Background;
        stoneTypeIndex = typeIdx;
        sr.sprite = sprite;
        gameObject.tag = "BGStone";

        // 배경 돌은 물리 불필요 → Rigidbody/Physics Collider 제거
        if (rb) { Destroy(rb); rb = null; }
        if (physCol) { Destroy(physCol); physCol = null; }

        clickCol.enabled = true;
        sr.color = Color.white;
    }

    /// <summary>실제 플레이어블 돌(Dragging 상태)로 전환</summary>
    public void InitAsPlayable(Sprite sprite, float mass = 1f, float friction = 0.8f)
    {
        sr.sprite = sprite;
        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";

        // Rigidbody가 없으면 새로 부착
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // 드래그 중에는 중력 없음
            rb.mass = mass;
        }
        else
        {
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }

        // Physics Collider가 없으면 추가(Box 또는 Circle 등 원하는 타입)
        if (!physCol)
        {
            physCol = gameObject.AddComponent<CircleCollider2D>();
            physCol.isTrigger = false;
        }
        physCol.enabled = false;   // 드래그 중 물리 off

        clickCol.enabled = true;

        dragOffset = Vector3.zero;  // OnMouseDown 에서 다시 계산
        holdTimer = 0;
        isRotating = false;
        sr.color = new Color(1, 1, 1, 0.5f); // 반투명
    }
    #endregion

    /* ---------- 입력 이벤트 ---------- */
    void OnMouseDown()
    {
        /* --- 1) Background 돌 클릭 → Spawner 가 실제 돌 생성 --- */
        if (state == StoneState.Background)
        {
            StoneSpawner.Instance.SpawnPlayableAndBeginDrag(this); // 새 돌 생성
            StoneSpawner.Instance.RemoveStub(this);                // 패널에서 stub 삭제
            return;
        }

        /* --- 2) Placed 돌 다시 클릭 → Drag 모드 --- */
        if (state == StoneState.Placed)
        {
            StartDragging();
        }
    }

    void OnMouseDrag()
    {
        if (state != StoneState.Dragging) return;

        Vector2 mouseWorld = ScreenToWorld();

        /* --- 회전 / 이동 전환 --- */
        if (isRotating)
        {
            // dead-zone 이상 손가락/마우스 이동 시 → 회전 종료 & 이동 모드
            if (Vector2.Distance(mouseWorld, holdStartPos) >= moveDeadZone)
            {
                isRotating = false;
                rb.angularVelocity = 0;
                dragOffset = transform.position - (Vector3)mouseWorld;
            }
            return; // 회전은 FixedUpdate 에서 angularVelocity 로 처리
        }

        /* --- 일반 드래그 (MovePosition) --- */
        rb.MovePosition((Vector3)mouseWorld + dragOffset);

        /* --- 홀드 타이머 --- */
        holdTimer += Time.deltaTime;
        if (holdTimer >= holdToRotate)
        {
            isRotating = true;
            holdTimer = 0;
            holdStartPos = mouseWorld;
            rb.angularVelocity = rotateSpeed;
        }
    }

    void OnMouseUp()
    {
        if (state != StoneState.Dragging) return;

        /* --- 드래그 종료 : Placed --- */
        state = StoneState.Placed;
        gameObject.tag = "PlacedStone";

        rb.isKinematic = false;
        rb.gravityScale = 1f;
        rb.angularVelocity = 0;
        physCol.enabled = true;
        clickCol.enabled = true;
        sr.color = Color.white;

        StoneSpawner.Instance.NotifyPlaced(this);

        /* ▼▼ 새 돌 4초 타이머 예약 ▼▼ */
        StoneSpawner.Instance.ScheduleRandomStone(4f);
    }

    /* ---------- 내부 도움 메서드 ---------- */
    void StartDragging()
    {
        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";

        dragOffset = transform.position - (Vector3)ScreenToWorld();
        holdTimer = 0;
        holdStartPos = ScreenToWorld();
        isRotating = false;

        rb.isKinematic = true;
        rb.gravityScale = 0;
        physCol.enabled = false;

        sr.color = new Color(1, 1, 1, 0.5f);
    }

    Vector2 ScreenToWorld()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    void FixedUpdate()
    {
        // 회전 중일 때만 지속
        if (isRotating && state == StoneState.Dragging)
            rb.angularVelocity = rotateSpeed;
    }
}
