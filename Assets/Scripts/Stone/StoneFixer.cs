using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class StoneFixer : MonoBehaviour
{
    public static StoneFixer Instance { get; private set; }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Settings")]
    public int threshold = 48;                   // 48개씩 묶음

    [Header("UI")]
    public TextMeshProUGUI remainingTMP;         // 남은 돌 표시

    [Header("SavePoint")]
    public GameObject savePointPrefab;                 // 막대 프리팹 (IsTrigger 콜라이더 포함)
    GameObject currentSavePoint;
    public float HighestSettledY { get; private set; } = 0f;
    public float HighestFixedY { get; private set; } = 0f;

    readonly List<StoneController> batch = new();   // 이번 라운드 Settled
    int wave = 0;                                   // 몇 번째 묶음인지

    // StoneController 가 Settled 될 때마다 호출
    public void RegisterSettled(StoneController sc)
    {
        if (sc.state != StoneState.Settled) return;

        float topY = sc.transform.position.y;

        if (sc.TryGetComponent(out Collider2D col))
        {
            topY = col.bounds.max.y;
        }
        // 최고 높이 갱신
        if (topY > HighestSettledY)
        {
            HighestSettledY = topY;
            CameraController.Instance.CenterOnY(HighestSettledY);
        }
        if (sc.transform.position.y > HighestFixedY)
            HighestFixedY = sc.transform.position.y;
        // 중복 방지

        if (!batch.Contains(sc)) { batch.Add(sc); ChapterManager.Instance?.AddCount(); }//ChapterManger 카운트 올리기
        UpdateUI();

        //조건 달성: 세이브포인트 생성 
        if (batch.Count >= threshold && currentSavePoint == null)
        {
            CheckAndSound();//배경음악 Ambience로 전환
            wave++;
            Vector3 spawnPos = new(
                0f,   // 가장 최근 돌의 X (원한다면 0 또는 중앙값으로)
                HighestSettledY + 2f,
                0f);
            currentSavePoint = Instantiate(savePointPrefab, spawnPos, Quaternion.identity);
            // SavePoint 스크립트에 StoneFixer 참조를 자동으로 넘기려면 다음 라인 추가
            currentSavePoint.GetComponent<SavePoint>()?.Init(this);

            Debug.Log($"[StoneFixer] Wave {wave} reached. SavePoint spawned at {spawnPos}");
        }
    }

    void UpdateUI()
    {
        if (!remainingTMP) return;

        int remain = Mathf.Max(0, threshold - batch.Count); // 0 이하 방지
        remainingTMP.text = $"<b>{remain}</b>";
    }
    #region 돌 고정 로직
    public void FixAllStones()
    {
        if (batch.Count == 0) return;
        ResetStone.Instance.CreatePlatform();
        // Pile 루트 생성
        GameObject pileRoot = new($"StonePile_Wave{wave}");
        var pileRB = pileRoot.AddComponent<Rigidbody2D>();
        pileRB.bodyType = RigidbodyType2D.Static;   // Static
        var pileCC = pileRoot.AddComponent<CompositeCollider2D>();
        pileCC.geometryType = CompositeCollider2D.GeometryType.Polygons; // Outlines

        // 이번 wave 의 돌들을 자식으로 옮기고 usedByComposite 설정
        foreach (var s in batch)
        {
            if (s.state != StoneState.Settled && s.state != StoneState.Fixed) continue;

            // 돌 상태 Fixed
            s.SetFixed();

            // Rigidbody 제거 → 정적 Collider 로 변환
            if (s.TryGetComponent(out Rigidbody2D rb))
                Destroy(rb);

            // Collider 를 Composite 로 편입
            if (s.TryGetComponent(out PolygonCollider2D pc2d))
                pc2d.usedByComposite = true;

            // Pile 루트의 자식으로 이동(월드 좌표 유지)
            s.transform.SetParent(pileRoot.transform, true);
        }

        // 내부 리스트 초기화·UI 리셋
        batch.Clear();
        UpdateUI();

        HighestFixedY = GetHighestFixedYInScene();
        CameraController.Instance.CenterOnY(HighestFixedY);

        // SavePoint 오브젝트 제거
        if (currentSavePoint)
        {
            StartCoroutine(RemoveSavePointAfterFade(currentSavePoint));
            currentSavePoint = null;          // 코루틴이 참조를 가지고 있으므로 안전
        }

        Debug.Log($"[StoneFixer] Wave {wave} fixed → PileCollider 생성");
        StartCoroutine(FuseAllStonesIntoOne());
    }
    IEnumerator RemoveSavePointAfterFade(GameObject sp)
    {
        if (!sp) yield break;

        var animator = sp.GetComponent<Animator>();
        if (animator && animator.runtimeAnimatorController)
        {
            Debug.Log("진입완료");
            const string fadeState = "SavePointFadeout";
            animator.speed = 1;

            // 스테이트 ‘강제’ 진입
            animator.Play(fadeState, 0, 0f);          // (layer = 0, normalizedTime = 0)

            while (true)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(fadeState) && info.normalizedTime >= 0.99f)
                    break;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }
        Destroy(sp);
    }
    IEnumerator FuseAllStonesIntoOne() // Pile된 객체들의 콜라이더를 하나의 콜라이더로 만들기
    {
        GameObject root = new("StonePile_All");
        var rootRb = root.AddComponent<Rigidbody2D>();
        rootRb.bodyType = RigidbodyType2D.Static;

        var comp = root.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons;

        // 돌 정리: 자식 편입 + usedByComposite
        foreach (var s in FindObjectsOfType<StoneController>())
        {
            if (s.state != StoneState.Settled && s.state != StoneState.Fixed) continue;

            // Rigidbody/Collider 유지한 채 자식으로
            s.transform.SetParent(root.transform, true);

            if (s.TryGetComponent(out PolygonCollider2D pc))
                pc.usedByComposite = true;
            if (s.TryGetComponent(out Rigidbody2D rb))
                rb.bodyType = RigidbodyType2D.Static;   // 물리 무력화
        }

        yield return new WaitForFixedUpdate();

        // 새 PolygonCollider2D에 경계 복사
        var poly = root.AddComponent<PolygonCollider2D>();
        poly.pathCount = comp.pathCount;
        var pts = new List<Vector2>();
        for (int i = 0; i < comp.pathCount; ++i)
        {
            pts.Clear();
            comp.GetPath(i, pts);
            poly.SetPath(i, pts.ToArray());
        }

        // 자식 돌의 Rigidbody/Collider 파괴
        foreach (Transform child in root.transform)
        {
            Destroy(child.GetComponent<Rigidbody2D>());
            Destroy(child.GetComponent<Collider2D>());

        }
        Destroy(comp);
    }
    #endregion
    float GetHighestFixedYInScene()
    {
        float top = 0f;
        foreach (var s in FindObjectsOfType<StoneController>())
            if (s.state == StoneState.Fixed && s.transform.position.y > top)
                top = s.transform.position.y;
        return top;
    }
    public void RefreshHeightsFromScene()
    {
        HighestSettledY = 0f;
        HighestFixedY = 0f;

        foreach (var s in FindObjectsOfType<StoneController>())
        {
            float y = s.transform.position.y;

            if (s.state == StoneState.Settled && y > HighestSettledY)
                HighestSettledY = y;

            if (s.state == StoneState.Fixed && y > HighestFixedY)
                HighestFixedY = y;
        }

        // Fixed 돌이 없으면 Settled 값이라도 써야 함
        if (HighestFixedY < HighestSettledY)
            HighestFixedY = HighestSettledY;
    }
    // 추락한 돌이 파괴되면 StoneDespawnZone → NotifyStoneLost 로 보고
    public void NotifyStoneLost(StoneController sc)
    {
        batch.Remove(sc);
        UpdateUI();
    }
    public List<StoneController> GetBatch() { return batch; }
    public int GetWave() { return wave; }
    public void SetY(float yPos) { HighestFixedY = yPos; }

    private void CheckAndSound()
    {
        var chap = ChapterManager.Instance.chapter;
        switch (chap)
        {
            case chapter.spring4:
                SoundManager.Instance.PlayBGM("Spring", 0);
                break;
            case chapter.summer5:
                SoundManager.Instance.PlayBGM("Summer", 0);
                break;
            case chapter.autumn11:
                SoundManager.Instance.PlayBGM("Autumn", 0);
                break;
            case chapter.winter4:
                SoundManager.Instance.PlayBGM("Winter", 0);
                break;
        }
    }
}