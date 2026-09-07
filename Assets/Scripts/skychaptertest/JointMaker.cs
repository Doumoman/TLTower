using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
구름의 연결 능력을 담당. JointmakerPhysics로부터 충돌 정보를 받아 다른 구름과의 연결 형성
다른 구름과의 연결정보 다룸
연결정보를 활용한 탐색기능
*/
public class JointMaker : MonoBehaviour
{
    public float breakForce;
    [SerializeField] private List<JointMaker> initial = new List<JointMaker>();  //날 잡고있는 오브젝트. 중복 연결 방지용
    private bool isNotyfied = false;
    private bool cleanupStarted;

    //스크립트 형성시(연결능력 부여시) or 조인트 당했으면 뭐가 Joint했는지 알기
    public void Init(JointMaker jm)
    {
        if (cleanupStarted) return;
        if (jm) initial.Add(jm);

        // 처음 한 번만 보고하기
        if (isNotyfied) return;
        CloudSystem.Instance.NotifyJoint(this);
        isNotyfied = true;

        //조인트된 구름 설정
        if (TryGetComponent<CloudController>(out CloudController c)) c.Jointed();

        //체크포인트에 연결된 경우
        if (jm && jm.gameObject == CloudSystem.Instance.savePoint)
        {
            //GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static; //체크포인트랑 닿은건 고정시킴
            if (c) c.IgnoreCollision(jm.gameObject);

        }
    }

    //날 잡고있던 조인트 파괴시, 날 잡던 구름을 initial 리스트에서 제거하기. 그리고 검사 실행
    public void RemoveInit(JointMaker jm)
    {
        if (cleanupStarted) return;

        initial.Remove(jm);
        if (CloudSystem.Instance) CloudSystem.Instance.ExamineAndDeprive();
    }

    public void MakeBoneJoint(JointMakerPhysics jmp, GameObject otherBone)
    {
        GameObject bone = jmp.gameObject;

        JointMaker jm = null;
        if (otherBone.TryGetComponent<JointMakerPhysics>(out JointMakerPhysics otherJmp)) jm = otherJmp.GetJointMaker();  //닿은 대상과 연결된 jm
        else return;  //jmp없으면 bone이 아니므로 멈추기

        if (initial.Contains(jm) || jm == this) return;       //initial에 등록된 JointMaker(이미 연결된거)면 실행 안함. 또는 자기 자신인 경우도(간혹 있음)

        //닿은 대상이 아직 joint2d를 형성하지 않은 구름이라면
        List<FixedJoint2D> joint2Ds = GetJointList(); 
        if (!joint2Ds.Find(x => GetConnectedJointMaker(x) == jm))  //이미 잡고있는 구름중에 지금 찾은 jm이 없다면
        {
            jmp.StartJoint(otherBone, breakForce);

            //닿은 대상에 JointMaker로 오브젝트 전달. 없다면 추가(연결 능력 부여)
            if (jm != null)
            {
                jm.Init(this);
            }
            else
            {
                jm = otherBone.transform.parent.gameObject.AddComponent<JointMaker>(); //어차피 체크포인트에 jointmaker추가할 일은 없으니 부모로
                jm.Init(this);
                jm.breakForce = breakForce;
            }

            SoundManager.Instance.PlaySFX("cloud_connect");
        }
    }

    //구름 드래그시 initial의 구름과의 연결 끊기
    public void Detach()
    {
        foreach (JointMaker jm in initial.Where(x => x).Distinct().ToList())
        {
            List<FixedJoint2D> joint2Ds = jm.GetJointList()
                .FindAll(x => GetConnectedJointMaker(x) == this);  //이 오브젝트를 잡고있는 조인트들.
            foreach (FixedJoint2D joint in joint2Ds)
            {
                joint.breakForce = 0;
            }
        }
    }

    //활성화된 조인트 리스트 얻기
    public List<FixedJoint2D> GetJointList()
    {
        List<FixedJoint2D> joint2Ds = GetAllJoints().FindAll(x => x.enabled);
        foreach (FixedJoint2D joint in joint2Ds)
        {
            if (joint.connectedBody != null) continue;

            joint.enabled = false;
            Destroy(joint);
        }

        return joint2Ds.FindAll(x => x.connectedBody != null);
    }

    public void PrepareForDespawn()
    {
        CleanupConnections(updateCloudState: false);
    }

    //이 스크립트를 삭제 및 조인트 모두 해제
    private void OnDestroy()
    {
        CleanupConnections(updateCloudState: true);
    }

    private void CleanupConnections(bool updateCloudState)
    {
        if (cleanupStarted) return;
        cleanupStarted = true;

        bool disconnectedAny = false;

        // 다른 구름이 이 구름을 잡고 있는 조인트를 먼저 제거한다.
        foreach (JointMaker owner in initial.Where(x => x).Distinct().ToList())
        {
            foreach (FixedJoint2D joint in owner.GetAllJoints())
            {
                if (GetConnectedJointMaker(joint) != this) continue;
                disconnectedAny |= DisableAndDestroy(joint);
            }
        }
        initial.Clear();

        // 이 구름이 다른 구름을 잡고 있는 조인트와 상대방의 역참조를 제거한다.
        foreach (FixedJoint2D joint in GetAllJoints())
        {
            JointMaker connected = GetConnectedJointMaker(joint);
            if (connected && connected != this)
                connected.RemoveInitialReferenceOnly(this);

            disconnectedAny |= DisableAndDestroy(joint);
        }

        if (disconnectedAny && SoundManager.Instance)
            SoundManager.Instance.PlaySFX("cloud_disconnected");

        if (updateCloudState && TryGetComponent(out CloudController c))
        {
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
            c.DisJointed();
            c.StartSeparate();
        }
    }

    private List<FixedJoint2D> GetAllJoints()
    {
        return new List<FixedJoint2D>(GetComponentsInChildren<FixedJoint2D>(true));
    }

    private static JointMaker GetConnectedJointMaker(FixedJoint2D joint)
    {
        if (!joint || !joint.connectedBody) return null;
        if (!joint.connectedBody.TryGetComponent(out JointMakerPhysics physics)) return null;
        return physics.GetJointMaker();
    }

    private static bool DisableAndDestroy(FixedJoint2D joint)
    {
        if (!joint || !joint.enabled) return false;

        joint.enabled = false;
        Destroy(joint);
        return true;
    }

    private void RemoveInitialReferenceOnly(JointMaker owner)
    {
        initial.RemoveAll(x => !x || x == owner);
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
        foreach (FixedJoint2D joint2D in joint2Ds)
        {
            JointMaker connected = GetConnectedJointMaker(joint2D);
            if (connected) jmList.Add(connected);
        }
        //연결된 것들 중 탐색 안된 것 모두 검사
        foreach (JointMaker jm in jmList)
        {
            if (jm && !jms.Contains(jm)) jm.DFS(ref jms);
        }
    }
}
