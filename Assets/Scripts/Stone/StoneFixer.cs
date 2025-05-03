using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StoneFixer : MonoBehaviour
{
    public static StoneFixer Instance { get; private set; }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Settings")]
    public int threshold = 10;                   // 10개씩 묶음

    [Header("UI")]
    public TextMeshProUGUI remainingTMP;         // 남은 돌 표시

    [Header("SavePoint")]
    public GameObject savePointPrefab;                 // 막대 프리팹 (IsTrigger 콜라이더 포함)
    GameObject currentSavePoint;
    public float HighestSettledY { get; private set; } = 0f;

    readonly List<StoneController> batch = new();   // 이번 라운드 Settled
    int wave = 0;                                   // 몇 번째 묶음인지

    // StoneController 가 Settled 될 때마다 호출
    public void RegisterSettled(StoneController sc)
    {
        if (sc.state != StoneState.Settled) return;

        // 최고 높이 갱신
        if (sc.transform.position.y > HighestSettledY)
            HighestSettledY = sc.transform.position.y;

        // 중복 방지
        if (!batch.Contains(sc)) batch.Add(sc);
        UpdateUI();

        //조건 달성: 세이브포인트 생성 
        if (batch.Count >= threshold && currentSavePoint == null)
        {
            wave++;
            Vector3 spawnPos = new(
                sc.transform.position.x,   // 가장 최근 돌의 X (원한다면 0 또는 중앙값으로)
                HighestSettledY + 3f,
                0f);
            currentSavePoint = Instantiate(savePointPrefab, spawnPos, Quaternion.identity);
            // SavePoint 스크립트에 StoneFixer 참조를 자동으로 넘기려면 다음 라인 추가
            currentSavePoint.GetComponent<SavePoint>()?.Init(this);

            Debug.Log($"[StoneFixer] Wave {wave} reached. SavePoint spawned at {spawnPos}");
        }
    }

    void UpdateUI()
    {
        if (!remainingTMP) return;

        int remain = Mathf.Max(0, threshold - batch.Count); // 0 이하 방지
        remainingTMP.text = $"남은 돌: <b>{remain}</b>";
    }
    public void FixAllStones()
    {
        foreach (var s in FindObjectsOfType<StoneController>())
        {
            if (s.state == StoneState.Settled)  
                s.SetFixed();
        }

        batch.Clear();
        UpdateUI();

        if (currentSavePoint) Destroy(currentSavePoint);
        currentSavePoint = null;
        Debug.Log("[StoneFixer] All Settled stones fixed by SavePoint!");
    }

    // 추락한 돌이 파괴되면 StoneDespawnZone → NotifyStoneLost 로 보고
    public void NotifyStoneLost(StoneController sc)
    {
        if (batch.Remove(sc))
            UpdateUI();
    }
}