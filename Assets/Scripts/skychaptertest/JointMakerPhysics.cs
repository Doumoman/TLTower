using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//jointmaker의 물리 담당. jointmaker컴포넌트가 같이 있을 떄, 또는 부모 오브젝트에 있을 때 두 경우.
public class JointMakerPhysics : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision) 
    {
        if (!collision.gameObject.CompareTag("Cloud")) return;  //Cloud인 것만 인식

        JointMaker jm = GetJointMaker();
        if (jm) jm.MakeBoneJoint(this, collision);
    }

    //깨진 joint2d 삭제. joint2D 대상에 연결 끊긴거 알려줌(initial에서 삭제)
    private void OnJointBreak2D(Joint2D joint)
    {
        JointMaker jm = GetJointMaker();
        JointMaker otherJm = joint.connectedBody.GetComponent<JointMakerPhysics>().GetJointMaker();

        //Destroy는 프레임 끝에서 뒤늦게 실행되므로 비활성화를 통해 끊긴 걸 바로 표시(근데 또 이번엔 enabled가 나중에됨)
        joint.enabled = false;
        otherJm.RemoveInit(jm);
        Destroy(joint);
    }

    //이것과 연결된 jointmaker 반환. 없으면 null
    public JointMaker GetJointMaker()
    {
        if (!TryGetComponent<JointMaker>(out JointMaker jm)) jm = transform.parent.GetComponent<JointMaker>();
        return jm;
    }
}
