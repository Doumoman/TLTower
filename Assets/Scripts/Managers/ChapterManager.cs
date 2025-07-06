using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum chapter {land, spring, summer, autumn, winter, space};
public class ChapterManager : MonoBehaviour
{
    public chapter chapter = chapter.land;

    [Header("References")]
    public List<GameObject> springObstacles;
    public List<GameObject> summerObstacles;
    public List<GameObject> autumnObstacles;
    public List<GameObject> winterObstacles;

    [Header("Settings")]
    public int[] stonesForChapter = {  };
    
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        //시작 챕터 감지
        chapter[] arr = { chapter.land, chapter.spring, chapter.summer,
            chapter.autumn, chapter.winter, chapter.space };
        idx = Array.IndexOf(arr, chapter);
        SetObstacle();
        onChapterChage?.Invoke(this, EventArgs.Empty);
    }

    //돌 개수 늘어날 때 마다 챕터전환 확인
    public void ChangeChapter()
    {
        chapter[] arr = { chapter.land, chapter.spring, chapter.summer,
            chapter.autumn, chapter.winter, chapter.space };
        if (idx < arr.Count()-1 && stoneCount >= stonesForChapter[idx])  //현재 챕터에서 넘어가는 기준 충족 & idx증가가 space까지만 되게 하는 조건
        {
            chapter = arr[++idx];
            SetObstacle();
            onChapterChage?.Invoke(this, EventArgs.Empty);
            stoneCount = 0;
            Debug.Log(chapter);
        }
    }
    public void AddCount() { stoneCount += 1; onSetteled?.Invoke(this, EventArgs.Empty); }
    public void RemoveCount() { stoneCount -= 1; }


    void SetObstacle()
    {
        //현재 챕터의 요소 설정
        List<GameObject> obstacles = new List<GameObject>();
        switch (chapter)
        {
            case chapter.land: //land
                BosalManager.Instance.Speak("TempleStart");
                BosalManager.Instance.NoIdle = false; //true;
                return;
            case chapter.spring: //spring
                obstacles = springObstacles;
                BosalManager.Instance.Speak("FindTree");
                BosalManager.Instance.NoIdle = false;
                break;
            case chapter.summer: //summer
                obstacles = summerObstacles;
                BosalManager.Instance.Speak("EnterSummer");
                break;
            case chapter.autumn: //autumn
                obstacles = autumnObstacles;
                BosalManager.Instance.Speak("EnterAutumn");
                break;
            case chapter.winter: //winter
                obstacles = winterObstacles;
                BosalManager.Instance.Speak("BeforeEnterWinter");
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
