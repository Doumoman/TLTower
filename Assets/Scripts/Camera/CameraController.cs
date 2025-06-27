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

    public void EndDrag() //외부에서 돌 드래그 상태 입력받기
    {
        _dragTarget = null;
        
    }

    private void Update()
    {
        if (StoneController.AnyStoneBeingDragged)
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

    private float TopLimit
    {
        get
        {
            // StoneFixer 싱글톤이 아직 없다면 제한 없음
            if (StoneFixer.Instance == null)
                return Mathf.Infinity;

            // Fixed 높이가 있으면 우선, 없으면 Settled 높이라도 사용
            float h = Mathf.Max(StoneFixer.Instance.HighestFixedY,
                                StoneFixer.Instance.HighestSettledY);

            return h + topPadding;
        }
    }

    private void MoveCamera() //카메라 이동 (Y축 전용)
    {
        if (_directionForce == Vector3.zero) return;

        Vector3 targetPos = transform.position + _directionForce;


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
}
