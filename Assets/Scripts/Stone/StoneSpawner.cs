using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StoneSpawner : MonoBehaviour
{
    public static StoneSpawner Instance { get; private set; }
    [Header("Stone 목록 (SO)")]
    public List<StoneData> stoneDataList;      // Inspector에서 SO Drag & Drop
    public List<StoneData> penaltyStoneData; // Penalty 돌 데이터

    [Header("Spawn Slots (FlowerPoint 태그)")]
    public List<Transform> spawnSlots = new();         // Inspector 필요 X

    readonly HashSet<Transform> knownSlots = new();

    [Header("기타 옵션")]
    public float defaultSpawnDelay = 2f;
    //0이라면 tick이 얼마 남지 않았을 경우 바로 스폰하여 좋지 않으므로 tick이 2초 이하 남았다면 다음 tick에 스폰하도록 설정
    public Transform stonesParent;
    ChapterManager cm;
    readonly Dictionary<Transform, StoneController> slotToStub = new();
    readonly Dictionary<Transform, Coroutine> slotTimer = new();
    readonly List<StoneController> active = new();
    readonly List<Transform> pendingSlots = new();
    static readonly WaitForSeconds FlowerPointPollDelay = new WaitForSeconds(0.5f);
    TickManager tickManager;

    public static bool Penalty = false; // true면 다음 돌이 번뇌돌, PenaltyManager에서 관리
    void Awake()
    {
        cm = ChapterManager.Instance;
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    void Start()
    {
        cm = ChapterManager.Instance;
        if (!stonesParent) stonesParent = new GameObject("Stones").transform;

        tickManager = TickManager.Instance;
        tickManager.OnTickEvent += HandleTick;

        StartCoroutine(MonitorFlowerPoints());
    }

    void HandleTick(object sender, System.EventArgs eventArgs)
    {
        foreach (Transform slot in pendingSlots)
            CreateStubAtSlot(slot);

        pendingSlots.Clear();
    }

    void OnDestroy()
    {
        if (tickManager != null)
            tickManager.OnTickEvent -= HandleTick;

        if (Instance == this)
            Instance = null;
    }

    IEnumerator MonitorFlowerPoints()
    {
        while (true)
        {
            // 현재 씬에 존재하는 FlowerPoint 전부 스캔
            GameObject[] flowerPoints = GameObject.FindGameObjectsWithTag("FlowerPoint");
            foreach (GameObject flowerPoint in flowerPoints)
            {
                Transform tr = flowerPoint.transform;
                // 아직 등록되지 않은 슬롯이면 즉시 추가 + Stub 생성
                if (!knownSlots.Contains(tr))
                {
                    knownSlots.Add(tr);
                    spawnSlots.Add(tr);
                    CreateStubAtSlot(tr);
                }
            }

            /* 옵션: 사라진 슬롯 제거
            knownSlots.RemoveWhere(t => t == null);
            spawnSlots.RemoveAll(t => t == null);
            */

            yield return FlowerPointPollDelay;   // 주기 조정 가능
        }
    }

    StoneData GetStoneDataById(int id) =>
    stoneDataList.Find(d => d.typeId == id);
    // Stub 생성 
    void CreateStubAtSlot(Transform slot)
    {
        if (cm.chapter == chapter.space || IsAutumnChapter(cm.chapter))
        {
            Debug.Log("생성금지");
            return;
        }
        StoneData data = Penalty ? GetPenaltyStoneData()
                             : GetRandomStoneData();

        int idx;
        Sprite spr = data.GetRandomSprite(out idx);

        GameObject go = Instantiate(data.backgroundPrefab, slot.position,
                                       Quaternion.identity, slot);

        var sc = go.GetComponent<StoneController>() ?? go.AddComponent<StoneController>();
        sc.SetTypeId(data.typeId);
        sc.SetSpriteIndex(idx);
        sc.InitAsBackground(data, spr);

        if (!go.TryGetComponent<StoneFreezer>(out StoneFreezer sf)) go.AddComponent<StoneFreezer>();

        slotToStub[slot] = sc;
        active.Add(sc);
    }

    // 스폰 확률 계산 
    StoneData GetRandomStoneData()
    {
        float total = 0f;
        foreach (StoneData data in stoneDataList)
            total += data.spawnChance;

        float r = Random.value * total;
        float acc = 0f;

        foreach (var d in stoneDataList)
        {
            acc += d.spawnChance;
            if (r <= acc) return d;
        }
        return stoneDataList[0]; // fallback
    }

    StoneData GetPenaltyStoneData()
    {
        Penalty = false; // Penalty 상태 초기화
        GuideManager.Instance.PlayGuide("penalty");
        return penaltyStoneData[0];
    }

    // Stub → Playable 변환 
    int currentOrder;
    public void SpawnPlayableAndBeginDrag(StoneController stub)
    {
        StoneData data = stub.Data;

        // Stub 오브젝트를 바로 Playable로 변환
        stub.InitAsPlayable(data, stub.GetComponent<SpriteRenderer>().sprite);
        BringToFront(stub.GetComponent<SpriteRenderer>());

        // 슬롯 해제 & 재스폰 예약
        Transform slot = stub.transform.parent;
        RemoveStubFromSlot(slot);
        ScheduleStub(slot, defaultSpawnDelay);
    }

    void BringToFront(SpriteRenderer sr)
    {
        sr.sortingOrder = ++currentOrder;
        var outline = sr.transform.Find("Outline");
        if (outline && outline.TryGetComponent(out SpriteRenderer osr))
            osr.sortingOrder = sr.sortingOrder - 1;
    }

    // Stub 관리
    void RemoveStubFromSlot(Transform slot)
    {
        if (slotToStub.TryGetValue(slot, out var sc))
        {
            slotToStub.Remove(slot);
            sc.transform.SetParent(stonesParent);
        }
    }

    void ScheduleStub(Transform slot, float delay)
    {
        if (slotTimer.ContainsKey(slot)) return;
        slotTimer[slot] = StartCoroutine(RespawnCoroutine(slot, delay));
    }
    IEnumerator RespawnCoroutine(Transform slot, float delay)
    {
        yield return new WaitForSeconds(delay);
        TickCreateStubAtSlot(slot);
        slotTimer.Remove(slot);
    }

    void TickCreateStubAtSlot(Transform slot)
    {
        if (cm.chapter == chapter.space || IsAutumnChapter(cm.chapter))
        {
            Debug.Log("생성금지");
            return;
        }
        pendingSlots.Add(slot);
    }

    public void NotifyPlaced(StoneController sc)
    {
        // 아직 목록에 없으면 추가
        if (!active.Contains(sc))
            active.Add(sc);
    }

    // 외부 호출용 쇼트컷 
    public void ScheduleRandomStone(float delay)
    {
        foreach (Transform slot in spawnSlots)
        {
            if (slotToStub.ContainsKey(slot) || slotTimer.ContainsKey(slot))
                continue;

            ScheduleStub(slot, delay);
            break;
        }
    }

    static bool IsAutumnChapter(chapter value) =>
        value >= chapter.autumn && value <= chapter.autumn11;

    StoneData GetStoneDataByIndex(int idx)
    {
        if (idx < 0 || idx >= stoneDataList.Count)
        {
            Debug.LogError($"잘못된 stoneTypeIndex = {idx}");
            return stoneDataList[0];
        }
        return stoneDataList[idx];
    }
    public StoneController SpawnFixedStone(int typeId, int spriteIdx, Vector2 pos, float rotZ)
    {
        StoneData data = GetStoneDataById(typeId);
        Sprite spr = data.GetSprite(spriteIdx);      // 같은 모양을 저장하려면 spriteIndex도 SaveData에 저장

        // 프리팹 인스턴스화 (parent는 Stones 폴더)
        GameObject go = Instantiate(
            data.backgroundPrefab,
            pos,
            Quaternion.Euler(0, 0, rotZ),
            stonesParent);

        // 스프라이트/Collider 셋업
        var sc = go.GetComponent<StoneController>() ?? go.AddComponent<StoneController>();
        sc.SetTypeId(typeId);
        sc.SetSpriteIndex(spriteIdx);
        sc.InitAsBackground(data, spr);  // Collider 두 개 생성
        sc.SetFixed();                   // 상태·태그 → Fixed

        // 3) 내부 리스트 관리
        if (!active.Contains(sc))
            active.Add(sc);

        return sc;
    }
    public void ResetAfterClear()
    {
        // 배경 돌·Active 리스트·코루틴 모두 초기화
        foreach (var kv in slotTimer)
            if (kv.Value != null) StopCoroutine(kv.Value);
        slotTimer.Clear();

        slotToStub.Clear();
        active.Clear();
    }
    public void RebuildStubSlots()
    {
        foreach (var slot in spawnSlots)
        {
            // 혹시 남아 있는 자식 오브젝트가 있으면 제거
            foreach (Transform child in slot) Destroy(child.gameObject);

            // 새 Stub 하나 생성
            CreateStubAtSlot(slot);
        }
    }
}
