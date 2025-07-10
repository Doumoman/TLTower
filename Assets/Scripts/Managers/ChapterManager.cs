using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SeasonObstacle
{
    public chapter season; // or Season enum
    public List<GameObject> obstacles;
}

public enum chapter
{
    land,
    spring, spring2, spring3,
    summer, summer2, summer3, summer4,
    autumn, autumn2, autumn3, autumn4,
    winter, winter2, winter3, winter4, winter5, winter6,
    space
}; //사운드 편의상 분류 수를 늘림
public class ChapterManager : MonoBehaviour
{
    public chapter chapter = chapter.land;

    [Header("References")]
    public List<SeasonObstacle> seasonalObstacles;

    [Header("Settings")]
    [Tooltip("land챕터부터 space전(winter) 챕터 까지")]
    public int[] stonesForChapter = new int[(int)chapter.space];
    
    int stoneCount = 0;
    int idx = 0;
    Dictionary<GameObject, Coroutine> co = new Dictionary<GameObject, Coroutine>();
    List<GameObject> currentObstacles = new List<GameObject>();
    public event EventHandler onChapterChage;
    public event EventHandler onSetteled;

    public static ChapterManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        //시작 챕터 감지
        chapter[] arr = (chapter[])System.Enum.GetValues(typeof(chapter));
        idx = Array.IndexOf(arr, chapter);

        //List<GameObject>[] obList = { springObstacles, summerObstacles, autumnObstacles, winterObstacles };
        //if (idx > 0 && idx < 5) currentObstacles = obList[idx];
        SetObstacle();
        StoneFixer.Instance.threshold = stonesForChapter[idx];
        StoneFixer.Instance.NotifyStoneLost(null);
        onChapterChage?.Invoke(this, EventArgs.Empty);
    }

    //돌 개수 늘어날 때 마다 챕터전환 확인
    public void ChangeChapter()
    {
        chapter[] arr = (chapter[])System.Enum.GetValues(typeof(chapter));
        if (idx < arr.Count()-1 && stoneCount >= stonesForChapter[idx])  //현재 챕터에서 넘어가는 기준 충족 & idx증가가 space까지만 되게 하는 조건
        {
            chapter = arr[++idx];
            //SetObstacle();
            /* space 챕터에는 threshold 가 없으므로 안전 체크 */
            if (idx < stonesForChapter.Length)
                StoneFixer.Instance.threshold = stonesForChapter[idx];
            StoneFixer.Instance.NotifyStoneLost(null);
            onChapterChage?.Invoke(this, EventArgs.Empty);
            stoneCount = 0;
            Debug.Log(chapter);
        }
        if (chapter == chapter.spring || chapter == chapter.summer || chapter == chapter.autumn || chapter == chapter.winter)
        {
            Debug.Log("Play");
            AnimationManager.Instance.Play();
        }
        if (chapter == chapter.space)
        {
            Debug.Log("PlayBck");
            CameraController.Instance.LowerBackgrounds();
        }
    }
    void RemoveAllCheckpoints()
    {
        foreach (var cp in GameObject.FindGameObjectsWithTag("Checkpoint"))
            Destroy(cp);
    }
    public void AddCount() { stoneCount += 1; onSetteled?.Invoke(this, EventArgs.Empty); }
    public void RemoveCount() { stoneCount -= 1; }

    public List<GameObject> GetObstaclesByChapter(chapter c)
    {
        var entry = seasonalObstacles.Find(x => x.season == c);
        return entry != null ? entry.obstacles : new List<GameObject>();
    }



    void SetObstacle()
    {
        //현재 챕터의 요소 설정
        List<GameObject> obstacles = new List<GameObject>();
        obstacles = GetObstaclesByChapter(ChapterManager.Instance.chapter);

        // LAND
        if (chapter == chapter.land)
        {
            BosalManager.Instance.Speak("TempleStart");
            BosalManager.Instance.NoIdle = false;
            ChapterSoundManager.Instance.SetStage(SoundStateData.Stage.Ground);
            return;
        }

        // SPRING
        if (chapter == chapter.spring)
        {
            BosalManager.Instance.Speak("SpringFirst");
            BosalManager.Instance.NoIdle = false;
            Debug.Log("NextState 호출중");
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Spring, 0);
        }
        else if (chapter == chapter.spring2)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Spring, 1);
        }
        else if (chapter == chapter.spring3)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Spring, 2);
        }

        // SUMMER
        else if (chapter == chapter.summer)
        {
            BosalManager.Instance.Speak("SummerFirst");
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Summer, 0);
        }
        else if (chapter == chapter.summer2)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Summer, 1);
        }
        else if (chapter == chapter.summer3)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Summer, 2);
        }
        else if (chapter == chapter.summer4)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Summer, 3);
        }

        // AUTUMN
        else if (chapter == chapter.autumn)
        {
            BosalManager.Instance.Speak("AutumnFirst");
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Autumn, 0);
        }
        else if (chapter == chapter.autumn2)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Autumn, 1);
        }
        else if (chapter == chapter.autumn3)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Autumn, 2);
        }
        else if (chapter == chapter.autumn4)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Autumn, 3);
        }

        // WINTER
        else if (chapter == chapter.winter)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Winter, 0);
        }
        else if (chapter == chapter.winter2)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Winter, 1);
        }
        else if (chapter == chapter.winter3)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Winter, 2);
        }
        else if (chapter == chapter.winter4)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Winter, 3);
        }
        else if (chapter == chapter.winter5)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Winter, 4);
        }
        else if (chapter == chapter.winter6)
        {
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Winter, 5);
        }

        // SPACE
        else if (chapter == chapter.space)
        {
            BosalManager.Instance.NoIdle = true;
            ChapterSoundManager.Instance.Transition(SoundStateData.Stage.Space, 0);
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
            go.SetActive(true);
        }
    }
}
