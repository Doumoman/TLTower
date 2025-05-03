using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("카메라와 함께 Y축으로 움직일 돌 스폰 위치")]
    public Transform[] followWithCamera = new Transform[4];

    private const float DirectionForceReduceRate = 0.900f; // 감속 비율
    private const float DirectionForceMin = 0.001f; // 멈춤 임계값

    [Header("Speed Settings")]
    [SerializeField] private float dragSensitivity = 0.2f;   // 손가락 이동 → 카메라 이동 비율(0.0~1.0)
    [SerializeField] private float followSmooth = 0.15f;  // 돌 부드럽게 따라가기 (돌 집을때)

    private bool _userMoveInput;        // 드래그 중
    private Vector3 _lastPointerWorldPos; // 직전 포인터 위치(월드)
    private Vector3 _directionForce;      // 이동값 (관성)

    private StoneController _dragTarget;  // 집고 있는 돌
    private Vector3 _baseCamPos;          // 드래그 시작 시점 카메라 위치
    private Camera _cam;
    private readonly List<float> _followYOffset = new(); // followWithCamera[i].y - cam.y 
    // 터치한 지점으로부터 터치 드래그가 이루어지면 시작했던 위치와 드래그된 위치를 빼서 그 위치만큼 드래그가 되도록.

    [Header("Clamp Range")]
    [SerializeField] private float minY = 0f;

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
        if (_dragTarget == null)
        {
            HandlePointerInput();   // 손가락/마우스 입력
            ReduceDirectionForce(); // 관성 감속
        }
        else
        {
            FollowDragTarget();     // 돌 따라가기
        }
        MoveCamera();    // 실제 카메라 이동
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
   
    private void FollowDragTarget() //드래그로 잡은 돌 따라가기

    {
        if (!_dragTarget) return;

        float targetY = _dragTarget.transform.position.y;      // 목표 Y
        float newY = Mathf.SmoothDamp(transform.position.y, // 현 Y, 목표 Y
                                         targetY,
                                         ref _smoothVelocity,
                                         followSmooth);

        _directionForce = new Vector3(0f, newY - transform.position.y, 0f);
    }

    private void MoveCamera() //카메라 이동 (Y축 전용)
    {
        if (_directionForce == Vector3.zero) return;

        Vector3 targetPos = transform.position + _directionForce;

        // 위쪽 한계를 StoneFixer의 가장 높은 돌 로 설정 
        float topLimit = StoneFixer.Instance              // 싱글톤이 살아있고
                       ? StoneFixer.Instance.HighestSettledY // 가장 높은 Settled 돌 Y
                       : Mathf.Infinity;                   // (없으면 무한)

        targetPos.y = Mathf.Clamp(targetPos.y, minY, topLimit);

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
}