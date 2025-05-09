using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;


public enum StoneState { Background, Dragging, Dropping, Settled, Fixed }

[RequireComponent(typeof(SpriteRenderer))]
public class StoneController : MonoBehaviour,
                               IPointerDownHandler,
                               IPointerUpHandler,
                               IDragHandler
{
    const int STUB_ORDER = 10_000;   // Stub(Background) 최상단 출력용
    const float STUB_Z = -1f;    // 카메라 쪽(z‑축 ‑값)으로 살짝 당김
    const float NORMAL_Z = 0f;     // 게임 중 돌의 기본 z

    public StoneState state = StoneState.Background;
    public int stoneTypeIndex = 0;
    public StoneData Data { get; private set; }

    [Header("StoneSettled")]
    SpriteRenderer outlineSR;
    [SerializeField] float settleCheckTime = .3f;
    [SerializeField] float settleVelocityEps = .05f;

    float settleTimer = 0f;

    [Header("Drag & Rotate")]
    public float holdToRotate = 0.75f;
    public float rotateSpeed = -90f;
    public float moveDeadZone = 0.4f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Collider2D physCol;   // isTrigger = false
    Collider2D clickCol;  // isTrigger = true

    Vector3 dragOffset;
    Vector2 holdStartPos;
    Vector2 lastPointerWorld;
    float holdTimer;
    bool isRotating;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        gameObject.tag = "Stone";
        var cols = GetComponents<Collider2D>();
        physCol = cols.FirstOrDefault(c => !c.isTrigger);
        clickCol = cols.FirstOrDefault(c => c.isTrigger);

        if (clickCol == null)
            Debug.LogWarning($"[{name}] Trigger Collider(ClickCol) 가 없습니다!", this);
        if (physCol == null && state != StoneState.Background)
            Debug.LogWarning($"[{name}] Physics Collider(PhysCol) 가 없습니다!", this);
        CreateOutlineObject(); // 빨간색 테두리 형성
    }

    void Update()
    {
        if (state == StoneState.Dropping)
        {
            bool slow = rb.velocity.magnitude < settleVelocityEps &&
                        Mathf.Abs(rb.angularVelocity) < 5f;

            settleTimer = slow ? settleTimer + Time.deltaTime : 0;

            if (settleTimer >= settleCheckTime)
            {
                state = StoneState.Settled;
                outlineSR.enabled = true;
                StoneFixer.Instance?.RegisterSettled(this); //Settled 됐다고 StoneFixer 에 보고
                settleTimer = 0;
            }
        }
        if (state == StoneState.Dragging && !isRotating)
        {
            // 현재 포인터의 월드 좌표 얻기
            Vector2 curWorld = GetCurrentPointerWorld();

            // 거의 안 움직였으면 정지로 간주 → 시간 누적
            if (Vector2.Distance(curWorld, lastPointerWorld) < moveDeadZone)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdToRotate)
                {
                    // 회전 시작
                    isRotating = true;
                    holdTimer = 0f;
                    holdStartPos = curWorld;
                    rb.angularVelocity = rotateSpeed;
                }
            }
            else
            {
                // 움직였으면 홀드 시간 리셋
                holdTimer = 0f;
                holdStartPos = curWorld;
            }

            lastPointerWorld = curWorld;
        }
    }
    void FixedUpdate()
    {
        if (isRotating && state == StoneState.Dragging)
            rb.angularVelocity = rotateSpeed;
    }
    public void InitAsBackground(StoneData data)
    {
        Data = data;

        state = StoneState.Background;
        sr.sprite = data.sprite;
        outlineSR.sprite = data.sprite;
        gameObject.tag = "BGStone";

        if (rb) rb.simulated = false;
        if (physCol) physCol.enabled = false;

        sr.color = Color.white;
        sr.sortingOrder = STUB_ORDER;
        outlineSR.sortingOrder = STUB_ORDER - 1;

        transform.position = new Vector3(transform.position.x,
                                         transform.position.y,
                                         STUB_Z);
    }

    // Playable(Dragging 시작)
    public void InitAsPlayable(StoneData data)
    {
        Data = data;

        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";

        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.mass = data.mass;
        rb.angularDrag = data.angularDrag;
        rb.gravityScale = 0f;
        rb.isKinematic = true;

        if (physCol)
        {
            physCol.enabled = false;
            if (data.material2D) physCol.sharedMaterial = data.material2D;
        }

        sr.sprite = data.sprite;
        outlineSR.sprite = data.sprite;
        outlineSR.enabled = false;
        sr.color = new Color(1, 1, 1, .5f);

        transform.position = new Vector3(transform.position.x,
                                         transform.position.y,
                                         NORMAL_Z);
        sr.sortingOrder = 0;
        outlineSR.sortingOrder = -1;
    }
    public void SetFixed()
    {
        if (state == StoneState.Fixed) return;

        state = StoneState.Fixed;
        gameObject.tag = "FixedStone";

        rb.isKinematic = true;
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        outlineSR.enabled = false;          // 돌 고정되면 테두리 끄기

        // 물리 충돌은 유지 (physCol.enabled = true)
    }
    int activePointer = -1;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointer != -1) return;
        activePointer = eventData.pointerId;

        if (state == StoneState.Background)     // Stub → Playable
        {
            StoneSpawner.Instance.SpawnPlayableAndBeginDrag(this);
            return;
        }
        if (state == StoneState.Dropping){
            StartDragging();
        }
        dragOffset = transform.position - (Vector3)ScreenToWorld(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer) return;
        if (state != StoneState.Dragging) return;

        Vector2 mouseWorld = ScreenToWorld(eventData.position);

        // 회전 중
        if (isRotating)
        {
            if (Vector2.Distance(mouseWorld, holdStartPos) >= moveDeadZone)
            {
                isRotating = false;
                rb.angularVelocity = 0;
                dragOffset = transform.position - (Vector3)mouseWorld;
            }
            return;
        }

        // 일반 드래그 
        rb.MovePosition((Vector3)mouseWorld + dragOffset);

        holdTimer += Time.deltaTime;
        if (holdTimer >= holdToRotate)
        {
            isRotating = true;
            holdTimer = 0;
            holdStartPos = mouseWorld;
            rb.angularVelocity = rotateSpeed;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer) return;
        activePointer = -1;

        if (state != StoneState.Dragging) return;

        state = StoneState.Dropping;
        gameObject.tag = "PlacedStone";

        rb.isKinematic = false;
        rb.gravityScale = 1f;
        rb.angularVelocity = 0;

        if (physCol) physCol.enabled = true;
        sr.color = Color.white;

        StoneSpawner.Instance.NotifyPlaced(this);
    }
    

    void StartDragging() //드래그 중 돌의 상태 설정
    {
        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.isKinematic = true;
        rb.gravityScale = 0;

        dragOffset = transform.position - (Vector3)ScreenToWorld();
        holdTimer = 0;
        holdStartPos = ScreenToWorld();
        isRotating = false;

        if (physCol) physCol.enabled = false;

        sr.color = new Color(1, 1, 1, 0.5f);
    }

    Vector2 ScreenToWorld() =>
    Camera.main.ScreenToWorldPoint(Input.mousePosition);

    Vector2 ScreenToWorld(Vector2 screenPos) =>
        Camera.main.ScreenToWorldPoint(screenPos);

    
    Vector2 GetCurrentPointerWorld()
    {
        // 마우스(-1) vs 터치(0,1,2…)
        if (activePointer < 0)
            return ScreenToWorld(Input.mousePosition);

#if UNITY_EDITOR    // 에디터·PC 테스트용: 마우스만 사용
        return ScreenToWorld(Input.mousePosition);
#else
    foreach (Touch t in Input.touches)
        if (t.fingerId == activePointer)
            return ScreenToWorld(t.position);

    // 해당 fingerId가 없을 때는 마지막 좌표 그대로 반환
    return lastPointerWorld;
#endif
    }
    void CreateOutlineObject() //자식 스프라이트를 통해 붉은 테두리 형성
    {
        if (outlineSR) return;

        var go = new GameObject("Outline");
        go.transform.SetParent(transform, false);

        outlineSR = go.AddComponent<SpriteRenderer>();
        outlineSR.sprite = sr.sprite;
        outlineSR.color = Color.red;

        outlineSR.sortingLayerID = sr.sortingLayerID;
        outlineSR.sortingOrder = sr.sortingOrder - 1;

        outlineSR.transform.localScale = Vector3.one * 1.04f;
        outlineSR.enabled = false;
    }
}