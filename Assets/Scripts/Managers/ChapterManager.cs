using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
// using static UnityEditor.Progress; // 오류가 나서 주석처리 0731 06:07 이동건
using UnityEngine.SceneManagement;
#if UNITY_EDITOR              // ← 에디터에서만 컴파일 0731 06:07 이동건
using static UnityEditor.Progress;

#endif




[System.Serializable]
public class SeasonObstacle
{
    public chapter season; // or Season enum
    public List<GameObject> obstacles;
}

public enum chapter
{
    //land,
    spring, spring2, spring3,spring4,
    summer, summer2, summer3, summer4,summer5,
    autumn, autumn2, autumn3, autumn4, autumn5, autumn6, autumn7, autumn8, autumn9, autumn10, autumn11,
    winter, winter2, winter3,winter4,
    space
}; //사운드 편의상 분류 수를 늘림
public class ChapterManager : MonoBehaviour
{
    public chapter chapter = chapter.spring;

    [Header("References")]
    public List<SeasonObstacle> seasonalObstacles;
    public static GameObject[] CloudCheckPoint;

    [Header("Settings")]
    [Tooltip("land챕터부터 space전(winter) 챕터 까지")] public int[] stonesForChapter = new int[(int)chapter.space];
    public float waitTimeBeforeChange = 1f;

    [Header("현재 스테이지 UI")]
    [SerializeField] TextMeshProUGUI currentStageTMP;
    [SerializeField] Vector2 currentStageOffset = new Vector2(36f, -32f);
    [SerializeField] Vector2 currentStageSize = new Vector2(360f, 80f);
    [SerializeField] float currentStageFontSize = 36f;
    [SerializeField] Color springStageColor = new Color32(190, 107, 127, 255);
    [SerializeField] Color summerStageColor = new Color32(82, 132, 88, 255);
    [SerializeField] Color autumnStageColor = new Color32(165, 105, 61, 255);
    [SerializeField] Color winterStageColor = new Color32(92, 132, 174, 255);

    int stoneCount = 0;
    Dictionary<GameObject, Coroutine> co = new Dictionary<GameObject, Coroutine>();
    List<GameObject> currentObstacles = new List<GameObject>();
    public event EventHandler onChapterChage;
    public event EventHandler onSetteled;
    public event EventHandler onDestroyed;
    public event EventHandler removeYumju;

    
    public static ChapterManager Instance { get; private set; }



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        if (stonesForChapter == null || stonesForChapter.Length != (int)chapter.space)  //chapter개수보다 하나 작으므로 +1 필요 없음
        {
            stonesForChapter = new int[(int)chapter.space];
        }

        if (seasonalObstacles == null || seasonalObstacles.Count != (int)chapter.space+1)  //chapter는 0부터 시작하므로 +1
        {
            List<SeasonObstacle> newItems = new List<SeasonObstacle>();
            //for (int i = 0; i < Mathf.Min(seasonalObstacles?.Count ?? 0, (int)chapter.space)+1; i++)
            foreach (SeasonObstacle se in seasonalObstacles)
            {
                newItems.Add(se); // 기존 값 유지
            }
            foreach (chapter c in Enum.GetValues(typeof(chapter)))
            {
                bool hasChapter = false;
                foreach (SeasonObstacle se in newItems)
                {
                    if (se.season != c) continue;
                    hasChapter = true;   //이미 추가된 챕터라면 건너뛰기
                }

                //새로운 챕터라면 추가하기!
                if (!hasChapter)
                {
                    SeasonObstacle se = new SeasonObstacle();
                    se.season = c;
                    newItems.Add(se);
                }
            }
            seasonalObstacles = newItems.OrderBy(se => se.season).ToList();
        }
    }

    private void Start()
    {
        if (!SceneManager.GetActiveScene().name.Contains("sky prototype"))SaveSystem.Instance?.LoadGame();
        if (chapter == chapter.space) return; // LoadChapter already handed off to the ending.
        //시작 챕터 감지
        chapter[] arr = (chapter[])System.Enum.GetValues(typeof(chapter));
        int idx = Array.IndexOf(arr, chapter);

        //List<GameObject>[] obList = { springObstacles, summerObstacles, autumnObstacles, winterObstacles };
        //if (idx > 0 && idx < 5) currentObstacles = obList[idx];
        SetObstacle();
        onChapterChage?.Invoke(this, EventArgs.Empty);
        Debug.Log("chaptermanager 챕터변환 실행");
        ConfigureCurrentStage();

        //가을챕터 체크포인트 설정
        int targetLayer = LayerMask.NameToLayer("SkySavePoint");    //  모든 오브젝트 찾기 (씬 전체)
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);  //  특정 레이어 필터링
        CloudCheckPoint = allObjects
            .Where(obj => obj.layer == targetLayer)
            .OrderBy(obj => obj.transform.position.y)   // Y좌표 오름차순
            .ToArray();
        if (chapter >= chapter.autumn && chapter <= chapter.autumn11)
            CloudSystem.Instance.SetSavePoint(CloudCheckPoint[(int)chapter - (int)chapter.autumn]);
    }

    //돌 개수 확인 후 챕터전환 확인
    public void ChangeChapter(bool checkpointReached = false)
    {

        chapter[] arr = (chapter[])System.Enum.GetValues(typeof(chapter));
        int idx = System.Array.IndexOf(arr, chapter);

        if (idx < arr.Count()-1 &&
            (checkpointReached || stoneCount >= stonesForChapter[idx]))  // 체크포인트 도착 후에는 돌 개수를 다시 검사하지 않음
        {
            chapter = arr[++idx];
            stoneCount = 0;
            ConfigureCurrentStage();
            Debug.Log(chapter);

            if (chapter == chapter.space)
            {
                // Enter space logically to stop normal input/spawning, but keep the
                // winter background/obstacles until clouds conceal the visual change.
                ResetStone.Instance?.SetPlatformVisualsVisible(false);
                StartCoroutine(WaitAndStartSpace());
                return;
            }

            if (chapter == chapter.spring || chapter == chapter.summer || chapter == chapter.autumn || chapter == chapter.winter)
            {
                AnimationManager.Instance.Play1();
                StartCoroutine(WaitAndChange());
            }
            else
            {
                SetObstacle();
                onChapterChage?.Invoke(this, EventArgs.Empty);
            }

            if (chapter != chapter.space)
            {
                SaveSystem.Instance?.SaveGame();
            }
        }
    }

    IEnumerator WaitAndStartSpace()
    {
        SpaceEndingAudioController audio = SpaceEndingAudioController.Instance;
        audio.BeginWinterTransition();
        yield return AnimationManager.Instance.PlayWinterToSpaceTransition(() =>
        {
            SetObstacle();
            onChapterChage?.Invoke(this, EventArgs.Empty);
            removeYumju?.Invoke(this, EventArgs.Empty);
            ResetStone.Instance?.HideStartingPlatformVisualsForSpace();
            ResetStone.Instance?.SetPlatformVisualsVisible(true);
        }, audio.SetWinterTransitionProgress);

        // Clouds are gone: reveal the already-present hand, then hold space silently.
        audio.BeginSilentIntro();
        yield return audio.PlaySpaceIntro();
        CameraController.Instance.RaiseCameraY();
    }
    
    IEnumerator WaitAndChange()
    {
        yield return new WaitForSeconds(waitTimeBeforeChange);
        SetObstacle();
        onChapterChage?.Invoke(this, EventArgs.Empty);

        if (chapter == chapter.autumn)
        {
            CloudSystem.Instance.SetSavePoint(CloudCheckPoint[0]);
        }
        if (chapter == chapter.winter)
        {
            //가을->겨울 다시 돌 기반으로 복귀
            StoneFixer.Instance.SetY(CloudCheckPoint[CloudCheckPoint.Length - 1].transform.position.y);  //젤 높은 구름 체크포인트 위치
            ResetStone.Instance.CreatePlatform();
        }
    }
    void RemoveAllCheckpoints()
    {
        foreach (var cp in GameObject.FindGameObjectsWithTag("Checkpoint"))
            Destroy(cp);
    }
    public void AddCount() { stoneCount += 1; onSetteled?.Invoke(this, EventArgs.Empty); }
    public void RemoveCount() { stoneCount -= 1; onDestroyed?.Invoke(this, EventArgs.Empty); }

    public List<GameObject> GetObstaclesByChapter(chapter c)
    {
        var entry = seasonalObstacles.Find(x => x.season == c);
        return entry != null ? entry.obstacles : new List<GameObject>();
    }

    public void LoadChapter(chapter ch)
    {
        //저장된게 우주챕터면 바로 엔딩으로 이동
        RiseStoneAndSceneChange rsasc = FindObjectOfType<RiseStoneAndSceneChange>(true);
        string nextScene = rsasc.nextScene;
        if (ch == chapter.space)
        {
            chapter = ch;
            SpaceEndingAudioController.Instance.PlayFinalReplay();
            SceneManager.LoadScene(nextScene);
            return;
        }

        chapter = ch;
        stoneCount = 0;

        ConfigureCurrentStage();
        onChapterChage?.Invoke(this, System.EventArgs.Empty);
    }

    void ConfigureCurrentStage()
    {
        UpdateCurrentStageUI();

        StoneFixer fixer = StoneFixer.Instance;
        if (!fixer) return;

        int idx = (int)chapter;
        bool isCloudStage = chapter >= chapter.autumn && chapter <= chapter.autumn11;
        bool isStoneStage = chapter < chapter.autumn ||
                            (chapter >= chapter.winter && chapter < chapter.space);

        if (chapter == chapter.space || isCloudStage)
        {
            fixer.HideProgressUI();
            return;
        }

        if (isStoneStage && idx < stonesForChapter.Length)
            fixer.ConfigureStage(stonesForChapter[idx], spawnSavePointWhenZero: true);
    }

    void UpdateCurrentStageUI()
    {
        EnsureCurrentStageUI();
        if (!currentStageTMP) return;

        if (chapter == chapter.space)
        {
            currentStageTMP.gameObject.SetActive(false);
            return;
        }

        currentStageTMP.gameObject.SetActive(true);
        currentStageTMP.text = GetCurrentStageLabel(chapter);
        currentStageTMP.color = GetCurrentStageColor(chapter);
    }

    void EnsureCurrentStageUI()
    {
        if (currentStageTMP) return;

        TextMeshProUGUI template = StoneFixer.Instance ? StoneFixer.Instance.remainingTMP : null;
        Canvas targetCanvas = template
            ? template.canvas
            : FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (!targetCanvas) return;

        GameObject textObject = new(
            "CurrentStageText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = targetCanvas.gameObject.layer;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(targetCanvas.transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = currentStageOffset;
        rect.sizeDelta = currentStageSize;

        currentStageTMP = textObject.GetComponent<TextMeshProUGUI>();
        if (template && template.font)
        {
            currentStageTMP.font = template.font;
            currentStageTMP.fontSharedMaterial = template.fontSharedMaterial;
        }

        currentStageTMP.fontSize = currentStageFontSize;
        currentStageTMP.alignment = TextAlignmentOptions.TopLeft;
        currentStageTMP.textWrappingMode = TextWrappingModes.NoWrap;
        currentStageTMP.overflowMode = TextOverflowModes.Overflow;
        currentStageTMP.raycastTarget = false;
        currentStageTMP.transform.SetAsLastSibling();
    }

    string GetCurrentStageLabel(chapter value)
    {
        if (value >= chapter.spring && value <= chapter.spring4)
            return $"봄 - {(int)value - (int)chapter.spring + 1}";

        if (value >= chapter.summer && value <= chapter.summer5)
            return $"여름 - {(int)value - (int)chapter.summer + 1}";

        if (value >= chapter.autumn && value <= chapter.autumn11)
            return $"가을 - {(int)value - (int)chapter.autumn + 1}";

        if (value >= chapter.winter && value <= chapter.winter4)
            return $"겨울 - {(int)value - (int)chapter.winter + 1}";

        return string.Empty;
    }

    Color GetCurrentStageColor(chapter value)
    {
        if (value <= chapter.spring4) return springStageColor;
        if (value <= chapter.summer5) return summerStageColor;
        if (value <= chapter.autumn11) return autumnStageColor;
        return winterStageColor;
    }

    public string idleScript = "";
    public bool autumnCloudCatch = false;
    public float autumnCloudTime = 10f;
    void SetObstacle()
    {
        //현재 챕터의 요소 설정
        List<GameObject> obstacles = new List<GameObject>();
        obstacles = GetObstaclesByChapter(ChapterManager.Instance.chapter);

        /* LAND
        if (chapter == chapter.land)
        {
            BosalManager.Instance.Speak("TempleStart");
            SoundManager.Instance.PlayBGM("Ground", 0);
            BosalManager.Instance.NoIdle = false;
            return;
        }
        */

        /* SPRING 
        1 : 시작 (비트만 루프)
        2 : 멜로디 전체 재생, 1로 돌아가면 마디가 끝나는 대로 멜로디를 중지시키고 루프
        0 : Ambience만 재생
        메인화면 2 -> 게임 시작 후 1 -> 2 -> 1
        마지막 체크포인트 등장시 0으로 전환
        */
        if (chapter == chapter.spring)
        {
            GuideManager.Instance.PlayGuide("control", 1f);
            GuideManager.Instance.PlayGuide("yumju", 1f);
            SoundManager.Instance.PlayBGM("Spring", 1);
            BosalManager.Instance.Speak("FindTree");
            BosalManager.Instance.Speak("FindTree");
            idleScript = "Spring";
        }
        else if (chapter == chapter.spring2)
        {
            SoundManager.Instance.PlayBGM("Spring", 2);
            BosalManager.Instance.Speak("Spring");
        }
        else if (chapter == chapter.spring3)
        {
            SoundManager.Instance.PlayBGM("Spring", 1);
            BosalManager.Instance.Speak("Spring");
        }
        else if (chapter == chapter.spring4)
        {
            SoundManager.Instance.PlayBGM("Spring", 2);
            BosalManager.Instance.Speak("Spring");
        }

        /* SUMMER
        1 : 시작 (비트만 루프)
        2 : 멜로디 전체 재생, 1로 돌아가면 멜로디 중지
        3 : 1번과 동일, 비가 올 때만 재생
        4 : 2번과 동일, 비가 올 때만 재생
        0 : Ambience만 재생
        1 -> 2로 재생, 비 이벤트 시 1이라면 3, 2라면 4로 전환
        마지막 체크포인트 등장시 0으로 전환
        */
        else if (chapter == chapter.summer)
        {
            SoundManager.Instance.PlayBGM("Summer", 1);
            BosalManager.Instance.Speak("Summer");
            idleScript = "Summer";
        }
        else if (chapter == chapter.summer2)
        {
            SoundManager.Instance.PlayBGM("Summer", 2);
        }
        else if (chapter == chapter.summer3)
        {
            SoundManager.Instance.PlayBGM("Summer", 2);
            BosalManager.Instance.Speak("Summer");
        }
        else if (chapter == chapter.summer4)
        {
            SoundManager.Instance.PlayBGM("Summer", 2);
        }
        else if (chapter == chapter.summer5)
        {
            SoundManager.Instance.PlayBGM("Summer", 2);
            BosalManager.Instance.Speak("Summer");
        }

        /* AUTUMN
        0 : 시작 (멜로디만)
        1 : Pad 재생
        2 : Pad 조 바꿔서 재생
        1이나 2에서 0으로 전환 : Ambience만 재생
        0 -> 1 <-> 2 재생
        마지막 체크포인트 등장시 0으로 전환
        */
        else if (chapter == chapter.autumn)
        {
            float currentTime = Time.deltaTime;
            IEnumerator cloudCheck()
            {
                if (autumnCloudCatch)
                {
                    GuideManager.Instance.PlayGuide("autumn", 2f);
                    yield break;
                }
                if (currentTime - Time.deltaTime > autumnCloudTime)
                {
                    GuideManager.Instance.PlayGuide("autumn", 2f);
                    yield break;
                }
                yield return null;
            }
            StartCoroutine(cloudCheck());
            GuideManager.Instance.PlayGuide("autumn", 2f);
            BosalManager.Instance.Speak("Autumn");
            SoundManager.Instance.PlayBGM("Autumn", 0);
            idleScript = "Autumn";
        }
        else if (chapter == chapter.autumn2)
        {
            SoundManager.Instance.PlayBGM("Autumn", 1);
        }
        else if (chapter == chapter.autumn3)
        {
            SoundManager.Instance.PlayBGM("Autumn", 2);
        }
        else if (chapter == chapter.autumn4)
        {
            SoundManager.Instance.PlayBGM("Autumn", 1);
            BosalManager.Instance.Speak("Autumn");
        }
        else if (chapter == chapter.autumn5)
        {
            SoundManager.Instance.PlayBGM("Autumn", 2);
        }
        else if (chapter == chapter.autumn6)
        {
            SoundManager.Instance.PlayBGM("Autumn", 1);
        }
        else if (chapter == chapter.autumn7)
        {
            SoundManager.Instance.PlayBGM("Autumn", 2);
            BosalManager.Instance.Speak("Autumn");
        }
        else if (chapter == chapter.autumn8)
        {
            SoundManager.Instance.PlayBGM("Autumn", 1);
        }
        else if (chapter == chapter.autumn9)
        {
            SoundManager.Instance.PlayBGM("Autumn", 2);
        }
        else if (chapter == chapter.autumn10)
        {
            SoundManager.Instance.PlayBGM("Autumn", 1);
            BosalManager.Instance.Speak("Autumn");
        }
        else if (chapter == chapter.autumn11)
        {
            SoundManager.Instance.PlayBGM("Autumn", 2);
        }
        /* WINTER 
        1 : 시작 (비트 1회 재생 후 멜로디 A 루프)
        2 : 멜로디 A, B 루프
        3 : 비트만 루프
        0 : 노래 종료 후 Ambience만 재생
        1 -> 2 -> 3 재생
        마지막 체크포인트 등장시 0으로 전환
        */
        else if (chapter == chapter.winter)
        {
            SoundManager.Instance.PlayBGM("Winter", 1);
            BosalManager.Instance.Speak("BeforeEnterWinter");
            BosalManager.Instance.Speak("BeforeEnterWinter");
            idleScript = "Winter";
        }
        else if (chapter == chapter.winter2)
        {
            SoundManager.Instance.PlayBGM("Winter", 2);
        }
        else if (chapter == chapter.winter3)
        {
            SoundManager.Instance.PlayBGM("Winter", 3);
        }
        else if (chapter == chapter.winter4)
        {
            SoundManager.Instance.PlayBGM("Winter", 3);
        }

        /* SPACE
        0 : 시작 (재생)
        1 ~ 4 : 돌 1 ~ 4개 완성 시 재생, 악기 쌓기
        5 : 돌 모두 완성 시 재생, 하이라이트로 전환s
         */
        else if (chapter == chapter.space)
        {
            //Debug.Log("우주브금 실행");
            idleScript = null;
        }

        else
        {
            Debug.LogWarning("Unhandled chapter: " + chapter);
        }

        //현재 챕터에 없는 이전 챕터 요소 비활성화
        foreach (GameObject go in currentObstacles)
        {
            if (!obstacles.Contains(go)) go.SetActive(false);
        }

        currentObstacles = obstacles;
        //현재 챕터 요소들 각각 활성화
        foreach (GameObject go in obstacles)
        {
            if (go) go.SetActive(true);
        }
    }
}
