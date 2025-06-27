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

    //돌 개수 늘어날 때 마다 챕터전환 확인
    void ChangeChapter()
    {
        chapter[] arr = { chapter.land, chapter.spring, chapter.summer,
            chapter.autumn, chapter.winter, chapter.space };
        if (idx < arr.Count()-1 && stoneCount >= stonesForChapter[idx])
        {
            chapter = arr[++idx];
            SetObstacle();
            onChapterChage?.Invoke(this, EventArgs.Empty);
            Debug.Log(chapter);
        }
    }
    public void AddCount() { stoneCount += 1; onSetteled?.Invoke(this, EventArgs.Empty); ChangeChapter(); }
    public void RemoveCount() { stoneCount -= 1; }


    void SetObstacle()
    {
        //현재 챕터의 요소 설정
        List<GameObject> obstacles = new List<GameObject>();
        switch (idx)
        {
            case 0: //land
                return;
            case 1: //spring
                obstacles = springObstacles;
                return;
            case 2: //summer
                obstacles = summerObstacles;
                break;
            case 3: //autumn
                obstacles = autumnObstacles;
                break;
            case 4: //winter
                obstacles = winterObstacles;
                break;
            case 5: //space
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
