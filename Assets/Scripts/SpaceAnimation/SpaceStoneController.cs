using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// Floating ↔ Dragging ↔ Snapped 세 상태
public enum SpaceStoneState { Floating, Dragging, Snapping, Snapped }

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
        if (State == SpaceStoneState.Snapped) return;

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

        if (target.expectedIndex != presetIndex) return;

        if (State == SpaceStoneState.Floating &&
            Vector2.Distance(transform.position, target.snapPoint.position) < target.snapRange)
        {
            StartCoroutine(CoSnapToTarget(target));
        }
    }
    IEnumerator CoSnapToTarget(SpaceStoneTarget t)
    {
        State = SpaceStoneState.Snapping;   // 드래그·스냅 중복 방지
        tag = "SnappingStone";

        /* 물리·충돌 끄기 */
        _rb.velocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.isKinematic = true;
        _phys.enabled = false;
        if (_click) _click.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = t.snapPoint.position;
        Quaternion endRot = Quaternion.Euler(0, 0, t.snapRotationZ);

        float dur = Mathf.Max(0.01f, t.snapTime);
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float k = elapsed / dur;

            transform.position = Vector3.Lerp(startPos, endPos, k);
            transform.rotation = Quaternion.Lerp(startRot, endRot, k);

            yield return null;
        }

        /* 최종 값 보정 후 완전히 고정 */
        transform.position = endPos;
        transform.rotation = endRot;

        State = SpaceStoneState.Snapped;
        tag = "PlacedStone";
        Debug.Log($"Snap! stoneIdx={presetIndex}, targetIdx={t.expectedIndex}");
        AnimationManager.Instance?.FlashAndHide(presetIndex);
        NewAnimationManager.Instance?.FlashAndHide(presetIndex);
        AnimationManager.Instance?.NotifyStoneSnapped();
        NewAnimationManager.Instance?.NotifyStoneSnapped();

        SoundManager.Instance.PlaySFX("stone_snap");
        BosalManager.Instance.Speak("SpaceChapterBuilding");
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
