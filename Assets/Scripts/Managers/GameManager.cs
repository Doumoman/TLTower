using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public StoneDatabase database;
    public Transform worldRoot;         // 실제 쌓이는 공간의 루트
    public SidePanelSpawner sidePanel;

    public int saveThreshold = 30;

    readonly List<Stone> worldStones = new();

    void Start()
    {
        // 세이브된 내용 있으면 복원
        SaveManager.Load(database, worldRoot, worldStones);
    }

    // 월드에 새 돌이 생길 때마다 호출 (터치 입력이 알아서 worldRoot 로 parent 변경)
    public void RegisterWorldStone(Stone s)
    {
        if (!worldStones.Contains(s)) worldStones.Add(s);
        if (worldStones.Count >= saveThreshold) SaveManager.Save(worldStones);
    }
}