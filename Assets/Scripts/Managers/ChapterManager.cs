using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

[System.Serializable]
public class SeasonObstacle
{
    public chapter season; // or Season enum
    public List<GameObject> obstacles;
}

public enum chapter
{
    //land,
    spring, spring2, spring3,
    summer, summer2, summer3, summer4,
    autumn, autumn2, autumn3,
    winter, winter2, winter3,
    space
}; //사운드 편의상 분류 수를 늘림
public class ChapterManager : MonoBehaviour
{
    public chapter chapter = chapter.spring;

    [Header("References")]
    public List<SeasonObstacle> seasonalObstacles;

    [Header("Settings")]
    [Tooltip("land챕터부터 space전(winter) 챕터 까지")]
    public int[] stonesForChapter = new int[(int)chapter.space];
    
    int stoneCount = 0;
    Dictionary<GameObject, Coroutine> co = new Dictionary<GameObject, Coroutine>();
    List<GameObject> currentObstacles = new List<GameObject>();
    public event EventHandler onChapterChage;
    public event EventHandler onSetteled;
    public event EventHandler onDestroyed;
    public event EventHandler removeYumju;

    
    public static ChapterManager Instance;

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
        
        SaveSystem.Instance?.LoadGame();
        //시작 챕터 감지
        chapter[] arr = (chapter[])System.Enum.GetValues(typeof(chapter));
        int idx = Array.IndexOf(arr, chapter);

        //List<GameObject>[] obList = { springObstacles, summerObstacles, autumnObstacles, winterObstacles };
        //if (idx > 0 && idx < 5) currentObstacles = obList[idx];
        SetObstacle();
        if (StoneFixer.Instance)
        {
            StoneFixer.Instance.threshold = stonesForChapter[idx];
            StoneFixer.Instance.NotifyStoneLost(null);
        }
        onChapterChage?.Invoke(this, EventArgs.Empty);
        Debug.Log("chaptermanager 챕터변환 실행");
    }

    //돌 개수 확인 후 챕터전환 확인
    public void ChangeChapter()
    {

        chapter[] arr = (chapter[])System.Enum.GetValues(typeof(chapter));
        int idx = System.Array.IndexOf(arr, chapter);

        if (idx < arr.Count()-1 && stoneCount >= stonesForChapter[idx])  //현재 챕터에서 넘어가는 기준 충족 & idx증가가 space까지만 되게 하는 조건
        {
            chapter = arr[++idx];
            SetObstacle();
            if (StoneFixer.Instance)
            {
                /* space 챕터에는 threshold 가 없으므로 안전 체크 */
                if (idx < stonesForChapter.Length)
                    StoneFixer.Instance.threshold = stonesForChapter[idx];
                StoneFixer.Instance.NotifyStoneLost(null);
            }
            onChapterChage?.Invoke(this, EventArgs.Empty);
            stoneCount = 0;
            Debug.Log(chapter);
        }
        if (chapter == chapter.spring || chapter == chapter.summer || chapter == chapter.autumn || chapter == chapter.winter)
        {
            AnimationManager.Instance.Play();
        }
        if (chapter == chapter.space) //여기서부터 우주애니메이션 시작
        {
            Debug.Log("PlayBck");
            CameraController.Instance.RaiseCameraY();
            removeYumju?.Invoke(this, EventArgs.Empty);
        }
        SaveSystem.Instance?.SaveGame();
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
        chapter = ch;

        // idx 재계산
        chapter[] arr = (chapter[])System.Enum.GetValues(typeof(chapter));
        int idx = System.Array.IndexOf(arr, ch);

        SetObstacle();  // 장애물·사운드 등 새 챕터 세팅
        if (idx < stonesForChapter.Length)
            StoneFixer.Instance.threshold = stonesForChapter[idx];

        StoneFixer.Instance.NotifyStoneLost(null);
        onChapterChage?.Invoke(this, System.EventArgs.Empty);
    }

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

        // SPRING
        if (chapter == chapter.spring)
        {
            GuideManager.Instance.PlayGuide("control", 2f);
            SoundManager.Instance.PlayBGM("Spring", 1);
            BosalManager.Instance.Speak("Spring");
            //BosalManager.Instance.NoIdle = false;
        }
        else if (chapter == chapter.spring2)
        {
            SoundManager.Instance.PlayBGM("Spring", 2);
            BosalManager.Instance.Speak("Spring");
        }
        else if (chapter == chapter.spring3)
        {
            SoundManager.Instance.PlayBGM("Spring", 0);
            BosalManager.Instance.Speak("Spring");

        }

        // SUMMER
        else if (chapter == chapter.summer)
        {
            BosalManager.Instance.Speak("SummerFirst");
            SoundManager.Instance.PlayBGM("Summer", 1);
        }
        else if (chapter == chapter.summer2)
        {
            SoundManager.Instance.PlayBGM("Summer", 2);
        }
        else if (chapter == chapter.summer3)
        {
            SoundManager.Instance.PlayBGM("Summer", 3);
        }
        else if (chapter == chapter.summer4)
        {
            SoundManager.Instance.PlayBGM("Summer", 4);
        }

        // AUTUMN
        else if (chapter == chapter.autumn)
        {
            BosalManager.Instance.Speak("AutumnFirst");
            GuideManager.Instance.PlayGuide("autumn", 4);
            SoundManager.Instance.PlayBGM("Autumn", 1);
        }
        else if (chapter == chapter.autumn2)
        {
            SoundManager.Instance.PlayBGM("Autumn", 2);
        }
        else if (chapter == chapter.autumn3)
        {
            SoundManager.Instance.PlayBGM("Autumn", 0);
        }
        // WINTER
        else if (chapter == chapter.winter)
        {
            SoundManager.Instance.PlayBGM("Winter", 3);
        }
        else if (chapter == chapter.winter2)
        {
            SoundManager.Instance.PlayBGM("Winter", 1);
        }
        else if (chapter == chapter.winter3)
        {
            SoundManager.Instance.PlayBGM("Winter", 2);
        }

        // SPACE
        else if (chapter == chapter.space)
        {
            SoundManager.Instance.PlayBGM("Space", 0);
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
