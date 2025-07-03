using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointMaker : MonoBehaviour
{
    public float breakForce;
    private List<JointMaker> initial = new List<JointMaker>();  //이 오브젝트에 joint2D를 형성한 오브젝트. 중복 연결 방지용
    private bool isconnected = false;
    private bool isNotyfied = false;


    //스크립트 형성시(연결능력 부여시) or 조인트 당했으면 뭐가 Joint했는지 알기
    public void Init(JointMaker jm)
    {
        initial.Add(jm);
        this.GetComponent<SpriteRenderer>().color = Color.blue;

        // 처음 한 번만 보고하기
        if (isNotyfied) return;
        CloudSystem.Instance.NotifyJoint(this);
        isNotyfied=true;

    }


    //initial 리스트에서 제거하기. 그리고 검사 실행
    public void RemoveInit(JointMaker jm)
    {
        initial.Remove(jm);
        CloudSystem.Instance.ExamineAndDeprive();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        if (!other.CompareTag("Cloud") || 
            (other.TryGetComponent<JointMaker>(out JointMaker jm) && initial.Contains(jm))) return; //initial의 오브젝트라면 실행 안함

        //닿은 대상이 아직 joint2d를 형성하지 않은 구름이라면
        List<Joint2D> joint2Ds = GetJointList();
        if (!joint2Ds.Find(x => x.connectedBody == other.GetComponent<Rigidbody2D>()))
        {
            // 닿은 대상과 joint2d 형성
            Joint2D jo = gameObject.AddComponent<FixedJoint2D>();
            jo.connectedBody = other.GetComponent<Rigidbody2D>();
            jo.breakAction = JointBreakAction2D.CallbackOnly;  //joint2d가 깨지면 호출하고록 설정
            jo.breakForce = breakForce;
            jo.enableCollision = true;

            //닿은 대상에 JointMaker로 오브젝트 전달. 없다면 추가(연결 능력 부여)
            if (jm != null)
            {
                jm.Init(this);
            }
            else
            {
                jm = other.AddComponent<JointMaker>();
                jm.Init(this);
                jm.breakForce = breakForce;

            }

        }
    }

    //깨진 joint2d 삭제. joint2D 대상에 연결 끊긴거 알려줌(initial에서 삭제)
    private void OnJointBreak2D(Joint2D joint)
    {
        JointMaker jm = joint.connectedBody.GetComponent<JointMaker>();

        //Destroy는 프레임 끝에서 뒤늦게 실행되므로 비활성화를 통해 끊긴 걸 바로 표시
        joint.enabled = false;
        jm.RemoveInit(this);
        DestroyUnenabled();
    }

    public List<Joint2D> GetJointList()
    {
        List<Joint2D> joint2Ds = new List<Joint2D>();
        joint2Ds = new List<Joint2D>(gameObject.GetComponents<Joint2D>()).FindAll(x => x.enabled == true);
        return joint2Ds;
    }

    public void UnConnected()
    {
        List<Joint2D> joint2Ds = GetJointList();
        foreach (Joint2D joint in joint2Ds) Destroy(joint);
        this.GetComponent<SpriteRenderer>().color = Color.white;
        Destroy(this);

    }

    public bool IsConnected() { return isconnected; }
    public void ResetSearch() { isconnected = false; }

    //깊이우선탐색 방식. 검사된 것은 참으로 바꿈
    public void DFS(ref List<JointMaker> jms)
    {
        jms.Add(this);

        //initial을 포함하여 연결된 JointMaker리스트 얻기
        List<Joint2D> joint2Ds = GetJointList();
        List<JointMaker> jmList = new List<JointMaker>();
        jmList.AddRange(initial);
        foreach (Joint2D joint2D in joint2Ds) jmList.Add(joint2D.connectedBody.GetComponent<JointMaker>());

        //연결된 것들 중 탐색 안된 것 모두 검사
        foreach (JointMaker jm in jmList) if (!jms.Contains(jm)) jm.DFS(ref jms);    
    }

    //비활성화된 joint2D만 삭제하기
    public void DestroyUnenabled()
    {
        List<Joint2D> unenabledJoints = new List<Joint2D>(gameObject.GetComponents<Joint2D>()).FindAll(x => x.enabled == false);
        foreach (Joint2D joint in unenabledJoints) Destroy(joint);
    }
}
