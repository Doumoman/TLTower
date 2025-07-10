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

public enum chapter {land, spring, spring2, spring3,  summer, summer2, summer3, summer4, autumn, autumn2, autumn3, autumn4, winter, space};
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
        chapter[] arr = { chapter.land, chapter.spring, chapter.spring2, chapter.spring3, chapter.summer, chapter.summer2, chapter.summer3, chapter.summer4,
            chapter.autumn, chapter.winter, chapter.space };
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
        chapter[] arr = { chapter.land, chapter.spring, chapter.spring2, chapter.spring3, chapter.summer, chapter.summer2, chapter.summer3, chapter.summer4,
            chapter.autumn, chapter.winter, chapter.space };
        if (idx < arr.Count()-1 && stoneCount >= stonesForChapter[idx])  //현재 챕터에서 넘어가는 기준 충족 & idx증가가 space까지만 되게 하는 조건
        {
            chapter = arr[++idx];
            SetObstacle();
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
        switch (chapter)
        {
            case chapter.land: //land
                BosalManager.Instance.Speak("TempleStart");
                BosalManager.Instance.NoIdle = false; //true;
                ChapterSoundManager.Instance.SetStage(SoundStateData.Stage.Ground);
                return;
            case chapter.spring: //spring
                BosalManager.Instance.Speak("SpringFirst");
                BosalManager.Instance.NoIdle = false;
                Debug.Log("NextState 호출중");
                ChapterSoundManager.Instance.NextState("Spring");
                break;
            case chapter.spring2:
                ChapterSoundManager.Instance.NextState("Spring");
                break;
            case chapter.spring3:
                ChapterSoundManager.Instance.NextState("Spring");
                break;
            case chapter.summer: //summer
                BosalManager.Instance.Speak("SummerFirst");
                ChapterSoundManager.Instance.NextState("Summer");
                break;
            case chapter.summer2:
                ChapterSoundManager.Instance.NextState("Summer");
                break;
            case chapter.summer3:
                ChapterSoundManager.Instance.NextState("Summer");
                break;
            case chapter.summer4:
                ChapterSoundManager.Instance.NextState("Summer");
                break;
            case chapter.autumn: //autumn
                BosalManager.Instance.Speak("AutumnFirst");
                ChapterSoundManager.Instance.NextState("Summer");
                break;
            case chapter.winter: //winter
                break;
            case chapter.space: //space
                BosalManager.Instance.NoIdle = true;
                break;
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
