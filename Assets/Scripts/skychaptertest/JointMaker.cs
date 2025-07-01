using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointMaker : MonoBehaviour
{
    public float breakForce;
    private GameObject initial = null;
    public void init(GameObject go)
    {
        //스크립트 형성시(연결능력 부여시) 뭘로부터 부여받았는지 알기. CloudSystem에 자신 보고
        initial = go;
        CloudSystem.Instance.NotifyJoint(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        List<Joint2D> joint2Ds = new List<Joint2D>();
        joint2Ds = new List<Joint2D>(gameObject.GetComponents<Joint2D>());
        GameObject other = collision.gameObject;
        if (other.tag != "Cloud" || other == initial) return;

        //닿은 대상이 아직 joint2d를 형성하지 않은 구름이라면
        if (!joint2Ds.Find(x => x.connectedBody == other))
        {
            // 닿은 대상과 joint2d 형성
            Joint2D jo = gameObject.AddComponent<FixedJoint2D>();
            jo.connectedBody = other.GetComponent<Rigidbody2D>();
            jo.breakAction = JointBreakAction2D.CallbackOnly;  //joint2d가 깨지면 호출
            jo.breakForce = breakForce;

            //닿은 대상에 JointMaker함수 추가(연결 능력 부여)
            JointMaker jm = other.AddComponent<JointMaker>();
            jm.init(gameObject);
            jm.breakForce = breakForce;
        }
    }

    //깨진 joint2d가 연결한 대상을 CloudSystem에 전달. 그리고 joint2d 및 JointMaker삭제
    private void OnJointBreak2D(Joint2D joint)
    {
        CloudSystem.Instance.NotifyBreak(joint.connectedBody);
        GameObject go = joint.connectedBody.gameObject;
        Destroy(go.GetComponent<JointMaker>());
        Destroy(joint);
    }
}
