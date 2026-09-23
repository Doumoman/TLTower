using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/*
구름 상태 변경 및 조작 기능.
구름의 콜라이더들 조정
*/

public enum CloudState { flow, Dragging, Dropped, ReturningToFlow };
[RequireComponent(typeof(Rigidbody2D))]
public class CloudController : MonoBehaviour,
                               IPointerDownHandler,
                               IPointerUpHandler,
                               IDragHandler
{
    private const float AutumnCircleCorrectionDuration = 0.15f;
    private const float AutumnCircleClearance = 0.025f;
    private const float AutumnCircleSearchStep = 0.05f;
    private const int AutumnCircleSearchStepCount = 160;

    private readonly struct TransformSnapshot
    {
        public readonly Transform Transform;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;
        public readonly int Layer;
        public readonly string Tag;
        public readonly bool ActiveSelf;

        public TransformSnapshot(Transform target)
        {
            Transform = target;
            LocalPosition = target.localPosition;
            LocalRotation = target.localRotation;
            LocalScale = target.localScale;
            Layer = target.gameObject.layer;
            Tag = target.gameObject.tag;
            ActiveSelf = target.gameObject.activeSelf;
        }
    }

    Rigidbody2D rb;
    Rigidbody2D[] rbChildren;
    Collider2D col;
    Collider2D[] colChildren;
    SpriteRenderer sr;
    JointMakerPhysics[] jointPhysics;
    TransformSnapshot[] childTransformSnapshots;
    
    CloudState state = CloudState.flow;
    
    //상호작용 체킹용
    JointMaker jm;
    [HideInInspector] public bool crush = false;

    //구름 반발력 용도
    Collider2D separator;
    Coroutine co = null;
    Coroutine flowTransitionCoroutine = null;

    float timer = 0f; //상호작용 없으면 돌아가는 용도

    private static CloudController draggedCloud;
    public static bool AnyCloudBeingDragged => draggedCloud != null;
    //Vector3 dragOffset;
    Vector2 holdStartPos;
    Vector2 lastPointerWorld;
    Vector2 lastValidPlacementPosition;
    float holdTimer;
    bool isRotating;

    [Header("Settings")]
    public float waitTime = 1f;
    public float flowSpeed = -1;
    [Tooltip("구름 분리시 검사 실행 쿨타임")]public float checktime = 0.3f;
    public Color baseColor = new Color32(0x6F, 0x54, 0x4D, 0xFF);
    public Color flowColor;
    public Color dragColor;

    [Header("Flow Color Transition")]
    [Tooltip("독립된 구름이 flow 상태로 돌아가기 전에 flowColor로 서서히 바뀌는 시간")]
    [SerializeField, Min(0f)] private float flowColorTransitionDuration = 0.65f;

    [Header("Drag & Rotate")]
    public float holdToRotate = 0.75f;
    public float rotateSpeed = -90f;
    public float moveDeadZone = 0.4f;
    private readonly Collider2D[] separateOverlapResults = new Collider2D[1];
    private readonly HashSet<Collider2D> ignoredCollisionTargets = new HashSet<Collider2D>();
    private Camera inputCamera;
    private Color authoredBaseColor;
    private float authoredFlowSpeed;
    private Quaternion authoredRootRotation;
    private Vector3 authoredRootScale;
    private int authoredRootLayer;
    private string authoredRootTag;
    private bool authoredRendererEnabled;
    private int authoredSortingOrder;
    private SkyCloudPool cloudPool;
    private bool isInPool;
    private bool isDespawning;

    private void Awake()
    {
        authoredBaseColor = baseColor;
        authoredFlowSpeed = flowSpeed;
        authoredRootRotation = transform.localRotation;
        authoredRootScale = transform.localScale;
        authoredRootLayer = gameObject.layer;
        authoredRootTag = gameObject.tag;
        lastValidPlacementPosition = transform.position;

        inputCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rbChildren = GetComponentsInChildren<Rigidbody2D>(true)
            .Where(c => c.gameObject != gameObject)
            .ToArray();

        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>(true)
            .Where(c => c.gameObject != gameObject)
            .ToArray();
        separator = childColliders.FirstOrDefault(
            c => c.gameObject.layer == LayerMask.NameToLayer("CloudSeparate"));
        colChildren = childColliders.Where(c => c != separator).ToArray();

        sr = GetComponent<SpriteRenderer>();
        authoredRendererEnabled = sr.enabled;
        authoredSortingOrder = sr.sortingOrder;
        jointPhysics = GetComponentsInChildren<JointMakerPhysics>(true);
        childTransformSnapshots = GetComponentsInChildren<Transform>(true)
            .Where(t => t != transform)
            .Select(t => new TransformSnapshot(t))
            .ToArray();
    }

    internal bool IsPoolCleanupComplete =>
        GetComponent<JointMaker>() == null &&
        GetComponentsInChildren<FixedJoint2D>(true).Length == 0;

    internal void InitializePool(SkyCloudPool owner)
    {
        cloudPool = owner;
    }

    internal void PrepareForSpawn(
        Vector3 spawnPosition,
        int directionalForce,
        float minBaseColorNoise,
        float maxBaseColorNoise)
    {
        StopAllCoroutines();
        co = null;
        flowTransitionCoroutine = null;
        StopJointPhysicsCoroutines();
        CancelDragInteraction();
        RestoreIgnoredCollisions();

        SetBodiesSimulated(false);
        transform.SetParent(null, false);
        transform.SetPositionAndRotation(spawnPosition, authoredRootRotation);
        transform.localScale = authoredRootScale;
        gameObject.layer = authoredRootLayer;
        gameObject.tag = authoredRootTag;

        foreach (TransformSnapshot snapshot in childTransformSnapshots)
        {
            if (!snapshot.Transform) continue;

            snapshot.Transform.localPosition = snapshot.LocalPosition;
            snapshot.Transform.localRotation = snapshot.LocalRotation;
            snapshot.Transform.localScale = snapshot.LocalScale;
            snapshot.Transform.gameObject.layer = snapshot.Layer;
            snapshot.Transform.gameObject.tag = snapshot.Tag;
            snapshot.Transform.gameObject.SetActive(snapshot.ActiveSelf);
        }

        foreach (JointMakerPhysics physics in jointPhysics)
        {
            if (physics) physics.enabled = true;
        }

        baseColor = authoredBaseColor;
        ApplyBaseColorNoise(minBaseColorNoise, maxBaseColorNoise);
        flowSpeed = authoredFlowSpeed * (directionalForce < 0 ? -1f : 1f);

        activePointer = -1;
        holdStartPos = Vector2.zero;
        lastPointerWorld = Vector2.zero;
        lastValidPlacementPosition = spawnPosition;
        holdTimer = 0f;
        isRotating = false;
        timer = 0f;
        crush = false;
        jm = null;
        isDespawning = false;
        isInPool = false;
        inputCamera = Camera.main;

        sr.enabled = authoredRendererEnabled;
        sr.sortingOrder = authoredSortingOrder;
        Flow();
        gameObject.SetActive(true);
    }

    internal void StoreInPool(Transform poolRoot)
    {
        StopAllCoroutines();
        co = null;
        flowTransitionCoroutine = null;
        StopJointPhysicsCoroutines();
        CancelDragInteraction();
        RestoreIgnoredCollisions();

        FreezePhysics();
        DisableInteractionColliders();

        state = CloudState.flow;
        timer = 0f;
        crush = false;
        jm = null;
        gameObject.layer = authoredRootLayer;
        gameObject.tag = authoredRootTag;
        sr.sortingOrder = authoredSortingOrder;

        isInPool = true;
        transform.SetParent(poolRoot, false);
        gameObject.SetActive(false);
    }

    private void StopJointPhysicsCoroutines()
    {
        foreach (JointMakerPhysics physics in jointPhysics)
        {
            if (!physics) continue;
            physics.StopAllCoroutines();
            physics.enabled = false;
        }
    }

    private void SetBodiesSimulated(bool simulated)
    {
        rb.simulated = simulated;
        foreach (Rigidbody2D childBody in rbChildren)
        {
            if (childBody) childBody.simulated = simulated;
        }
    }

    private void SetBonePhysicsEnabled(bool enabled)
    {
        foreach (Rigidbody2D childBody in rbChildren)
        {
            if (!childBody) continue;

            if (!enabled)
            {
                if (childBody.bodyType != RigidbodyType2D.Static)
                {
                    childBody.linearVelocity = Vector2.zero;
                    childBody.angularVelocity = 0f;
                }

                childBody.gravityScale = 0f;
                // Transform/SpriteSkin은 유지하고, 연결된 Collider와 Joint만 물리계에서 제외한다.
                childBody.simulated = false;
                childBody.bodyType = RigidbodyType2D.Static;
                continue;
            }

            Vector2 visualPosition = childBody.transform.position;
            float visualRotation = childBody.transform.eulerAngles.z;

            childBody.bodyType = RigidbodyType2D.Dynamic;
            childBody.position = visualPosition;
            childBody.rotation = visualRotation;
            childBody.linearVelocity = Vector2.zero;
            childBody.angularVelocity = 0f;
            childBody.gravityScale = 0f;
            childBody.simulated = true;
        }
    }

    private void FreezePhysics()
    {
        if (rb.bodyType != RigidbodyType2D.Static)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = false;
        SetBonePhysicsEnabled(false);
    }

    private void DisableInteractionColliders()
    {
        col.enabled = false;
        separator.enabled = false;
        foreach (Collider2D childCollider in colChildren)
        {
            if (childCollider) childCollider.enabled = false;
        }
    }

    public void ApplyBaseColorNoise(float minPercent, float maxPercent)
    {
        float min = Mathf.Max(0f, Mathf.Min(minPercent, maxPercent));
        float max = Mathf.Max(min, Mathf.Max(minPercent, maxPercent));

        Color.RGBToHSV(authoredBaseColor, out float hue, out float saturation, out float brightness);

        saturation = Mathf.Clamp01(saturation * (1f + RandomSignedPercent(min, max)));
        brightness = Mathf.Clamp01(brightness * (1f + RandomSignedPercent(min, max)));

        baseColor = Color.HSVToRGB(hue, saturation, brightness);
        baseColor.a = authoredBaseColor.a;
    }

    private static float RandomSignedPercent(float min, float max)
    {
        float amount = Random.Range(min, max);
        return Random.value < 0.5f ? -amount : amount;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 풀 밖에서 직접 생성된 구름과 기존 테스트 장면의 동작은 유지한다.
        if (cloudPool == null) Flow();
    }

    void Flow()
    {
        timer = 0f;
        if (rb.bodyType != RigidbodyType2D.Static)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        rb.simulated = true;
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Static;
        SetBonePhysicsEnabled(false);
        
        col.enabled = true;
        separator.isTrigger = true;
        separator.enabled = true;
        col.isTrigger = true;
        foreach (var col in colChildren)
        {
            col.enabled = false;
            col.isTrigger = false;
        }

        gameObject.tag = "FlowCloud";
        sr.sortingLayerName = "FlowCloud";
        sr.color = flowColor;

        state = CloudState.flow;
    }

    // Update is called once per frame
    void Update()
    {
        if (isInPool || isDespawning) return;

        if (state == CloudState.Dragging && !isRotating)
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
        if (isRotating) transform.Rotate(new Vector3(0, 0, rotateSpeed) * Time.deltaTime);
        if (state == CloudState.flow)
        {
            Vector3 moveVetor = new Vector2(flowSpeed, 0);
            transform.position += moveVetor * Time.deltaTime;
        }
        if (state == CloudState.Dropped)
        {
            //충돌, 분리, 접착(상호작용) 조건 확인
            if (co == null && jm == null && !crush) timer += Time.deltaTime;
            else
            {
                timer = 0f;
                crush = false;
            }
            if (timer > waitTime)
            {
                StartFlowTransition();
            }
        }
        if (transform.position.x < -15 || transform.position.x > 15)
            ReturnToPoolImmediately(reexamineConnections: true);
    }

    private void StartFlowTransition()
    {
        if (flowTransitionCoroutine != null) return;

        state = CloudState.ReturningToFlow;
        flowTransitionCoroutine = StartCoroutine(FadeToFlowColor());
    }

    private IEnumerator FadeToFlowColor()
    {
        Color startColor = sr.color;
        float elapsed = 0f;

        while (elapsed < flowColorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = flowColorTransitionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / flowColorTransitionDuration);
            sr.color = Color.Lerp(startColor, flowColor, progress);
            yield return null;
        }

        sr.color = flowColor;
        flowTransitionCoroutine = null;
        Flow();
    }

    void CheckOverlap()
    {
        JointMakerPhysics[] jmps = GetComponentsInChildren<JointMakerPhysics>();
        foreach (JointMakerPhysics jmp in jmps)
        {
            jmp.CheckOverlap();
        }
        if (!TryGetComponent<JointMaker>(out jm))
        {
            StartSeparate();
        }
    }

    public void StartSeparate()
    {
        if (state != CloudState.Dropped) return;
        if (co != null) return;
        co = StartCoroutine(Separate());
    }


    //separator랑 겹치는게 있으면 분리 실행(참고로jm파괴는 아직 안됐을 시점)
    public IEnumerator Separate() 
    {
        yield return null;
        WaitForSeconds checkDelay = new WaitForSeconds(checktime);
        LayerMask mask = LayerMask.GetMask("CloudSeparate", "JointedCloud");
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(mask);

        int count = separator.Overlap(filter, separateOverlapResults);
        if (count > 0)
        {
            foreach (Collider2D col in colChildren) col.isTrigger = true;
            separator.isTrigger = false;
            if (separateOverlapResults[0].transform.parent.TryGetComponent<CloudController>(out CloudController c)) c.StartSeparate();

            //separator랑 겹치는 게 없을 때 까지 콜라이더 활성화(튕기기)실행
            while (true)
            {
                yield return checkDelay;

                count = separator.Overlap(filter, separateOverlapResults);
                if (count > 0)
                {
                    if (separateOverlapResults[0].transform.parent.TryGetComponent<JointMaker>(out JointMaker _)) //JointMaker가 있는 대상이면 조인트용 겹침검사 실행
                    {
                        CheckOverlap();
                        break;
                    }
                    else if (separateOverlapResults[0].transform.parent.TryGetComponent<CloudController>(out c)) c.StartSeparate();
                }
                else break;
            }

            //복구
            foreach (Collider2D col in colChildren) col.isTrigger = false;
            separator.isTrigger = true;
        }
        co = null;
    }

    //조인트된 구름의 상태 설정
    public void Jointed()
    {
        separator.gameObject.layer = LayerMask.NameToLayer("JointedCloud");
        foreach (Collider2D col in colChildren) col.gameObject.layer = LayerMask.NameToLayer("CloudChild");
    }

    //조인트 해제된 구름의 상태 설정
    public void DisJointed()
    {
        separator.gameObject.layer = LayerMask.NameToLayer("CloudSeparate");
        foreach (Collider2D col in colChildren) col.gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public void Disappear(bool reexamineConnections = true)
    {
        if (!BeginDespawn(reexamineConnections)) return;

        StartCoroutine(FadeOutAndReturn());
    }

    private IEnumerator FadeOutAndReturn()
    {
        Color c = sr.color;
        float current = c.a;
        float timer = 0;

        while (true)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(current, 0, timer);
            sr.color = c;

            if (c.a < 0.05f) break;
            yield return null;
        }

        CompleteDespawn();
    }

    private void ReturnToPoolImmediately(bool reexamineConnections)
    {
        if (!BeginDespawn(reexamineConnections)) return;
        CompleteDespawn();
    }

    private bool BeginDespawn(bool reexamineConnections)
    {
        if (isInPool || isDespawning) return false;

        isDespawning = true;
        StopAllCoroutines();
        co = null;
        flowTransitionCoroutine = null;
        StopJointPhysicsCoroutines();
        CancelDragInteraction();
        RestoreIgnoredCollisions();

        state = CloudState.ReturningToFlow;
        FreezePhysics();
        DisableInteractionColliders();

        JointMaker dynamicJointMaker = GetComponent<JointMaker>();
        if (dynamicJointMaker)
        {
            dynamicJointMaker.PrepareForDespawn();
            if (CloudSystem.Instance)
                CloudSystem.Instance.NotifyCloudDespawned(dynamicJointMaker, reexamineConnections);

            Destroy(dynamicJointMaker);
        }

        // 연결 생성 도중 반환되는 예외 상황도 정리한다. 프리팹에는 FixedJoint2D가 없다.
        foreach (FixedJoint2D joint in GetComponentsInChildren<FixedJoint2D>(true))
        {
            if (!joint || !joint.enabled) continue;
            joint.enabled = false;
            Destroy(joint);
        }

        jm = null;
        return true;
    }

    private void CompleteDespawn()
    {
        if (cloudPool != null)
        {
            cloudPool.Release(this);
            return;
        }

        Destroy(gameObject);
    }

    //구름의 bone들과 특정 오브젝트(체크포인트)와의 충돌 무시
    public void IgnoreCollision(GameObject go)
    {
        if (!go || !go.TryGetComponent(out Collider2D targetCollider)) return;

        ignoredCollisionTargets.Add(targetCollider);
        foreach (Collider2D childCollider in colChildren)
        {
            Physics2D.IgnoreCollision(childCollider, targetCollider, true);
        }
    }

    private void RestoreIgnoredCollisions()
    {
        foreach (Collider2D targetCollider in ignoredCollisionTargets)
        {
            if (!targetCollider) continue;

            foreach (Collider2D childCollider in colChildren)
            {
                if (childCollider)
                    Physics2D.IgnoreCollision(childCollider, targetCollider, false);
            }
        }

        ignoredCollisionTargets.Clear();
    }

    private void CancelDragInteraction()
    {
        bool ownedDrag = draggedCloud == this;
        if ((ownedDrag || isRotating) && SoundManager.Instance)
            SoundManager.Instance.StopLoop("cloud_rotate");

        activePointer = -1;
        holdTimer = 0f;
        holdStartPos = Vector2.zero;
        lastPointerWorld = Vector2.zero;
        isRotating = false;

        if (!ownedDrag) return;

        draggedCloud = null;
        if (CameraController.Instance) CameraController.Instance.EndDrag();
    }


    int activePointer = -1;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isInPool || isDespawning) return;
        if (activePointer != -1) return;
        if (draggedCloud && draggedCloud != this) return;

        activePointer = eventData.pointerId;
        SoundManager.Instance.PlaySFX("cloud_select");

        if (this.TryGetComponent<JointMaker>(out jm))
            jm.Detach();
        StartDragging();
        //dragOffset = transform.position - (Vector3)ScreenToWorld(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointer) return;
        if (state != CloudState.Dragging) return;
        //if (rb.bodyType != RigidbodyType2D.Kinematic) rb.bodyType = RigidbodyType2D.Kinematic;

        Vector2 mouseWorld = ScreenToWorld(eventData.position);

        // 회전 중
        if (isRotating)
        {
            if (Vector2.Distance(mouseWorld, holdStartPos) >= moveDeadZone)
            {
                isRotating = false;
                SoundManager.Instance.StopLoop("cloud_rotate");
                //rb.angularVelocity = 0;
                //dragOffset = transform.position - (Vector3)mouseWorld;
            }
            return;
        }

        // 일반 드래그 
        //rb.MovePosition((Vector3)mouseWorld + dragOffset);
        transform.position = mouseWorld;

        holdTimer += Time.deltaTime;
        if (holdTimer >= holdToRotate)
        {
            isRotating = true;
            holdTimer = 0;
            holdStartPos = mouseWorld;
            //rb.angularVelocity = rotateSpeed;
            SoundManager.Instance.PlayLoop("cloud_rotate");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SoundManager.Instance.StopLoop("cloud_rotate");
        isRotating = false;

        if (eventData.pointerId != activePointer) return;
        activePointer = -1;

        if (state != CloudState.Dragging) return;

        state = CloudState.Dropped;
        gameObject.tag = "Cloud";
        SoundManager.Instance.PlaySFX("cloud_deselect");

        sr.color = baseColor; // 생성 시 정해진 이 구름만의 고유 색상으로 복구
        sr.sortingLayerName = "Default";
        if (draggedCloud == this) draggedCloud = null;
        CameraController.Instance.EndDrag();
        separator.gameObject.layer = LayerMask.NameToLayer("CloudSeparate");
        StartCoroutine(FinalizeDrop());
    }


    private IEnumerator FinalizeDrop()
    {
        PrepareDropCorrectionCheck();
        Physics2D.SyncTransforms();

        if (TryGetAutumnCircleCorrectionTarget(out Vector2 correctionTarget))
            yield return MoveToCorrectedPosition(correctionTarget);

        if (state != CloudState.Dropped || isInPool || isDespawning) yield break;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        SetBonePhysicsEnabled(true);
        rb.Sleep();

        foreach (Collider2D childCollider in colChildren)
        {
            childCollider.enabled = true;
            childCollider.isTrigger = false;
        }

        lastValidPlacementPosition = transform.position;
        CheckOverlap();
    }

    private void PrepareDropCorrectionCheck()
    {
        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Static;
        rb.position = transform.position;
        rb.rotation = transform.eulerAngles.z;

        foreach (Rigidbody2D childBody in rbChildren)
        {
            if (!childBody) continue;

            Vector2 visualPosition = childBody.transform.position;
            float visualRotation = childBody.transform.eulerAngles.z;
            if (childBody.bodyType != RigidbodyType2D.Static)
            {
                childBody.linearVelocity = Vector2.zero;
                childBody.angularVelocity = 0f;
            }

            childBody.bodyType = RigidbodyType2D.Static;
            childBody.position = visualPosition;
            childBody.rotation = visualRotation;
            childBody.gravityScale = 0f;
            childBody.simulated = true;
        }

        foreach (Collider2D childCollider in colChildren)
        {
            childCollider.enabled = true;
            childCollider.isTrigger = true;
        }
    }

    private bool TryGetAutumnCircleCorrectionTarget(out Vector2 targetPosition)
    {
        targetPosition = transform.position;
        AutumnIntermediateCircle[] markers = FindObjectsByType<AutumnIntermediateCircle>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        if (markers.Length == 0) return false;

        List<CircleCollider2D> circles = new List<CircleCollider2D>(markers.Length);
        CircleCollider2D overlappingCircle = null;
        float closestCircleDistance = float.PositiveInfinity;
        Bounds cloudBounds = GetCloudBounds();

        foreach (AutumnIntermediateCircle marker in markers)
        {
            if (!marker.TryGetComponent(out CircleCollider2D circle) || !circle.enabled) continue;

            circles.Add(circle);
            if (!OverlapsCircle(circle, 0f)) continue;

            float centerDistance = ((Vector2)cloudBounds.center - (Vector2)circle.bounds.center).sqrMagnitude;
            if (centerDistance >= closestCircleDistance) continue;

            closestCircleDistance = centerDistance;
            overlappingCircle = circle;
        }

        if (!overlappingCircle) return false;

        Vector2 startPosition = transform.position;
        Vector2 direction = (Vector2)cloudBounds.center - (Vector2)overlappingCircle.bounds.center;
        if (direction.sqrMagnitude < 0.0001f)
            direction = lastValidPlacementPosition - (Vector2)overlappingCircle.bounds.center;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;
        direction.Normalize();

        float lowerDistance = 0f;
        float upperDistance = 0f;
        bool foundTarget = false;

        for (int i = 1; i <= AutumnCircleSearchStepCount; i++)
        {
            float distance = i * AutumnCircleSearchStep;
            transform.position = startPosition + direction * distance;
            Physics2D.SyncTransforms();

            if (!OverlapsAnyCircle(circles, AutumnCircleClearance))
            {
                upperDistance = distance;
                foundTarget = true;
                break;
            }

            lowerDistance = distance;
        }

        if (foundTarget)
        {
            for (int i = 0; i < 6; i++)
            {
                float middleDistance = (lowerDistance + upperDistance) * 0.5f;
                transform.position = startPosition + direction * middleDistance;
                Physics2D.SyncTransforms();

                if (OverlapsAnyCircle(circles, AutumnCircleClearance))
                    lowerDistance = middleDistance;
                else
                    upperDistance = middleDistance;
            }

            targetPosition = startPosition + direction * upperDistance;
        }
        else
        {
            targetPosition = lastValidPlacementPosition;
        }

        transform.position = startPosition;
        Physics2D.SyncTransforms();
        return true;
    }

    private Bounds GetCloudBounds()
    {
        Bounds bounds = separator.bounds;
        foreach (Collider2D childCollider in colChildren)
            bounds.Encapsulate(childCollider.bounds);
        return bounds;
    }

    private bool OverlapsAnyCircle(List<CircleCollider2D> circles, float clearance)
    {
        foreach (CircleCollider2D circle in circles)
        {
            if (OverlapsCircle(circle, clearance)) return true;
        }

        return false;
    }

    private bool OverlapsCircle(CircleCollider2D circle, float clearance)
    {
        if (IsWithinClearance(separator, circle, clearance)) return true;

        foreach (Collider2D childCollider in colChildren)
        {
            if (IsWithinClearance(childCollider, circle, clearance)) return true;
        }

        return false;
    }

    private static bool IsWithinClearance(Collider2D cloudCollider, CircleCollider2D circle, float clearance)
    {
        ColliderDistance2D distance = cloudCollider.Distance(circle);
        return distance.isValid && (distance.isOverlapped || distance.distance < clearance);
    }

    private IEnumerator MoveToCorrectedPosition(Vector2 targetPosition)
    {
        Vector2 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < AutumnCircleCorrectionDuration)
        {
            if (state != CloudState.Dropped || isInPool || isDespawning) yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / AutumnCircleCorrectionDuration);
            progress = Mathf.SmoothStep(0f, 1f, progress);
            transform.position = Vector2.Lerp(startPosition, targetPosition, progress);
            Physics2D.SyncTransforms();
            yield return null;
        }

        transform.position = targetPosition;
        Physics2D.SyncTransforms();
    }

    void StartDragging() //드래그 중 돌의 상태 설정
    {
        StopAllCoroutines();
        co = null;
        flowTransitionCoroutine = null;
        timer = 0f;
        draggedCloud = this;
        CameraController.Instance.BeginDrag(this);

        state = CloudState.Dragging;
        gameObject.tag = "DraggingCloud";
        //rb.velocity = Vector2.zero;
        //rb.angularVelocity = 0f;
        //foreach (Rigidbody2D rb in rbChildren) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; }
        separator.isTrigger = true;
        foreach (Collider2D col in colChildren) col.enabled = false;

        rb.bodyType = RigidbodyType2D.Static;
        // Flow에서 진입하면 비활성 상태를 유지하고, 연결된 구름은 기존 Joint 동작을 보존한다.
        foreach (Rigidbody2D childBody in rbChildren)
        {
            if (childBody) childBody.bodyType = RigidbodyType2D.Static;
        }

        //dragOffset = transform.position - (Vector3)ScreenToWorld();
        holdTimer = 0;
        holdStartPos = ScreenToWorld();
        isRotating = false;

        sr.color = dragColor;
        sr.sortingLayerName = "DraggingStone";
    }

    Vector2 ScreenToWorld() => ScreenToWorld(Input.mousePosition);

    Vector2 ScreenToWorld(Vector2 screenPos)
    {
        if (!inputCamera) inputCamera = Camera.main;
        return inputCamera.ScreenToWorldPoint(screenPos);
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

    private void OnDestroy()
    {
        StopAllCoroutines();
        CancelDragInteraction();
        RestoreIgnoredCollisions();
    }
}
