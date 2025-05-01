using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StoneSpawner : MonoBehaviour
{
    /* ────────────────── 싱글톤 ────────────────── */
    public static StoneSpawner Instance { get; private set; }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /* ───────────── 인스펙터 설정 ───────────── */
    [Header("Prefabs  (타입 순서 맞추기)")]
    public List<GameObject> backgroundStonePrefabs;   // Stub 용
    public List<GameObject> playableStonePrefabs;     // 실제 돌

    [Header("Spawn Slots (4개)")]
    public Transform[] spawnSlots = new Transform[4]; // 슬롯 위치 4개 Drag&Drop

    [Header("Options")]
    public float spawnDelay = 4f;                     // 집은 후 Stub 재생성 지연
    public Transform stonesParent;                    // 모든 돌 parent

    /* ───────────── 내부 상태 ───────────── */
    readonly Dictionary<Transform, StoneController> slotToStub = new();   // 슬롯 ↔ 현재 Stub
    readonly Dictionary<Transform, Coroutine> slotTimer = new();   // 슬롯 ↔ 지연 코루틴
    readonly List<StoneController> active = new();   // 모든 돌

    /* ───────────── 초기 Stub 4개 ───────────── */
    void Start()
    {
        if (!stonesParent) stonesParent = new GameObject("Stones").transform;

        int avail = Mathf.Min(backgroundStonePrefabs.Count, playableStonePrefabs.Count);
        if (avail == 0 || spawnSlots.Any(s => s == null))
        {
            Debug.LogError("StoneSpawner ▶ 프리팹 or 슬롯 설정 오류"); enabled = false; return;
        }

        // 슬롯마다 Stub 1개씩 배치
        foreach (var slot in spawnSlots) CreateStubAtSlot(slot);
    }

    /* ══════════════════════════════════
     *  Stub 생성 / 제거 / 재생성 타이머
     * ══════════════════════════════════ */
    void CreateStubAtSlot(Transform slot)
    {
        // 랜덤 타입 선택
        int avail = Mathf.Min(backgroundStonePrefabs.Count, playableStonePrefabs.Count);
        int idx = Random.Range(0, avail);

        GameObject go = Instantiate(backgroundStonePrefabs[idx],
                                    slot.position, Quaternion.identity, slot); // 부모 = 슬롯
        var sc = go.GetComponent<StoneController>() ?? go.AddComponent<StoneController>();

        Sprite spr = go.GetComponent<SpriteRenderer>()?.sprite;
        sc.InitAsBackground(idx, spr);

        slotToStub[slot] = sc;
        active.Add(sc);
    }

    

    IEnumerator RespawnAfterDelay(Transform slot)
    {
        yield return new WaitForSeconds(spawnDelay);
        CreateStubAtSlot(slot);
        slotTimer.Remove(slot);
    }

    /* ───────────── 외부 API ───────────── */

    // ① Stub 클릭 → 플레이어블 돌로 변환 & 즉시 드래그
    public void SpawnPlayableAndBeginDrag(StoneController stub)
    {
        Transform slot = stub.transform.parent;
        int typeIdx = stub.stoneTypeIndex;
        int avail = Mathf.Min(backgroundStonePrefabs.Count, playableStonePrefabs.Count);
        if (typeIdx < 0 || typeIdx >= avail) { Debug.LogError("잘못된 typeIdx"); return; }

        // 플레이어블 파라미터
        var prefab = playableStonePrefabs[typeIdx];
        Sprite spr = prefab.GetComponent<SpriteRenderer>()?.sprite;
        float mass = prefab.GetComponent<Rigidbody2D>()?.mass ?? 1f;

        // Stub → 플레이어블 전환
        stub.InitAsPlayable(spr, mass);

        // 슬롯에서만 분리, 파괴 X
        RemoveStubFromSlot(slot);

        // 드래그 로직은 StoneController 에서 계속
    }

    // 슬롯과의 매핑만 제거, 오브젝트는 살려둠
    void RemoveStubFromSlot(Transform slot)
    {
        if (slotToStub.TryGetValue(slot, out var sc))
        {
            slotToStub.Remove(slot);
            sc.transform.SetParent(stonesParent);   // 슬롯 부모 분리
            // Destroy 안 함 → 오브젝트 유지
        }
    }

    // ② Stub 삭제용 (Background 상태인 경우에만)
    public void RemoveStub(StoneController stub)
    {
        if (stub.state != StoneState.Background) return;
        Transform slot = stub.transform.parent;
        RemoveStubFromSlot(slot);
    }

    // ③ Placed 통보 (타이머 로직은 Stub 쪽에서 이미 돌고 있으므로 목록 관리만)
    public void NotifyPlaced(StoneController sc)
    {
        if (!active.Contains(sc)) active.Add(sc);
    }

    // ④ 외부에서 “n초 뒤 그 슬롯에 Stub” 직접 예약하고 싶을 때
    public void ScheduleRandomStone(float delay)  // StoneController 에서 그대로 호출 가능
    {
        // 가장 최근에 비어진 슬롯(=타이머 없는 첫 슬롯) 찾아 예약
        foreach (var slot in spawnSlots)
        {
            if (slotToStub.ContainsKey(slot)) continue;  // 이미 Stub 있음
            if (slotTimer.ContainsKey(slot)) continue;  // 이미 타이머 돌고 있음
            slotTimer[slot] = StartCoroutine(RespawnAfterDelayCustom(slot, delay));
            break;
        }
    }
    IEnumerator RespawnAfterDelayCustom(Transform slot, float d)
    {
        yield return new WaitForSeconds(d);
        CreateStubAtSlot(slot);
        slotTimer.Remove(slot);
    }
}