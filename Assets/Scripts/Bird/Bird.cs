using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BirdState {come, sat, go}
public class Bird : MonoBehaviour
{
    BirdState state;
    private Vector2 point;
    private Vector2 stonePoint;
    private GameObject satStone;
    //private FixedJoint2D joint;
    private Coroutine coroutine;

    [Header("Settings")]
    public float flyTime;
    public float moveDeadZone = 0.5f;

    public void Init(GameObject stone, Vector2 surfacePoint)
    {
        satStone = stone;
        point = surfacePoint;
        state = BirdState.come;
        coroutine = StartCoroutine(FlyToPoint(satStone, point));
    }
    // Update is called once per frame
    void Update()
    {
        stonePoint = satStone.transform.position;
        //돌 위치가 변하면 날아가기
        if (Vector2.Distance(stonePoint, satStone.transform.position) > moveDeadZone && state != BirdState.go)
        {
            state = BirdState.go;
            if(coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            coroutine = StartCoroutine(FlyAway());
        }
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
        /*
        joint = flyStone.AddComponent<FixedJoint2D>();
        joint.connectedBody = gameObject.GetComponent<Rigidbody2D>();
        */
    }
    IEnumerator FlyAway()
    {
        /*
        if (joint != null) joint.enabled = false;
        */
        yield break;
    }
}
