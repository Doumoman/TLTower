using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CloudSystem : MonoBehaviour
{
    public GameObject savePoint;
    [SerializeField]private List<JointMaker> nodes = new List<JointMaker>();
    public GameObject cloudSpawner;
    public GameObject skyStage;
    [HideInInspector]public int cloudLimit;
    public int[] cloudLimitList;

    [Header("UI")]
    public TextMeshProUGUI remainingTMP;

    public float HighestJointY {  get; private set; }
    public static CloudSystem Instance { get; private set; }

    [Header("CameraMove")]
    public float waitTime = 1f;
    public float moveTime = 2.5f;

    private void Awake()
    {
        if (Instance != null) Destroy(this);
        Instance = this;
    }

    private void OnValidate()
    {
        if (cloudLimitList == null || cloudLimitList.Length != (int)chapter.winter -  (int)chapter.autumn)
        {
            cloudLimitList = new int[(int)chapter.winter - (int)chapter.autumn];
        }
    }

    private void Start()
    {
        if (nodes == null) HighestJointY = StoneFixer.Instance.HighestSettledY;
        else FindHighestJM();

        //하늘 스테이지 시작 위치를 젤 높은 돌에 맞춤
        float yPos = MathF.Max(StoneFixer.Instance.HighestSettledY, StoneFixer.Instance.HighestFixedY);
        skyStage.transform.position = new Vector2(0, yPos);
    }


    public void SetSavePoint(GameObject go) => StartCoroutine(NewSavePoint(go));
    public IEnumerator NewSavePoint(GameObject go)
    {
        savePoint = go;
        float force = nodes[0].breakForce;
        if (nodes[0].TryGetComponent<JointMakerPhysics>(out JointMakerPhysics jmp)) Destroy(jmp);
        DestroyAll(true);
        yield return null;  //jm이 사라지길 한 프레임 기다리기

        JointMaker jm = go.AddComponent<JointMaker>();
        jm.breakForce = force;
        go.AddComponent<JointMakerPhysics>();
        jm.Init(null);
        FindHighestJM();
        StartCoroutine(MoveCamera());

        ChapterManager.Instance.CloudCheckPoint[(int)ChapterManager.Instance.chapter - (int)chapter.autumn + 1].AddComponent<CloudSavePoint>();
        cloudLimit = cloudLimitList[(int)ChapterManager.Instance.chapter - (int)chapter.autumn];

        SkyCloudSpawner.Instance.Changedirection();

        UpdateUI();
        //SkyCloudSpawner cs = cloudSpawner.GetComponent<SkyCloudSpawner>();
        //cs.Changedirection();
    }

    public IEnumerator MoveCamera()
    {
        yield return new WaitForSeconds(waitTime);
        CameraController.Instance.CenterOnY(HighestJointY + 4, moveTime);
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

        //고립대상들 삭제
        foreach (JointMaker node in nodes)
        {
            if (node.TryGetComponent<CloudController>(out CloudController c)) c.StartSeparate();
            if (jmList.Contains(node)) continue;
            Destroy(node);
        }
        nodes = jmList;

        FindHighestJM();  //가장 높은 구름 초기화
        UpdateUI();
    }

    //jointMaker모두 없애기
    public void DestroyAll(bool preventReJoint = false)
    {
        foreach (JointMaker node in nodes)
        {
            Destroy(node);
            if (node.TryGetComponent<CloudController>(out CloudController c)) c.Disappear();
        }
        nodes.Clear();
    }

    public int GetNodeLength()
    {
        List<JointMaker> newNodes = nodes;
        foreach(JointMaker node in nodes)
        {
            if (node == null) newNodes.Remove(node);
        }
        nodes = newNodes;
        return nodes.Count; 
    }

    void UpdateUI()
    {
        if (!remainingTMP) return;

        int remain = cloudLimit + 1 - GetNodeLength();
        remainingTMP.text = $"<b>{remain}</b>";
    }
}
