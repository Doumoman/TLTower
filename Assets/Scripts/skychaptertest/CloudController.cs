using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public enum CloudState { flow, Dragging, Dropped };
[RequireComponent(typeof(Rigidbody2D))]
public class CloudController : MonoBehaviour,
                               IPointerDownHandler,
                               IPointerUpHandler,
                               IDragHandler
{
    Rigidbody2D rb;
    Rigidbody2D[] rbChildren;
    Collider2D col;
    Collider2D[] colChildren;
    SpriteRenderer sr;
    
    CloudState state = CloudState.flow;
    
    //상호작용 체킹용
    JointMaker jm;
    [HideInInspector] public bool crush = false;

    //구름 반발력 용도
    Collider2D separator;
    Coroutine co = null;

    float timer = 0f; //상호작용 없으면 돌아가는 용도

    public static bool AnyCloudBeingDragged { get; private set; }
    //Vector3 dragOffset;
    Vector2 holdStartPos;
    Vector2 lastPointerWorld;
    float holdTimer;
    bool isRotating;

    [Header("Settings")]
    public float waitTime = 1f;
    public float flowSpeed = -1;
    [Tooltip("구름 분리시 검사 실행 쿨타임")]public float checktime = 0.3f; 

    [Header("Drag & Rotate")]
    public float holdToRotate = 0.75f;
    public float rotateSpeed = -90f;
    public float moveDeadZone = 0.4f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rbChildren = GetComponentsInChildren<Rigidbody2D>().Where<Rigidbody2D>(c => c.gameObject != gameObject).ToArray();
        colChildren = GetComponentsInChildren<Collider2D>().Where<Collider2D>(c => c.gameObject != gameObject).ToArray();
        separator = colChildren.FirstOrDefault(c => c.gameObject.layer == LayerMask.NameToLayer("CloudSeparate"));
        colChildren = colChildren.Where<Collider2D>(c => c != separator).ToArray();
        sr = GetComponent<SpriteRenderer>();
        Flow();
    }

    void Flow()
    {
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Static;
        foreach (var rbChild in rbChildren)
        {
            rbChild.velocity = Vector2.zero;
            rbChild.bodyType = RigidbodyType2D.Static;
            rbChild.gravityScale = 0;
        }
        
        separator.isTrigger = true;
        col.isTrigger = true;
        foreach (var col in colChildren)
        {
            col.enabled = false;
        }

        gameObject.tag = "FlowCloud";
        sr.sortingLayerName = "FlowCloud";
        sr.color = new Color(1, 1, 1, 0.7f);

        state = CloudState.flow;
    }

    // Update is called once per frame
    void Update()
    {
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
                Flow();
            }
        }
        if (transform.position.x < -15 || transform.position.x > 15) Destroy(gameObject);
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
        Collider2D[] results = new Collider2D[1];
        LayerMask mask = LayerMask.GetMask("CloudSeparate", "JointedCloud");
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(mask);

        int count = separator.OverlapCollider(filter, results);
        if (count > 0)
        {
            foreach (Collider2D col in colChildren) col.isTrigger = true;
            separator.isTrigger = false;
            if (results[0].transform.parent.TryGetComponent<CloudController>(out CloudController c)) c.StartSeparate();

            //separator랑 겹치는 게 없을 때 까지 콜라이더 활성화(튕기기)실행
            while (true)
            {
                yield return new WaitForSeconds(checktime);

                count = separator.OverlapCollider(filter, results);
                if (count > 0)
                {
                    if (results[0].transform.parent.TryGetComponent<JointMaker>(out JointMaker _)) //JointMaker가 있는 대상이면 조인트용 겹침검사 실행
                    {
                        CheckOverlap();
                        break;
                    }
                    else if (results[0].transform.parent.TryGetComponent<CloudController>(out c)) c.StartSeparate();
                }
                else break;
            }

            //복구
            foreach (Collider2D col in colChildren) col.isTrigger = false;
            separator.isTrigger = true;
        }
        co = null;
    }

    public void Disappear() => StartCoroutine(FadeOutAndDestory());

    private IEnumerator FadeOutAndDestory()
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

        Destroy(gameObject);
    }

    int activePointer = -1;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointer != -1) return;
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
                rb.angularVelocity = 0;
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

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        foreach (Rigidbody2D rb in rbChildren)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        rb.Sleep();

        foreach (Collider2D col in colChildren)
        {
            col.enabled = true;
            col.isTrigger = false;
        }
        sr.color = Color.white;
        sr.sortingLayerName = "Default";
        AnyCloudBeingDragged = false;
        CameraController.Instance.EndDrag();
        separator.gameObject.layer = 10;
        CheckOverlap();     //구름 놓았을 떄 닿아있는 구름에 연결 로직 실행
    }


    void StartDragging() //드래그 중 돌의 상태 설정
    {
        StopAllCoroutines(); co = null;
        AnyCloudBeingDragged = true;
        CameraController.Instance.BeginDrag(this);

        state = CloudState.Dragging;
        gameObject.tag = "DraggingCloud";
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        foreach (Rigidbody2D rb in rbChildren) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; }
        separator.isTrigger = true;
        foreach (Collider2D col in colChildren) col.enabled = false;

        rb.bodyType = RigidbodyType2D.Static;
        foreach (Rigidbody2D rb in rbChildren) rb.bodyType = RigidbodyType2D.Static;

        //dragOffset = transform.position - (Vector3)ScreenToWorld();
        holdTimer = 0;
        holdStartPos = ScreenToWorld();
        isRotating = false;

        sr.color = new Color(1, 1, 1, 0.5f);
        sr.sortingLayerName = "DraggingStone";
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

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
