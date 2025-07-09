using UnityEngine;
using UnityEngine.Playables;

/// 트리거 안의 PlacedStone 개수를 매 프레임 재계산해 Timeline을 재생
[RequireComponent(typeof(Collider2D))]
public class StoneTimelineTrigger : MonoBehaviour
{
    [Header("조건 설정")]
    [SerializeField] string stoneTag = "PlacedStone";
    [SerializeField] int requiredCount = 5;
    [SerializeField] bool playOnlyOnce = true;

    [Header("타임라인")]
    [SerializeField] PlayableDirector director;   // Inspector에서 할당

    /* ───────── 내부 ───────── */
    Collider2D triggerCol;
    ContactFilter2D filter;       // 모든 Collider 허용 (NoFilter)
    int lastCount = -1;           // 로그 스팸 방지용
    bool alreadyPlayed;

    void Awake()
    {
        triggerCol = GetComponent<Collider2D>();
        triggerCol.isTrigger = true;

        // ContactFilter 기본값: Nothing → NoFilter()로 초기화
        filter.NoFilter();
        filter.useTriggers = true;   // 트리거 콜라이더도 포함

    }

    void Update()
    {
        int cur = CountStonesInside();


        // 타임라인 재생 조건
        if ((!alreadyPlayed || !playOnlyOnce) && cur >= requiredCount)
        {
            director.Play();
            alreadyPlayed = true;
            ChapterSoundManager.Instance.NextState();
        }
    }

    /* ───────── 현재 트리거 내부 돌 개수 계산 ───────── */
    int CountStonesInside()
    {
        // NonAlloc 방식으로 GC 최소화
        const int Max = 64;  // 트리거 안에 동시에 있을 수 있는 최대 콜라이더 수
        Collider2D[] results = new Collider2D[Max];

        int hit = triggerCol.OverlapCollider(filter, results);

        int count = 0;
        for (int i = 0; i < hit; i++)
        {
            if (results[i] != null && results[i].CompareTag(stoneTag))
                count++;
        }
        return count;
    }
}