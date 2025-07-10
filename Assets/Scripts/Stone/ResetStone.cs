using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class ResetStone : MonoBehaviour
{
    public static ResetStone Instance { get; private set; }
    public GameObject platform;

    StoneFixer sf;
    int currentWave = 0;
    StoneController sc = null;
    ChapterManager cm;
    GameObject lastPlatform;
    private void Awake()
    {
        cm = ChapterManager.Instance;
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        cm = ChapterManager.Instance;
        sf = StoneFixer.Instance;
    }
    public void DestroyStones()
    {
        foreach (var s in sf.GetBatch().ToList())
        {
            sf.NotifyStoneLost(s);
            Destroy(s.gameObject);
        }

        if (sc != null)
        {
            Vector2 highPos = new Vector2(0, sc.transform.position.y);
            sf.SetY(highPos.y);

            if (currentWave > 0)
            {
                CreatePlatform();
            }
        }
    }

    //초기화시 위치 기준이 되는 돌의 stonecontroller를 얻음
    public void GetSc(StoneController s)
    {
        sc = s;
        currentWave = sf.GetWave();
    }
    public void ColToSc(Collider2D col)
    {
        sc = col.GetComponent<StoneController>();
        currentWave = sf.GetWave();
    }

    public void CreatePlatform()
    {
        
        float y = StoneFixer.Instance.HighestFixedY;
        Vector2 pos = new Vector2(0f, y);

        currentWave = -1;
        SpawnPlatformAt(pos);
    }

    void SpawnPlatformAt(Vector3 pos)
    {
        // 기존 플랫폼이 있으면 없애기
        if (lastPlatform != null) Destroy(lastPlatform);

        // 새 플랫폼 생성 & 기록
        lastPlatform = Instantiate(platform, pos, Quaternion.identity);
    }
    public void CreatePlatform(Vector2 spawnPos)
    {
        Vector3 pos = new Vector3(spawnPos.x, spawnPos.y, -8f);
        SpawnPlatformAt(pos);
    }

}
