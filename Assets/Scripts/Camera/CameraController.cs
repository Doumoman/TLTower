using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    Coroutine _centerRoutine;
    [Header("카메라와 함께 Y축으로 움직일 돌 스폰 위치")]
    public Transform[] followWithCamera = new Transform[4];

    private const float DirectionForceReduceRate = 0.900f; // 감속 비율
    private const float DirectionForceMin = 0.001f; // 멈춤 임계값

    [Header("Speed Settings")]
    [SerializeField] private float dragSensitivity = 0.2f;   // 손가락 이동 → 카메라 이동 비율(0.0~1.0)

    private float _pendingCenterY = float.NaN;
    private bool _userMoveInput;        // 드래그 중
    private Vector3 _lastPointerWorldPos; // 직전 포인터 위치(월드)
    private Vector3 _directionForce;      // 이동값 (관성)
    private float _targetCenterY;
    private StoneController _dragTarget;  // 집고 있는 돌
    private CloudController _dragCloudTarget; // 집고 있는 구름
    private Vector3 _baseCamPos;          // 드래그 시작 시점 카메라 위치
    private Camera _cam;
    private readonly List<float> _followYOffset = new(); // followWithCamera[i].y - cam.y 
    // 터치한 지점으로부터 터치 드래그가 이루어지면 시작했던 위치와 드래그된 위치를 빼서 그 위치만큼 드래그가 되도록.

    [Header("Clamp Range")]
    [SerializeField] private float minY = 0f;          // 바닥
    [SerializeField] private float topPadding = 1.5f;  // 돌 위에 보이는 여유 공간


    private void Awake()
    {
        /* 싱글톤 세팅 */
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _cam = GetComponent<Camera>();
    }

    private void Start()
    {
        foreach (var tf in followWithCamera)
        {
            _followYOffset.Add(tf ? tf.position.y - transform.position.y : 0f);
        }
    }

    public void BeginDrag(StoneController s) //외부에서 돌 드래그 상태 입력받기
    {
        _dragTarget = s;
        _baseCamPos = transform.position;
        _directionForce = Vector3.zero; // 관성 초기화
    }

    public void BeginDrag(CloudController c) //외부에서 구름 드래그 상태 입력받기
    {
        _dragCloudTarget = c;
        _baseCamPos = transform.position;
        _directionForce = Vector3.zero; // 관성 초기화
    }

    public void EndDrag() //외부에서 돌 드래그 상태 입력받기
    {
        _dragTarget = null;
        
    }

    private void Update()
    {
        if (StoneController.AnyStoneBeingDragged || CloudController.AnyCloudBeingDragged)
        {
            _directionForce = Vector3.zero;
            UpdateFollowers();               
            return;
        }
        ReduceDirectionForce();
        MoveCamera();
        HandlePointerInput();

        UpdateFollowers(); // followWithCamera 동기화
    }
    private void HandlePointerInput() // 입력 처리 (Y축 전용)
    {
        bool pointerDown;
        bool pointerHeld;
        Vector3 pointerScreenPos;

        // PC, 모바일 버전 입력이 서로 다르므로 일단 이렇게 구분
#if UNITY_EDITOR || UNITY_STANDALONE
        pointerDown = Input.GetMouseButtonDown(0);
        pointerHeld = Input.GetMouseButton(0);
        pointerScreenPos = Input.mousePosition;
#else
        if (Input.touchCount == 0)
        {
            _userMoveInput = false;
            return;
        }
        Touch t          = Input.GetTouch(0);
        pointerDown      = t.phase == TouchPhase.Began;
        pointerHeld      = t.phase == TouchPhase.Moved ||
                           t.phase == TouchPhase.Stationary;
        pointerScreenPos = t.position;
#endif
        Vector3 pointerWorld = _cam.ScreenToWorldPoint(
            new Vector3(pointerScreenPos.x, pointerScreenPos.y, _cam.nearClipPlane));
        pointerWorld.z = 0f;   // 2D

        if (pointerDown)
        {
            _userMoveInput = true;
            _lastPointerWorldPos = pointerWorld;
            _directionForce = Vector3.zero;
            return;
        }

        if (pointerHeld && _userMoveInput)
        {
            float deltaY = (_lastPointerWorldPos.y - pointerWorld.y) * dragSensitivity;
            _directionForce = new Vector3(0f, deltaY, 0f);
            _lastPointerWorldPos = pointerWorld;
        }

        else if (!pointerHeld)
        {
            _userMoveInput = false;
        }
    }

    private void ReduceDirectionForce()
    {
        if (_userMoveInput) return; // 입력 중엔 감속 금지

        _directionForce *= DirectionForceReduceRate;

        if (_directionForce.sqrMagnitude < DirectionForceMin * DirectionForceMin)
            _directionForce = Vector3.zero;
    }
    private float _smoothVelocity;  // SmoothDamp 내부 상태
    float _extraTopOffset;
    private float TopLimit
    {
        get
        {
            float limit;

            /* ① 가을 챕터일 때는 CloudSystem 기준 */
            if (ChapterManager.Instance.chapter.ToString().Contains("autumn"))
            {
                // CloudSystem 싱글톤이 아직 없다면 제한 없음
                if (CloudSystem.Instance == null)
                    return Mathf.Infinity;

                limit = CloudSystem.Instance.HighestJointY + topPadding;
            }
            /* ② 그 외 챕터는 StoneFixer 기준 */
            else
            {
                // StoneFixer 싱글톤이 아직 없다면 제한 없음
                if (StoneFixer.Instance == null)
                    return Mathf.Infinity;

                // Fixed 높이가 있으면 우선, 없으면 Settled 높이라도 사용
                float h = Mathf.Max(StoneFixer.Instance.HighestFixedY,
                                    StoneFixer.Instance.HighestSettledY);
                limit = h + topPadding;
            }

            /* ③ RaiseCameraY() 로 증가한 누적치 반영 */
            return limit + _extraTopOffset;
        }
    }

    private void MoveCamera() //카메라 이동 (Y축 전용)
    {
        if (_directionForce == Vector3.zero) return;

        Vector3 targetPos = transform.position + _directionForce;

        if (TopLimit-5f < 0f)
        {
            minY = 0f;
        }
        else
        {
            minY = TopLimit - 5f;
        }
        targetPos.y = Mathf.Clamp(targetPos.y, minY, TopLimit);

        targetPos.x = transform.position.x;
        targetPos.z = transform.position.z;

        transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
    }

    private void UpdateFollowers() // 돌 스폰 위치 동기화
    {
        for (int i = 0; i < followWithCamera.Length; ++i)
        {
            var tf = followWithCamera[i];
            if (!tf) continue;
            Vector3 p = tf.position;
            p.y = transform.position.y + _followYOffset[i];
            tf.position = p;
        }
    }
    public void CenterOnY(float y, float duration = 0.5f)
    {
        if (_centerRoutine != null)
            StopCoroutine(_centerRoutine);

        if (duration <= 0f)
        {
            Vector3 pos = transform.position;
            pos.y = y;
            transform.position = pos;

            _directionForce = Vector3.zero;   // 관성 제거
            _userMoveInput = false;          // 입력 플래그 리셋
            return;
        }

        _centerRoutine = StartCoroutine(CoCenterY(y, duration));
    }
    IEnumerator CoCenterY(float targetY, float dur)
    {
        float startY = transform.position.y;
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / dur);
            float newY = Mathf.SmoothStep(startY, targetY, ratio);

            Vector3 pos = transform.position;
            pos.y = newY;
            transform.position = pos;

            UpdateFollowers();   // 스폰 위치 실시간 보정
            yield return null;
        }

        Vector3 finalPos = transform.position;
        finalPos.y = targetY;
        transform.position = finalPos;

        _directionForce = Vector3.zero;     // 관성 초기화
        _centerRoutine = null;
    }
    #region 배경 내리기, 카메라 위로 올리기
    [Header("Background Sprites (Y -10f)")]
    [Tooltip("내려줄 첫 번째 배경 스프라이트(Transform)")]
    [SerializeField] private Transform bgSpriteA;
    public void LowerBackgrounds(float amount = -4.5f, float duration = 2f)
    {
        RaiseCameraY();
        // 이미 실행 중이면 중복 방지
        StopCoroutine(nameof(CoLowerBackgrounds));
        StartCoroutine(CoLowerBackgrounds(amount, duration));
    }

    IEnumerator CoLowerBackgrounds(float amount, float dur)
    {
        yield return new WaitForSeconds(1f);
        if (bgSpriteA == null) yield break;

        Vector3 startA = bgSpriteA ? bgSpriteA.position : Vector3.zero;
        Vector3 targetOffset = new(0f, amount, 0f);

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            if (bgSpriteA)
                bgSpriteA.position = Vector3.Lerp(startA, startA + targetOffset, k);

            yield return null;
        }

        // 정확한 도착 보정
        if (bgSpriteA) bgSpriteA.position = startA + targetOffset;
    }

    Coroutine _raiseRoutine;
    public void RaiseCameraY(float amount = 5f, float duration = 3f)
    {
        // 이미 실행 중인 이동 코루틴이 있으면 중지
        if (_raiseRoutine != null)
            StopCoroutine(_raiseRoutine);

        _raiseRoutine = StartCoroutine(CoRaiseCameraY(amount, duration));
    }


    IEnumerator CoRaiseCameraY(float amount, float dur)
    {
        // 1) 목표 Y 계산 → CenterOnY 재사용
        float targetY = transform.position.y + amount;

        // 2) 상한선을 미리 늘려 두면, 이동 중에도 Clamp 에 안 걸림
        _extraTopOffset += amount;

        // 3) 기존 SmoothStep 코루틴 호출
        CenterOnY(targetY, dur);

        // CenterOnY 내부 코루틴이 끝날 때까지 대기
        // (CenterOnY 가 _centerRoutine 에 저장하므로 그걸 추적)
        while (_centerRoutine != null)
            yield return null;

        _raiseRoutine = null;
    }
    #endregion
}
