using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class CloudSystem : MonoBehaviour
{
    public GameObject checkPoint;
    [SerializeField]private List<JointMaker> nodes = new List<JointMaker>();

    public static CloudSystem Instance;

    private void Awake()
    {
        if (Instance != null) Destroy(this);
        Instance = this;
    }

    public void SetCheckPoint(GameObject go)
    {
        checkPoint = go;
        go.AddComponent<JointMaker>();
        nodes.Clear();
    }

    public void NotifyJoint(JointMaker jm)
    {
        if (!nodes.Contains(jm)) nodes.Add(jm);
        ExamineAndDeprive();
    }

    //검사 및 능력 박탈 함수
    public void ExamineAndDeprive()
    {
        //체크포인트를 기점으로 깊이우선탐색 검사 실행
        JointMaker jm = checkPoint.GetComponent<JointMaker>();
        List<JointMaker> jmList = new List<JointMaker>();
        jm.DFS(ref jmList);


        //검사 초기화 및 고립대상들 삭제
        foreach (JointMaker node in nodes)
        {
            if (jmList.Contains(node))
            {
                node.ResetSearch();
                continue;
            }
            Destroy(node);
        }
        nodes = jmList;
    }
}
