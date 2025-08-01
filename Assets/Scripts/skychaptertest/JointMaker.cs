using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class JointMaker : MonoBehaviour
{
    public float breakForce;
    [SerializeField] private List<JointMaker> initial = new List<JointMaker>();  //날 잡고있는 오브젝트. 중복 연결 방지용
    private bool isNotyfied = false;
    Collider2D separator;

    //스크립트 형성시(연결능력 부여시) or 조인트 당했으면 뭐가 Joint했는지 알기
    public void Init(JointMaker jm)
    {
        if (jm) initial.Add(jm);

        // 처음 한 번만 보고하기
        if (isNotyfied) return;
        CloudSystem.Instance.NotifyJoint(this);
        isNotyfied = true;

        //분리에서 제외하기
        separator = GetComponentsInChildren<Collider2D>().FirstOrDefault(c => c.gameObject.layer == LayerMask.NameToLayer("CloudSeparate"));
        separator.gameObject.layer = LayerMask.NameToLayer("JointedCloud");

        if (jm && jm.gameObject == CloudSystem.Instance.savePoint) GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static; //체크포인트랑 닿은건 고정시킴
    }

    //날 잡고있던 조인트 파괴시, 날 잡던 구름을 initial 리스트에서 제거하기. 그리고 검사 실행
    public void RemoveInit(JointMaker jm)
    {
        initial.Remove(jm);
        CloudSystem.Instance.ExamineAndDeprive();
    }

    public void MakeBoneJoint(JointMakerPhysics jmp, GameObject otherBone)
    {
        GameObject bone = jmp.gameObject;

        JointMaker jm = null;
        if (otherBone.TryGetComponent<JointMakerPhysics>(out JointMakerPhysics otherJmp)) jm = otherJmp.GetJointMaker();  //닿은 대상과 연결된 jm
        if (initial.Contains(jm) || jm == this) return;       //initial에 등록된 JointMaker(이미 연결된거)면 실행 안함. 또는 자기 자신인 경우도(간혹 있음)

        //닿은 대상이 아직 joint2d를 형성하지 않은 구름이라면
        List<FixedJoint2D> joint2Ds = GetJointList(); 
        if (!joint2Ds.Find(x => x.connectedBody.GetComponent<JointMakerPhysics>().GetJointMaker() == jm))  //이미 잡고있는 구름중에 지금 찾은 jm이 없다면
        {
            jmp.StartJoint(otherBone, breakForce);

            //닿은 대상에 JointMaker로 오브젝트 전달. 없다면 추가(연결 능력 부여)
            if (jm != null)
            {
                jm.Init(this);
            }
            else
            {
                jm = otherBone.transform.parent.AddComponent<JointMaker>(); //어차피 체크포인트에 jointmaker추가할 일은 없으니 부모로
                jm.Init(this);
                jm.breakForce = breakForce;
            }

            SoundManager.Instance.PlaySFX("cloud_connect");
        }
    }

    //구름 드래그시 initial의 구름과의 연결 끊기
    public void Detach()
    {
        foreach (JointMaker jm in initial)
        {
            List<FixedJoint2D> joint2Ds = jm.GetJointList().FindAll(x => x.connectedBody.GetComponent<JointMakerPhysics>().GetJointMaker() == this);  //이 오브젝트를 잡고있는 조인트들.
            foreach (FixedJoint2D joint in joint2Ds)
            {
                joint.breakForce = 0;
            }
        }
    }

    //활성화된 조인트 리스트 얻기
    public List<FixedJoint2D> GetJointList()
    {
        List<FixedJoint2D> joint2Ds = new List<FixedJoint2D>();
        joint2Ds = new List<FixedJoint2D>(GetComponentsInChildren<FixedJoint2D>()).FindAll(x => x.enabled == true);
        foreach (Joint2D joint in joint2Ds)
        {
            if (joint.connectedBody == null) joint.breakForce = 0;
        }
        joint2Ds = joint2Ds.FindAll(x => x.connectedBody != null);
        if (joint2Ds == null) joint2Ds = new List<FixedJoint2D>(GetComponents<FixedJoint2D>()).FindAll(x => x.enabled == true);
        return joint2Ds;
    }

    //이 스크립트를 삭제 및 조인트 모두 해제
    public void OnDestroy()
    {
        List<FixedJoint2D> joint2Ds = GetJointList();
        foreach (Joint2D joint in joint2Ds)
        {
            joint.enabled = false;
            Destroy(joint);
            SoundManager.Instance.PlaySFX("cloud_disconnected");
            //StartCoroutine(disconnect(disconnectionInterval));
        }

        if (TryGetComponent<CloudController>(out CloudController _)) GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        if (separator) separator.gameObject.layer = LayerMask.NameToLayer("CloudSeparate");
        if (TryGetComponent<CloudController>(out CloudController c)) c.StartSeparate();
    }

    //[SerializeField] private float disconnectionInterval = 0.1f;
    //IEnumerator disconnect(float ReallyLongTime)
    //{
    //    SoundManager.Instance.PlaySFX("cloud_disconnected");
    //    yield return new WaitForSeconds(ReallyLongTime);
    //}

    public void DFS(ref List<JointMaker> jms)
    {
        jms.Add(this);

        //initial을 포함하여 연결된 JointMaker리스트 얻기
        List<FixedJoint2D> joint2Ds = GetJointList();
        List<JointMaker> jmList = new List<JointMaker>();
        jmList.AddRange(initial);
        foreach (FixedJoint2D joint2D in joint2Ds) jmList.Add(joint2D.connectedBody.GetComponent<JointMakerPhysics>().GetJointMaker());
        //연결된 것들 중 탐색 안된 것 모두 검사
        foreach (JointMaker jm in jmList) if (!jms.Contains(jm)) jm.DFS(ref jms);
    }
}
