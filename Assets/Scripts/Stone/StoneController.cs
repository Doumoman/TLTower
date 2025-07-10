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
    public int typeId { get; private set; }   // ← 이전 stoneTypeIndex 대체
    public int spriteIndex { get; private set; }
    public void SetTypeId(int id) => typeId = id;
    public int GetSpriteIndexSafe()
    {
        if (spriteIndex >= 0) return spriteIndex;                 // 이미 기록돼 있으면 그대로

        if (Data == null || Data.sprites == null) return 0;       // 예외 대비

        // 현재 SpriteRenderer가 들고 있는 스프라이트가 배열 몇 번째인지 역-검색
        var sr = GetComponent<SpriteRenderer>();
        int idx = System.Array.IndexOf(Data.sprites, sr.sprite);
        spriteIndex = idx < 0 ? 0 : idx;                          // 못 찾으면 0으로
        return spriteIndex;
    }
    public void RefreshCachedRefs()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }
    public void SetSpriteIndex(int idx) => spriteIndex = idx;
    public StoneData Data { get; private set; }
    public static bool AnyStoneBeingDragged { get; private set; }

    public bool DespawnerCheck = false; //StoneDespawnerZone에서 collider가 두 번 적용되는 버그 방지

    [Header("StoneSettled")]
    SpriteRenderer outlineSR;
    [SerializeField] float settleCheckTime = .3f;
    [SerializeField] float settleVelocityEps = .05f;

    float settleTimer = 0f;

    const string DRAG_LAYER = "DraggingStone";   // 드래그 전용 Sorting Layer
    string originalSortingLayer;                 // 복구용 레이어 이름
    int originalOrder;

    [Header("Drag & Rotate")]
    public float holdToRotate = 0.75f;
    public float rotateSpeed = -90f;
    public float moveDeadZone = 0.4f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    PolygonCollider2D physCol;   
    PolygonCollider2D clickCol;
    ChapterManager cm;

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
        //physCol = cols.FirstOrDefault(c => !c.isTrigger);
        //clickCol = cols.FirstOrDefault(c => c.isTrigger);

        if (clickCol == null)
            Debug.LogWarning($"[{name}] Trigger Collider(ClickCol) 가 없습니다!", this);
        if (physCol == null && state != StoneState.Background)
            Debug.LogWarning($"[{name}] Physics Collider(PhysCol) 가 없습니다!", this);
        CreateOutlineObject(); // 빨간색 테두리 형성
        cm = ChapterManager.Instance;
        cm.onChapterChage += OnChapterChanged;
    }
    void OnChapterChanged(object sender, System.EventArgs e)
    {
        if (cm.chapter == chapter.space)
            Destroy(gameObject);
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
                if (stoneTypeIndex == 99)
                {
                    PenaltyManager.Instance.PenaltyStoneSettled(); // 번뇌돌 Settled 보고, PenaltyManager에서 효과 관리
                    Debug.Log("번뇌돌 Settled");
                }
                outlineSR.enabled = true;
                StoneFixer.Instance?.RegisterSettled(this); //Settled 됐다고 StoneFixer 에 보고
                settleTimer = 0;
            }
        }
        if (state == StoneState.Dragging)
        {
            // 아직 전환 전이라면 한 번만 수행
            if (sr.sortingLayerName != DRAG_LAYER)
            {
                originalSortingLayer = sr.sortingLayerName;
                originalOrder = sr.sortingOrder;

                sr.sortingLayerName = DRAG_LAYER;
                sr.sortingOrder = 30_000;          // 충분히 큰 값
                outlineSR.sortingLayerID = sr.sortingLayerID;
                outlineSR.sortingOrder = sr.sortingOrder - 1;
            }
        }
        else   // Dragging 상태가 아닐 때는 원래 레이어로 복귀
        {
            if (sr.sortingLayerName == DRAG_LAYER)
            {
                sr.sortingLayerName = originalSortingLayer;
                sr.sortingOrder = originalOrder;
                outlineSR.sortingLayerID = sr.sortingLayerID;
                outlineSR.sortingOrder = sr.sortingOrder - 1;
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
    public void InitAsBackground(StoneData data, Sprite spr)
    {
        Data = data;
        state = StoneState.Background;

        sr.sprite = spr;
        outlineSR.sprite = spr;
        outlineSR.enabled = false;
        gameObject.tag = "BGStone";

        BuildColliders(spr);

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
    public void InitAsPlayable(StoneData data, Sprite spr)
    {
        AnyStoneBeingDragged = true;
        Data = data;
        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";

        sr.sprite = spr;
        outlineSR.sprite = spr;
        outlineSR.enabled = false;
        sr.color = new Color(1, 1, 1, .5f);

        BuildColliders(spr);

        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.mass = data.mass;
        rb.angularDrag = data.angularDrag;
        rb.gravityScale = 0f;
        rb.isKinematic = true;

        transform.position = new Vector3(transform.position.x,
                                         transform.position.y,
                                         NORMAL_Z);
        sr.sortingOrder = 0;
        outlineSR.sortingOrder = -1;
    }
    void BuildColliders(Sprite spr)
    {
        // 기존 Collider 제거
        foreach (var c in GetComponents<PolygonCollider2D>())
            Destroy(c);

        // 물리용
        physCol = gameObject.AddComponent<PolygonCollider2D>();
        physCol.isTrigger = false;

        // PhysicsMaterial2D 적용
        if (Data && Data.material2D)
            physCol.sharedMaterial = Data.material2D;

        // 클릭용 (조금 키워서 집기 편하게)
        clickCol = gameObject.AddComponent<PolygonCollider2D>();
        clickCol.isTrigger = true;
        clickCol.pathCount = physCol.pathCount;
        for (int i = 0; i < physCol.pathCount; ++i)
        {
            var path = physCol.GetPath(i);
            // 5%씩 확대
            for (int j = 0; j < path.Length; ++j)
                path[j] *= 1.05f;
            clickCol.SetPath(i, path);
        }
    }
    public void SetFixed()
    {
        if (state == StoneState.Fixed) return;

        state = StoneState.Fixed;
        tag = "FixedStone";
        gameObject.layer = LayerMask.NameToLayer("FixedStone");

        foreach (var col in GetComponents<Collider2D>())
            Destroy(col);                         // 모든 2D 콜라이더 파괴

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.bodyType = RigidbodyType2D.Static;
            rb.simulated = false;
        }

        // 스프라이트 렌더러 레이어 변경
        if (TryGetComponent(out SpriteRenderer sr))
            sr.sortingLayerName = "FixedStone";

        if (outlineSR) outlineSR.enabled = false;
    }
    public void Glued()
    {
        state = StoneState.Settled;
        gameObject.tag = "PlacedStone";

        gameObject.layer = LayerMask.NameToLayer("FixedStone");

        if (physCol)
        {
            physCol.enabled = true;
            physCol.isTrigger = false;
        }
        if (!TryGetComponent(out Rigidbody2D rb))
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        if (outlineSR) outlineSR.enabled = false;
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
        if (state == StoneState.Dropping || state == StoneState.Settled){
            StartDragging();
        }
        dragOffset = transform.position - (Vector3)ScreenToWorld(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        AnyStoneBeingDragged = true;
        if (physCol) physCol.enabled = false;
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

        sr.sortingLayerName = originalSortingLayer;
        sr.sortingOrder = originalOrder;
        outlineSR.sortingLayerID = sr.sortingLayerID;
        outlineSR.sortingOrder = sr.sortingOrder - 1;

        state = StoneState.Dropping;
        gameObject.tag = "PlacedStone";

        rb.isKinematic = false;
        rb.gravityScale = 1f;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.Sleep();

        if (physCol) physCol.enabled = true;
        SetGroupPhysicsColliders(true);
        sr.color = Color.white;

        StoneSpawner.Instance.NotifyPlaced(this);
        CameraController.Instance.EndDrag();
        AnyStoneBeingDragged = false;
    }
    

    void StartDragging() //드래그 중 돌의 상태 설정
    {
        AnyStoneBeingDragged = true;
        CameraController.Instance.BeginDrag(this);
        state = StoneState.Dragging;
        gameObject.tag = "DraggingStone";

        sr.sortingLayerName = DRAG_LAYER;
        sr.sortingOrder = 30_000;         // flower(3)보다 훨씬 큰 값
        outlineSR.sortingLayerID = sr.sortingLayerID;
        outlineSR.sortingOrder = sr.sortingOrder - 1;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        if (physCol) physCol.enabled = false;
        SetGroupPhysicsColliders(false);

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
    void SetGroupPhysicsColliders(bool enabled)
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
            if (!col.isTrigger)           // 클릭용 Trigger 는 그대로 두고
                col.enabled = enabled;
    }

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
    public void HideOutline() => outlineSR.enabled = false;
    public void ShowOutline(Color c) { outlineSR.color = c; outlineSR.enabled = true; }

    void OnDestroy()
    {
        if (cm) cm.onChapterChage -= OnChapterChanged;
        if (this.CompareTag("PlacedStone")) ChapterManager.Instance.RemoveCount();
    }
}