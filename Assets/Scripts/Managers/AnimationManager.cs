using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    /* ────── 싱글톤 기본 ────── */
    public static AnimationManager Instance { get; private set; }
    void Awake()
    {
        ChapterManager.Instance.removeYumju += RemoveYumju;
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
        [Tooltip("이 돌이 나타나기 전까지 기다릴 시간(초)")]
        public float spawnDelay;   // ★ 추가
    }

    [Header("★ Space Animation")]

    [Header("스폰시킬 돌")]
    public List<StonePreset> presets = new(5);

    [Header("Spawn Timing")]
    [Tooltip("Preset에 값이 없을 때 기본 대기 시간")]
    public float defaultSpawnDelay = 1.0f;

    [Tooltip("돌이 서서히 보이도록 하는 페이드‑인 시간")]
    public float fadeInDuration = 0.8f;

    [Header("✨ Glow Objects (순서 = presetIndex)")]
    public List<SpriteRenderer> glowObjects = new(5);   // 0~4
    [Tooltip("Glow가 그려질 SortingOrder (높을수록 앞)")]
    public int glowSortingOrder = 100;

    [Header("Glow Animation")]
    [Tooltip("처음 등장 시 0 → 1 로 페이드‑인되는 시간")]
    public float glowFadeIn = 0.35f;

    [Tooltip("깜빡이는 주기(초) ‑ 예: 0.8 → 0.5초마다 α최소/최대 교차")]
    public float glowBlinkPeriod = 0.8f;

    [Tooltip("깜빡임 최소 α")]
    [Range(0f, 1f)] public float glowMinAlpha = 0.5f;
    [Tooltip("깜빡임 최대 α")]
    [Range(0f, 1f)] public float glowMaxAlpha = 1f;

    [Tooltip("스냅 순간 ‘반짝’ 유지 시간")]
    public float glowFlashHold = 0.12f;
    [Tooltip("반짝 후 사라지는 페이드‑아웃 시간")]
    public float glowFadeOut = 0.3f;
    // 내부 코루틴 핸들 (스냅되면 강제 종료용)
    Coroutine[] glowCo = new Coroutine[5];

    [Header("돌 사라지는거 방지")]
    public GameObject Block;
    [Header("염주 제거")]
    public GameObject Yumju;
    [Header("부모 트랜스폼 (없으면 자동 생성)")]
    public Transform stonesParent;

    [Header("시퀀스매니저 할당")]
    public StoneSequenceManager seqManager;

    [Header("★ 우주 배경 애니메이션")]
    [SerializeField] GameObject spaceAnimRoot;   // Animator 가 달린 오브젝트
    [SerializeField] string animStateName = "Play"; // 첫 스테이트 이름
    [SerializeField] float startSpeed = 0.2f;   // 초반 재생 속도
    [SerializeField] float slowDuration = 3f;      // 느린 가속 구간(초)
    [SerializeField] float midSpeed = 0.5f;
    [SerializeField] float endSpeed = 2.0f;


    readonly int[] spawnSequence = { 4, 3, 1, 2, 0 };   // 원하는 순서
    int spawnStep = 0;
    int snappedCount = 0;
    bool cleared = false;
    public event EventHandler changeSpaceBackGround;

    public void SpawnSpaceStones()
    {
        Block.SetActive(true);
        TickManager.Instance.StopTick();

        snappedCount = 0;
        cleared = false;
        spawnStep = 0;          // ★ 리셋

        if (!stonesParent)
            stonesParent = new GameObject("Stones").transform;

        SpawnNextStone();          // 첫 돌(인덱스 4)만 생성
    }
    void SpawnNextStone()
    {
        if (spawnStep >= spawnSequence.Length) return;

        int presetIdx = spawnSequence[spawnStep];

        ShowGlow(presetIdx);

        float wait = presets[presetIdx].spawnDelay > 0f
                     ? presets[presetIdx].spawnDelay
                     : defaultSpawnDelay;

        StartCoroutine(CoSpawnAfterDelay(presetIdx, wait));
        spawnStep++;                     // 다음 인덱스로 미리 이동
    }

    IEnumerator CoSpawnAfterDelay(int presetIdx, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnSingleStone(presets[presetIdx], presetIdx);
    }
    int seq = 0;
    void SpawnSingleStone(StonePreset p, int index) // 여기서 인덱스 값에 따라 대사 나오게 하면 될듯
    {
        SoundManager.Instance.PlayBGM("Space", seq++);
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
        sr.color = new Color(1, 1, 1, 0);


        rb.mass = p.stoneData.mass;
        rb.angularDrag = p.stoneData.angularDrag;
        rb.gravityScale = 0f;               // Space → 무중력

        mc.Init(spr, index, rb.mass, rb.angularDrag);

        StartCoroutine(CoFadeIn(sr, fadeInDuration));
    }
    IEnumerator CoFadeIn(SpriteRenderer sr, float dur)
    {
        float t = 0f;
        Color c = sr.color;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / dur);
            sr.color = c;
            yield return null;
        }
        c.a = 1f;
        sr.color = c;
    }
    public void NotifyStoneSnapped()
    {
        if (cleared) return;

        snappedCount++;

        if (snappedCount < presets.Count)  // 아직 남은 돌이 있으면
            SpawnNextStone();              // 다음 돌 생성

        if (snappedCount >= presets.Count) // 5개 전부 스냅 완료
        {
            cleared = true;
            OnAllStonesSnapped();
        }
    }
    IEnumerator CoAccelerateAnimation(Animator anim)
    {
        float elapsed = 0f;
        float clipLen = anim.GetCurrentAnimatorStateInfo(0).length;
        bool pausedOnce = false;     // 1 회만 멈추도록 플래그

        /* 클립이 너무 짧을 때 대비 */
        if (clipLen < slowDuration)
            slowDuration = Mathf.Clamp(clipLen * 0.3f, 0.1f, clipLen);

        while (true)
        {
            elapsed += Time.deltaTime;
            AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(0);

            /* ① 슬로우 구간: startSpeed → midSpeed */
            if (elapsed <= slowDuration)
            {
                float k = elapsed / slowDuration;                    // 0→1
                anim.speed = Mathf.Lerp(startSpeed, midSpeed, k);
            }
            /* ② midSpeed 도달 직후 1 초 멈춤 (한 번만) */
            else if (!pausedOnce)
            {
                anim.speed = 0f;                                   // 완전 정지
                yield return new WaitForSecondsRealtime(1f);         // 실제 시간 1초
                anim.speed = midSpeed;                             // 다시 midSpeed
                pausedOnce = true;
            }
            /* ③ 정상 가속 구간: midSpeed → endSpeed */
            else
            {
                /* elapsed 기준을 slowDuration 이후부터 리매핑 */
                float k2 = Mathf.InverseLerp(slowDuration, clipLen, elapsed);
                anim.speed = Mathf.Lerp(midSpeed, endSpeed, k2);
            }

            /* 애니메이션이 끝났는지 체크 */
            if (s.normalizedTime >= 1f && !anim.IsInTransition(0))
                break;

            yield return null;
        }

        anim.speed = endSpeed;                   // 종료 속도 보정
        changeSpaceBackGround?.Invoke(this, EventArgs.Empty);
    }
    IEnumerator AfterSeconds(float seconds) // 마지막 돌이 끼워지면 해당 로직 실행
    {
        yield return new WaitForSeconds(seconds);
        //changeSpaceBackGround?.Invoke(this, EventArgs.Empty);
        seqManager.PlaySequence();
    }


    void OnAllStonesSnapped()
    {
        SoundManager.Instance.PlayBGM("Space", 5);
        Debug.Log("우주애니메이션 실행");
        foreach (var sc in stonesParent.GetComponentsInChildren<SpaceStoneController>())
            if (sc.State == SpaceStoneState.Snapped)
                Destroy(sc.gameObject);

        spaceAnimRoot.SetActive(true);
        //Animator anim = spaceAnimRoot.GetComponent<Animator>();

        StartCoroutine(AfterSeconds(12f)); // 마지막 돌이 끼워지면 해당 로직 실행
        Debug.Log("우주애니메이션 실행");
    }

    void RemoveYumju(object sender, EventArgs eventArgs)
    {
        Yumju.SetActive(false);
    }

    public void ShowGlow(int idx)
    {
        if (idx < 0 || idx >= glowObjects.Count) return;

        SpriteRenderer sr = glowObjects[idx];
        if (glowCo[idx] != null) StopCoroutine(glowCo[idx]);
        glowCo[idx] = StartCoroutine(CoGlowLoop(sr, idx));
    }
    IEnumerator CoGlowLoop(SpriteRenderer sr, int idx)
    {
        sr.gameObject.SetActive(true);
        sr.sortingOrder = glowSortingOrder;

        Color c = sr.color;
        /* ── ① 처음엔 α 0 ── */
        c.a = 0f;
        sr.color = c;

        /* ── ② 페이드‑인 (0 → 1) ── */
        for (float t = 0; t < glowFadeIn; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(0f, 1f, t / glowFadeIn);
            sr.color = c;
            yield return null;
        }
        c.a = 1f; sr.color = c;

        /* ── ③ 깜빡임 루프 (0.5 ↔ 1) ── */
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            float ping = Mathf.PingPong(timer, glowBlinkPeriod) / (glowBlinkPeriod * 0.5f); // 0~1
            c.a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, ping);
            sr.color = c;
            yield return null;
        }
    }
    public void FlashAndHide(int idx)
    {
        if (idx < 0 || idx >= glowObjects.Count) return;

        if (glowCo[idx] != null) StopCoroutine(glowCo[idx]);
        glowCo[idx] = StartCoroutine(CoFlashAndHide(glowObjects[idx], idx));
    }

    IEnumerator CoFlashAndHide(SpriteRenderer sr, int idx)
    {
        Color c = sr.color;
        c.a = 1f; sr.color = c;          // 최대 밝기로 고정
        yield return new WaitForSeconds(glowFlashHold);

        for (float t = 0; t < glowFadeOut; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(1f, 0f, t / glowFadeOut);
            sr.color = c;
            yield return null;
        }
        sr.gameObject.SetActive(false);
        glowCo[idx] = null;
    }
}