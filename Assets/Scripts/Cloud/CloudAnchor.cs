/*
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public interface ICloudAnchor
{
    void OnDrag(PointerEventData eventData);
    void OnPointerDown(PointerEventData eventData);
    void OnPointerUp(PointerEventData eventData);
}

public class CloudAnchor : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, ICloudAnchor
{

    [Header("Distances")] //isClose()의 BoxCollider2D의 크기를 설정하는데 사용, 돌이 가까우면 Cloud의 상태를 Idle에서 Platform으로 변경
    public float StoneDetectionDistanceX = 2f;
    public float StoneDetectionDistanceY = 2f;

    [Header("Stickiness")] //spring joint 관련련
    public float springFrequency = 6f;
    public float springDamping = 0.5f;
    public float springBreakForce = 10f;

    private enum CloudState
    {
        Idle = 0, //대기 상태
        Platform = 1, //고정
        Sticky = 2 //물리엔진 적용
    }

    private CloudState currentCloudState;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private CircleCollider2D col;
    private BoxCollider2D box;
    private bool isColliding = false;

    private GameObject stone;
    private List<Rigidbody2D> stones = new List<Rigidbody2D>();
    void Start()
    {
        currentCloudState = 0;
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;
        rb.gravityScale = 0.0f;
        rb.mass = 0.2f;
        rb.drag = 5.0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(StoneDetectionDistanceX, StoneDetectionDistanceY);
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = false;
        sr = GetComponent<SpriteRenderer>();
        sr.color = new Color(1, 1, 1, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        //가까운 곳에 Settled이나 Fixed인 돌이 있다면 Active 상태로 변경
        if (currentCloudState == CloudState.Idle && isColliding)
        {
            currentCloudState = CloudState.Platform;
            rb.simulated = true;
            sr.color = new Color(1, 1, 1, 1);
        }
        
        //Platform 상태일 때 고정
        if (currentCloudState == CloudState.Platform)
        {
            rb.bodyType = RigidbodyType2D.Static;
            rb.gravityScale = 0.0f;
            //col과 충돌한 stone에 spring joint 연결

        }

        //Sticky 상태일 때 물리엔진 적용
        if(currentCloudState == CloudState.Sticky)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0.0f;
            rb.simulated = true;
            //col과 충돌한 stone에 spring joint 연결

        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
        rb.simulated = false;
        
    }

    public void OnPointerUp(PointerEventData eventData){
        currentCloudState = CloudState.Idle;
        //spring joint 제거
        rb.simulated = false;
    }

    public void OnPointerDown(PointerEventData eventData){
        currentCloudState = CloudState.Sticky;
        rb.simulated = true;
    }

    void OnCollisionStay2D(UnityEngine.Collision2D collision)
    {
        
    }

    //인접한 stone에 spring joint 연결
    int SetSpringJoint(List<Rigidbody2D> stones)
    {
        int count = 0;
        foreach (var stone in stones)
        {
            var joint = gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = stone;
            joint.autoConfigureDistance = true;
            joint.frequency = springFrequency;
            joint.dampingRatio = springDamping;
            joint.breakForce = springBreakForce;
            count++;
        }
        return count;
    }
}
*/