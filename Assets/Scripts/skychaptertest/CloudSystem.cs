using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class CloudSystem : MonoBehaviour
{
    public GameObject savePoint;
    [SerializeField]private List<JointMaker> nodes = new List<JointMaker>();

    public float HighestJointY {  get; private set; }
    public static CloudSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) Destroy(this);
        Instance = this;
    }

    private void Start()
    {
        HighestJointY = StoneFixer.Instance.HighestSettledY;
    }

    public void SetSavePoint(GameObject go)
    {
        savePoint = go;
        nodes.Clear();
        go.AddComponent<JointMaker>();
        FindHighestJM();
    }

    //구름 조인트시 호출됨
    public void NotifyJoint(JointMaker jm)
    {
        if (!nodes.Contains(jm)) nodes.Add(jm);
        ExamineAndDeprive();
    }

    //가장 높은 JointMaker 찾기
    public void FindHighestJM()
    {
        float yPos = HighestJointY;
        foreach (JointMaker jm in nodes)
        {
            if (jm.transform.position.y > yPos) yPos = jm.transform.position.y;
        }
        HighestJointY = yPos;
    }

    //검사 및 능력 박탈 함수
    public void ExamineAndDeprive()
    {
        //체크포인트를 기점으로 깊이우선탐색 검사 실행
        JointMaker jm = savePoint.GetComponent<JointMaker>();
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

        FindHighestJM();  //가장 높은 구름 초기화
    }

    //jointMaker모두 없애기
    public void DestroyAll()
    {
        foreach (JointMaker node in nodes) Destroy(node);
        nodes.Clear();
    }
}
