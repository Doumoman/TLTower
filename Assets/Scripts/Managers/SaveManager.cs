using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
class Snapshot
{
    public string stoneName;
    public Vector3 pos;
    public Quaternion rot;
}

static class SaveManager
{
    const string KEY = "StoneSave";

    public static void Save(List<Stone> stones)
    {
        var list = new List<Snapshot>();
        foreach (var s in stones)
            list.Add(new Snapshot
            {
                stoneName = s.Data.stoneName,
                pos = s.transform.position,
                rot = s.transform.rotation
            });

        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(new Wrapper(list)));
        PlayerPrefs.Save();
        Debug.Log($"[SAVE] {list.Count}개 돌 저장 완료");
    }

    public static void Load(StoneDatabase db, Transform parent, List<Stone> outList)
    {
        if (!PlayerPrefs.HasKey(KEY)) return;
        var wrap = JsonUtility.FromJson<Wrapper>(PlayerPrefs.GetString(KEY));

        foreach (var s in wrap.items)
        {
            var data = db.Find(s.stoneName);
            if (!data) continue;

            var go = Object.Instantiate(data.prefab, s.pos, s.rot, parent);
            var st = go.GetComponent<Stone>();
            st.Init(data);
            outList.Add(st);
        }
    }

    [System.Serializable] class Wrapper { public List<Snapshot> items; public Wrapper(List<Snapshot> l) { items = l; } }
}