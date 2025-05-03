using UnityEngine;
using System.Linq;


public enum StoneState { Background, Dragging, Dropping, Settled, Fixed }

[RequireComponent(typeof(SpriteRenderer))]
public class StoneController : MonoBehaviour
{
    const int STUB_ORDER = 10_000;   // Stub(Background) 최상단 출력용
    const float STUB_Z = -1f;    // 카메라 쪽(z‑축 ‑값)으로 살짝 당김
    const float NORMAL_Z = 0f;     // 게임 중 돌의 기본 z

    public StoneState state = StoneState.Background;
    public int stoneTypeIndex = 0;

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
    }

    public void InitAsBackground(int typeIdx, Sprite sprite) // 돌 생성되었을때 설정
    {
        state = StoneState.Background;
        stoneTypeIndex = typeIdx;
        sr.sprite = sprite;
        outlineSR.sprite = sprite; // 테두리 스프라이트
        outlineSR.enabled = false;
        gameObject.tag = "BGStone";

        // 물리 필요 없으니 비활성화
        if (rb) rb.simulated = false;
        if (physCol) physCol.enabled = false;

        sr.color = Color.white;
        sr.sortingOrder = STUB_ORDER;
        outlineSR.sortingOrder = STUB_ORDER - 1;
        transform.position = new Vector3(transform.position.x,
                                               transform.position.y,
                                               STUB_Z);   // z 앞으로
    }

    // Playable(Dragging 시작)
    public void InitAsPlayable(Sprite sprite, float mass = 1f)
    {
        sr.sprite = sprite;
        outlineSR.sprite = sprite; // 테두리 스프라이트             
        outlineSR.enabled = false;

        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";

        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.mass = mass;
        rb.gravityScale = 0;
        rb.isKinematic = true;

        if (!physCol)
            Debug.LogError($"[{name}] 물리 Collider 가 없습니다! 프리팹에 미리 추가하세요.", this);
        else
            physCol.enabled = false;   // 드래그 중 OFF

        dragOffset = Vector3.zero;
        holdTimer = 0;
        isRotating = false;
        sr.color = new Color(1, 1, 1, 0.5f);

        // Stub 때 써 둔 z/정렬 값을 정상 값으로 복구
        transform.position = new Vector3(transform.position.x,
                                         transform.position.y,
                                         NORMAL_Z);       // 물리 충돌용 동일 z
        sr.sortingOrder = 0;                      // BringToFront 로 재조정
        outlineSR.sortingOrder = sr.sortingOrder - 1;
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

    void OnMouseDown()
    {
        if (state == StoneState.Background)     // Stub → Playable
        {
            StoneSpawner.Instance.SpawnPlayableAndBeginDrag(this);
            StoneSpawner.Instance.RemoveStub(this);
            CameraController.Instance.BeginDrag(this);
            return;
        }
        if (state == StoneState.Dropping){
            StartDragging();
            CameraController.Instance.BeginDrag(this);
        }
    }

    void OnMouseDrag()
    {
        if (state != StoneState.Dragging) return;

        Vector2 mouseWorld = ScreenToWorld();

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

    void OnMouseUp()
    {
        if (state != StoneState.Dragging) return;

        state = StoneState.Dropping;
        gameObject.tag = "PlacedStone";

        rb.isKinematic = false;
        rb.gravityScale = 1f;
        rb.angularVelocity = 0;

        if (physCol) physCol.enabled = true;
        sr.color = Color.white;

        StoneSpawner.Instance.NotifyPlaced(this);
        StoneSpawner.Instance.ScheduleRandomStone(4f);
        CameraController.Instance.EndDrag();
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

    void StartDragging() //드래그 중 돌의 상태 설정
    {
        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";

        dragOffset = transform.position - (Vector3)ScreenToWorld();
        holdTimer = 0;
        holdStartPos = ScreenToWorld();
        isRotating = false;

        rb.isKinematic = true;
        rb.gravityScale = 0;
        if (physCol) physCol.enabled = false;

        sr.color = new Color(1, 1, 1, 0.5f);
    }

    Vector2 ScreenToWorld() =>
        Camera.main.ScreenToWorldPoint(Input.mousePosition);

    void FixedUpdate()
    {
        if (isRotating && state == StoneState.Dragging)
            rb.angularVelocity = rotateSpeed;
    }
}