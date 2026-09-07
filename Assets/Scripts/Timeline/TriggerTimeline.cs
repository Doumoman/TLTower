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
    const int MaxOverlapResults = 64;
    readonly Collider2D[] overlapResults = new Collider2D[MaxOverlapResults];
    bool alreadyPlayed;          // PlayerPrefs 로드 결과
    const string PP_KEY = "StoneTimelinePlayed";

    [SerializeField] public bool groundTest = true;
    void Start()
    {
        triggerCol = GetComponent<Collider2D>();
        triggerCol.isTrigger = true;

        // ContactFilter 기본값: Nothing → NoFilter()로 초기화
        filter.NoFilter();
        filter.useTriggers = true;   // 트리거 콜라이더도 포함

        alreadyPlayed = PlayerPrefs.GetInt(PP_KEY, 0) == 1;

        /* ----- 이미 재생한 적이 있으면 5초 지점부터 바로 실행 ----- */
        if (alreadyPlayed)
        {
            director.time = 6.0;  // 6초 시점
            director.Play();
            Debug.Log("다음 챕터 진입중...");
            SoundManager.Instance.PlaySFX("next_chapter");
            SoundManager.Instance.PlayBGMInstant("Spring", 2);
            BosalManager.Instance.ManualSpeakStop();
        }
        else
        {
            SoundManager.Instance.PlayBGM("Ground", 0);
            BosalManager.Instance.Speak("TempleStart");
        }
    }

    void Update()
    {
        if (alreadyPlayed && playOnlyOnce)
            return;

        int cur = CountStonesInside();


        // 타임라인 재생 조건
        if ((!alreadyPlayed || !playOnlyOnce) && cur >= requiredCount)
        {
            director.Play();
            alreadyPlayed = true;
            playOnlyOnce = true;

            PlayerPrefs.SetInt(PP_KEY, 1);
            PlayerPrefs.Save();
            BosalManager.Instance.ManualSpeakStop();
            Debug.Log("다음 챕터 진입중...");
            SoundManager.Instance.PlaySFX("next_chapter");
            SoundManager.Instance.PlayBGM("Spring", 2);
        }
    }

    /* ───────── 현재 트리거 내부 돌 개수 계산 ───────── */
    int CountStonesInside()
    {
        int hit = triggerCol.Overlap(filter, overlapResults);

        int count = 0;
        for (int i = 0; i < hit; i++)
        {
            if (overlapResults[i] != null && overlapResults[i].CompareTag(stoneTag))
                count++;
        }
        return count;
    }
}
