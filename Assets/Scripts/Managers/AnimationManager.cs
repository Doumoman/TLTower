using System.Collections;
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
    }

    [Header("☁ Cloud Move Animation")]
    [Tooltip("애니메이션을 재생할 패널 루트 오브젝트")]
    [SerializeField] GameObject panel;
    [Tooltip("패널 왼쪽 구름(오른쪽으로 이동)")]
    [SerializeField] RectTransform leftCloud;
    [Tooltip("패널 오른쪽 구름(왼쪽으로 이동)")]
    [SerializeField] RectTransform rightCloud;

    [Tooltip("X축 이동 거리 (+왼쪽 → 오른쪽, -오른쪽 → 왼쪽)")]
    [SerializeField] float xDistance = 1400f;
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
}