using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StoneSpawner : MonoBehaviour
{
    public static StoneSpawner Instance { get; private set; }
    public static bool Penalty = false; // true면 다음 돌이 번뇌돌, PenaltyManager에서 관리
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Stone 목록 (SO)")]
    public List<StoneData> stoneDataList;      // Inspector에서 SO Drag & Drop
    public List<StoneData> penaltyStoneData; // Penalty 돌 데이터

    [Header("Spawn Slots (4개)")]
    public Transform[] spawnSlots = new Transform[4];

    [Header("기타 옵션")]
    public float defaultSpawnDelay = 2f;
    //0이라면 tick이 얼마 남지 않았을 경우 바로 스폰하여 좋지 않으므로 tick이 2초 이하 남았다면 다음 tick에 스폰하도록 설정
    public Transform stonesParent;

    readonly Dictionary<Transform, StoneController> slotToStub = new();
    readonly Dictionary<Transform, Coroutine> slotTimer = new();
    readonly List<StoneController> active = new();

    private List<System.Action> Actions = new(); // TickManager에서 호출할 액션 목록
    void Start()
    {
        if (!stonesParent) stonesParent = new GameObject("Stones").transform;
        if (stoneDataList.Count == 0 || spawnSlots.Any(s => s == null))
        {
            Debug.LogError("StoneSpawner ▶ SO or Slot 설정 오류"); enabled = false; return;
        }

        foreach (var slot in spawnSlots) CreateStubAtSlot(slot);

        TickManager.Instance.OnTickEvent += (sender, eventArgs) =>
        {
            foreach (var action in Actions)
                action.Invoke();
            
            Actions.Clear();
        }; // Tick에 액션 등록 후 실행
    }

    // Stub 생성 
    void CreateStubAtSlot(Transform slot)
    {
        StoneData data;
        Sprite chosenSpr;
        if (Penalty)
        {
            data = GetPenaltyStoneData();
            chosenSpr = data.GetRandomSprite();
        }
        else
        {
            data = GetRandomStoneData();
            chosenSpr = data.GetRandomSprite();
        }

        GameObject go = Instantiate(data.backgroundPrefab, slot.position,
                                       Quaternion.identity, slot);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr) sr.sprite = chosenSpr;

        var sc = go.GetComponent<StoneController>() ?? go.AddComponent<StoneController>();
        sc.InitAsBackground(data, chosenSpr);

        if (!go.TryGetComponent<StoneCounter>(out StoneCounter scnt)) go.AddComponent<StoneCounter>();
        if (!go.TryGetComponent<StoneFreezer>(out StoneFreezer sf)) go.AddComponent<StoneFreezer>();

        slotToStub[slot] = sc;
        active.Add(sc);
    }

    // 스폰 확률 계산 
    StoneData GetRandomStoneData()
    {
        float total = stoneDataList.Sum(d => d.spawnChance);
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
        Actions.Add(() => CreateStubAtSlot(slot));
    }

    public void NotifyPlaced(StoneController sc)
    {
        // 아직 목록에 없으면 추가
        if (!active.Contains(sc))
            active.Add(sc);
    }

    // 외부 호출용 쇼트컷 
    public void ScheduleRandomStone(float delay) =>  // 기존 API 유지
        spawnSlots.FirstOrDefault(s => !slotToStub.ContainsKey(s) && !slotTimer.ContainsKey(s))
                   ?.Let(slot => ScheduleStub(slot, delay));
}