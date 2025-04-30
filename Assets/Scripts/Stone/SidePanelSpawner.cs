using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬에 빈 트랜스폼(8개)을 자식으로 두고, 거기에 돌을 채워 넣음
/// </summary>
public class SidePanelSpawner : MonoBehaviour
{
    [SerializeField] StoneDatabase database;
    [SerializeField] Transform[] slots = new Transform[8];

    readonly Dictionary<Transform, Stone> occupants = new();

    void Start()
    {
        foreach (var t in slots) SpawnStone(t);
    }

    void SpawnStone(Transform slot)
    {
        var data = database.RandomStone();
        if (data == null || data.prefab == null) return;

        // parent 는 slot(Scene 오브젝트) 로 지정
        var go = Instantiate(data.prefab, slot.position, Quaternion.identity, slot);
        var stone = go.GetComponent<Stone>();
        stone.Init(data);

        stone.FromSideSlot = true;
        stone.SetPhysics(false);   // 패널에서는 고정

        occupants[slot] = stone;
    }
    /// <summary>TouchController가 “슬롯 돌이 드래그로 빠져나갔다” 고 알려줄 때 호출</summary>
    public void NotifySlotFreed(Stone stone)
    {
        foreach (var kv in occupants)
        {
            if (kv.Value == stone)
            {
                occupants.Remove(kv.Key);
                // 슬롯 비워졌으니 새 돌 채우기
                SpawnStone(kv.Key);
                return;
            }
        }
    }
}