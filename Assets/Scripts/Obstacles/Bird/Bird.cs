using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BirdState {come, sat, go}
public class Bird : MonoBehaviour
{
    BirdState state;
    private Vector2 goPoint;
    private Vector2 stonePoint;
    private GameObject satStone;
    //private FixedJoint2D joint;
    private Coroutine coroutine;
    private float timer;

    [Header("Settings")]
    public float flyTime;
    public float moveDeadZone;
    [Range(0, 1f)] public float forceChance;

    public void Init(GameObject stone, Vector2 surfacePoint)
    {
        goPoint = new Vector2(-transform.position.x, transform.position.y);
        satStone = stone;
        state = BirdState.come;
        coroutine = StartCoroutine(FlyToPoint(stone, surfacePoint));
        stonePoint = satStone.transform.position;
        timer = 0f;
    }

    void Update()
    {
        //돌 위치가 변하면 날아가기
        if (satStone != null)
        {
            if (Vector2.Distance(stonePoint, satStone.transform.position) > moveDeadZone && state != BirdState.go)
            {
                Go(false);
            }
        }
        else Go(false);
        if (state == BirdState.sat)
        {
            //일정 시간이 지나면 날아가기
            timer += Time.deltaTime;
            if (timer > BirdSpawner.cycle)
            {
                Go(false);
            }

            //터치한 오브젝트가 this면 날아가기
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended)
                {
                    Vector2 thouchPoint = Camera.main.ScreenToWorldPoint(touch.position);
                    RaycastHit2D hit2d = Physics2D.Raycast(thouchPoint, Vector2.zero);

                    if (hit2d.collider != null)
                    {
                        if (hit2d.transform == transform)
                        {
                            Go(true);
                        }
                    }
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                Vector2 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit2d = Physics2D.Raycast(mousePoint, Vector2.zero);
                if (hit2d.collider != null)
                {
                    if (hit2d.transform == transform)
                    {
                        Go(true);
                    }
                }
            }
        }
    }

    //돌과 부딪히면 날아가기
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (state == BirdState.sat && collision.tag == "PlacedStone" && collision.gameObject != satStone)
        {
            Go(true);
        }
    }

    //날아가는 코루틴 함수를 실행
    void Go(bool istouched)
    {
        //앉은 상태였으면 돌에 힘 가하기
        if (state == BirdState.sat)
        {
            if (satStone != null)
            {
                if (istouched) ForceRock();
                else if (!istouched && Random.value < forceChance) ForceRock();
            }
        }

        state = BirdState.go;
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
        coroutine = StartCoroutine(FlyAway());
    }

    //돌 가져가기
    void ForceRock()
    {
        Rigidbody2D rb = satStone.GetComponent<Rigidbody2D>();
        PolygonCollider2D[] cols = satStone.GetComponents<PolygonCollider2D>();
        rb.isKinematic = true;
        foreach (PolygonCollider2D col in cols)
        {
            col.enabled = false;
        }
        satStone.transform.SetParent(transform);
    }

    //돌의 표면까지 날아가기
    IEnumerator FlyToPoint(GameObject flyStone, Vector2 flyPoint)
    {
        Vector2 velocity = Vector2.zero;
        if (state != BirdState.come) yield break;
        while(true)
        {
            yield return null;
            Vector2 currentPoint = transform.position;

            transform.position = Vector2.SmoothDamp(currentPoint, flyPoint, ref velocity, flyTime);
            if (Vector2.Distance(flyPoint, gameObject.transform.position) < 0.1f) break;
        }
        state = BirdState.sat;
    }

    //새 퇴장 함수
    IEnumerator FlyAway()
    {
        Vector2 velocity = Vector2.zero;
        if (state != BirdState.go) yield break;
        while (true)
        {
            yield return null;
            Vector2 currentPoint = transform.position;

            transform.position = Vector2.SmoothDamp(currentPoint, goPoint, ref velocity, flyTime);
            if (Vector2.Distance(goPoint, gameObject.transform.position) < 1f) break;
        }
        Destroy(gameObject);
        yield break;
    }
}
