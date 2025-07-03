using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    [SerializeField] StoneSpawner spawner;
    string path;

    void Awake()
    {
        path = Path.Combine(Application.persistentDataPath, "stone_save.json");
    }

    public void SaveGame()
    {
        StoneSaveData data = new();
        data.wave = StoneFixer.Instance.GetWave();

        foreach (var st in FindObjectsOfType<StoneController>())
        {
            if (st.state != StoneState.Fixed) continue;

            data.stones.Add(new StoneInfo
            {
                typeId = st.typeId,
                spriteIndex = st.GetSpriteIndexSafe(),
                x = st.transform.position.x,
                y = st.transform.position.y,
                rot = st.transform.eulerAngles.z
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log($"<color=cyan>저장 완료 · {data.stones.Count}개</color>\n{path}");
    }

    public void LoadGame()
    {
        if (!File.Exists(path)) { Debug.Log("세이브 없음"); return; }
        StoneSpawner.Instance.ResetAfterClear();

        // 씬에 남아 있는 모든 StoneController 삭제
        foreach (var st in FindObjectsOfType<StoneController>())
            Destroy(st.gameObject);

        // JSON → 객체 
        string json = File.ReadAllText(path);
        StoneSaveData data = JsonUtility.FromJson<StoneSaveData>(json);

        // Fixed 돌 재생성
        foreach (var info in data.stones)
            StoneSpawner.Instance.SpawnFixedStone(
                info.typeId,
                info.spriteIndex,
                new Vector2(info.x, info.y),
                info.rot);

        StoneFixer.Instance.RefreshHeightsFromScene();
        float topY = StoneFixer.Instance.HighestFixedY;
        CameraController.Instance.CenterOnY(topY, 0.1f); 

        StoneSpawner.Instance.RebuildStubSlots();

        Debug.Log($"로드 완료 · {data.stones.Count}개");
    }
}