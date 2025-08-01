using System.Collections;
using UnityEngine;

//jointmaker의 물리 담당. jointmaker컴포넌트가 같이 있을 떄, 또는 부모 오브젝트에 있을 때 두 경우.
public class JointMakerPhysics : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision) 
    {
        if (transform.parent.TryGetComponent<CloudController>(out CloudController c)) c.crush = true;
        MakeJoint(collision.gameObject);
    }

    //깨진 joint2d 삭제. joint2D 대상에 연결 끊긴거 알려줌(initial에서 삭제)
    private void OnJointBreak2D(Joint2D joint)
    {
        StartCoroutine(JointBreak(joint));
    }
    public void MakeJoint(GameObject go)
    {
        if (!go.CompareTag("CloudChild")) return;  //CloudChild인 것만 인식
        if (CloudSystem.Instance.GetNodeLength() > CloudSystem.Instance.cloudLimit) return; //구름 개수 제한

        JointMaker jm = GetJointMaker();
        if (jm) jm.MakeBoneJoint(this, go);
    }

    private IEnumerator JointBreak(Joint2D joint)
    {
        JointMaker jm = GetJointMaker();
        JointMaker otherJm = null;
        if (joint.connectedBody.TryGetComponent<JointMakerPhysics>(out JointMakerPhysics jmp))
            otherJm = jmp.GetJointMaker();

        //Destroy는 프레임 끝에서 뒤늦게 실행되므로 비활성화를 통해 끊긴 걸 바로 표시(근데 또 이번엔 enabled가 나중에됨)
        joint.enabled = false;
        Destroy(joint);
        yield return null;  //한 프레임 쉬기(조인트 해제가 반영되길 기다림)

       if (otherJm) otherJm.RemoveInit(jm);
    }
    //이것과 연결된 jointmaker 반환. 없으면 null
    public JointMaker GetJointMaker()
    {
        if (!TryGetComponent<JointMaker>(out JointMaker jm)) jm = transform.parent.GetComponent<JointMaker>();
        return jm;
    }

    //드래그 끝났을 시 겹친 다른 오브젝트에 조인트 형성 함수 실행
    public void CheckOverlap()
    {
        Collider2D col = GetComponent<Collider2D>();
        Collider2D[] results = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
    
        int count = col.OverlapCollider(filter, results);
        for (int i = 0; i < count; i++)
        {
            GameObject go = results[i].gameObject;
            if (go.TryGetComponent<JointMakerPhysics>(out JointMakerPhysics jmp)) jmp.MakeJoint(gameObject);
        }
    }

    public void StartJoint(GameObject Go, float breakForce) => StartCoroutine(NewJoint(Go, breakForce));
    public IEnumerator NewJoint(GameObject otherBone, float breakForce)
    {
        // 닿은 대상과 joint2d 형성
        Joint2D joint = gameObject.AddComponent<FixedJoint2D>();
        joint.connectedBody = otherBone.GetComponent<Rigidbody2D>();
        joint.breakForce = 1000;
        joint.breakAction = JointBreakAction2D.CallbackOnly;

        yield return new WaitForSeconds(0.5f);
        if (joint) joint.breakForce = breakForce;
    }
}
