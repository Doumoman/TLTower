using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    /* ────── 싱글톤 기본 ────── */
    public static AnimationManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (!stonesParent)
            stonesParent = new GameObject("SpaceStones").transform;
    }

    [Header("☁ Cloud Move Animation")]
    [Tooltip("애니메이션을 재생할 패널 루트 오브젝트")]
    [SerializeField] GameObject panel;
    [Tooltip("패널 왼쪽 구름(오른쪽으로 이동)")]
    [SerializeField] RectTransform leftCloud;
    [Tooltip("패널 오른쪽 구름(왼쪽으로 이동)")]
    [SerializeField] RectTransform rightCloud;

    [Tooltip("X축 이동 거리 (+왼쪽 → 오른쪽, -오른쪽 → 왼쪽)")]
    [SerializeField] float xDistance = 2000f;
    [Tooltip("Y축 하강 거리 (양쪽 공통, 양수면 아래로)")]
    [SerializeField] float yDistance = 1000f;
    [Tooltip("X축 이동 시간")]
    [SerializeField] float xTime = .4f;
    [Tooltip("Y축 이동 시간")]
    [SerializeField] float yTime = .25f;

    public void Play()
    {
        if (panel == null || leftCloud == null || rightCloud == null)
        {
            Debug.LogWarning("AnimationManager: 패널/오브젝트가 지정되지 않았습니다!");
            return;
        }
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        panel.SetActive(true);

        Vector2 lStart = leftCloud.anchoredPosition;
        Vector2 rStart = rightCloud.anchoredPosition;

        Vector2 lEndX = lStart + Vector2.right * xDistance; 
        Vector2 rEndX = rStart + Vector2.left * xDistance; 
        yield return MovePairOverTime(leftCloud, lStart, lEndX,
                                      rightCloud, rStart, rEndX, xTime);
        yield return new WaitForSeconds(1f);
        Vector2 lEndY = lEndX + Vector2.down * yDistance;
        Vector2 rEndY = rEndX + Vector2.down * yDistance;
        yield return MovePairOverTime(leftCloud, lEndX, lEndY,
                                      rightCloud, rEndX, rEndY, yTime);

        panel.SetActive(false);
        leftCloud.anchoredPosition = lStart;
        rightCloud.anchoredPosition = rStart;
    }
    static IEnumerator MovePairOverTime(RectTransform a, Vector2 aFrom, Vector2 aTo,
                                        RectTransform b, Vector2 bFrom, Vector2 bTo,
                                        float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            a.anchoredPosition = Vector2.Lerp(aFrom, aTo, k);
            b.anchoredPosition = Vector2.Lerp(bFrom, bTo, k);
            yield return null;
        }
        a.anchoredPosition = aTo;
        b.anchoredPosition = bTo;
    }

    [System.Serializable]
    public struct StonePreset
    {
        public StoneData stoneData;   // 사용할 SO
        public int spriteIndex; // sprites[] 인덱스
        public Vector2 position;    // 월드 좌표
        public float rotationZ;   // Z축 회전
    }

    [Header("★ Space Animation")]

    [Header("스폰시킬 돌")]
    public List<StonePreset> presets = new(5);
    [Header("돌 사라지는거 방지")]
    public GameObject Block;
    [Header("염주 제거")]
    public GameObject Yumju;
    [Header("부모 트랜스폼 (없으면 자동 생성)")]
    public Transform stonesParent;


    public void SpawnSpaceStones()
    {
        Block.SetActive(true);
        Yumju.SetActive(false);
        TickManager.Instance.StopTick();
        // 부모가 없으면 자동 생성 (이전 로직 유지)
        if (!stonesParent)
            stonesParent = new GameObject("Stones").transform;

        // 준비된 프리셋 순서대로 스폰
        for (int i = 0; i < presets.Count; i++)
            SpawnSingleStone(presets[i], i);
    }
    void SpawnSingleStone(StonePreset p, int index)
    {
        GameObject go = Instantiate(
            p.stoneData.backgroundPrefab,       // 프리팹
            p.position,                         // 위치
            Quaternion.Euler(0, 0, p.rotationZ),// 회전
            stonesParent);                      // 부모

        go.name = $"SpaceStone_{index}";

        var sr = go.GetComponent<SpriteRenderer>() ??
                 go.AddComponent<SpriteRenderer>();

        var rb = go.GetComponent<Rigidbody2D>() ??
                 go.AddComponent<Rigidbody2D>();

        var mc = go.GetComponent<SpaceStoneController>() ??
                 go.AddComponent<SpaceStoneController>();

        Sprite spr = p.stoneData.GetSprite(p.spriteIndex);
        sr.sprite = spr;

        rb.mass = p.stoneData.mass;
        rb.angularDrag = p.stoneData.angularDrag;
        rb.gravityScale = 0f;               // Space → 무중력

        mc.Init(spr, index, rb.mass, rb.angularDrag);

        if (go.TryGetComponent<StoneFreezer>(out var freezer))
            Destroy(freezer);
    }
}