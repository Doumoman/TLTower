using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// • TickManager·FlowerPoint 없이 ‘정해진 5개 돌’만 배치  
/// • 돌은 MainStoneController 두 상태(Settled·Dragging)로 동작  
/// • StoneFreezer 자동 제거
/// </summary>
public class PresetStoneSpawnerMain : MonoBehaviour
{
    [System.Serializable]
    public struct StonePreset
    {
        public StoneData stoneData;   // 사용할 SO
        public int spriteIndex; // sprites[] 인덱스
        public Vector3 position;    // 월드 좌표 // z좌표 추가해서 돌 잘보이게 수정함
        public float rotationZ;   // Z축 회전
    }

    [Header("스폰시킬 돌")]
    public List<StonePreset> presets = new(5);

    [Header("부모 트랜스폼 (없으면 자동 생성)")]
    public Transform stonesParent;

    const string PP_KEY = "StoneTimelinePlayed";
    bool groundTest = false;

    void Awake()
    {
        if (!stonesParent)
            stonesParent = new GameObject("Stones").transform;
        groundTest = FindAnyObjectByType<StoneTimelineTrigger>()?.groundTest ?? true;
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(PP_KEY, 0) == 1 && !groundTest) return;

        foreach (var p in presets)
            SpawnSingleStone(p);
    }

    /* ─────────────────────────────────────────── */
    void SpawnSingleStone(StonePreset p)
    {
        /* 1) 프리팹 인스턴스 */
        Sprite spr = p.stoneData.GetSprite(p.spriteIndex);
        GameObject go = Instantiate(p.stoneData.backgroundPrefab,
                                     p.position,
                                     Quaternion.Euler(0, 0, p.rotationZ),
                                     stonesParent);

        /* 2) MainStoneController 확보 */
        var mc = go.GetComponent<MainStoneController>() ??
                 go.AddComponent<MainStoneController>();

        /* 3) Rigidbody2D 준비 + 파라미터 적용
              (MainStoneController.Awake 에서 자동 생성되지만
               mass·angularDrag 값을 넘기려면 Init 이전에 설정)         */
        var rb = go.GetComponent<Rigidbody2D>();
        rb.mass = p.stoneData.mass;
        rb.angularDrag = p.stoneData.angularDrag;
        rb.gravityScale = 1f;

        /* 4) MainStone 초기화 (스프라이트 등) */
        mc.Init(spr, p.stoneData.mass, p.stoneData.angularDrag);
        // MainStoneController 는 기본이 Settled 상태이므로 추가 설정 불필요

        /* 5) StoneFreezer 제거 (필요 없으므로) */
        if (go.TryGetComponent<StoneFreezer>(out var freezer))
            Destroy(freezer);
    }
}
