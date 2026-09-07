using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // for : someChild = transform.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.Contains("Child"));

public class BackGround : MonoBehaviour
{
    BackGround cm;             // 만약 BackGround 라는 컴포넌트를 찾고 싶다면
    
    Transform someChild;       // 자식 오브젝트 Transform


    public Sprite land;
    public Sprite spring;
    public Sprite summer;
    public Sprite autumn;
    public Sprite winter;
    public Sprite space;
    public Sprite space2;

    SpriteRenderer sr;
    Dictionary<chapter, Sprite> spriteForChaper;

    private void Awake()
    {   Debug.Log($"[Background] spriteRenderer = {sr}");
        Debug.Log($"[Background] someChild    = {spriteForChaper}");

        // 1) ChapterManager는 싱글톤으로 접근 (BackGround 자체에 ChapterManager가 없음)
        // cm = GetComponent<BackGround>(); // 이 줄은 제거 - BackGround는 자기 자신을 참조하는 의미가 없음

        // 2) 자식 트랜스폼 자동 할당 (선택적)
        someChild = transform.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.Contains("Child"));
        if (someChild == null)
            Debug.LogWarning("[Background] 'Child' 이름의 자식이 없습니다. (선택적)");

        // 3) 필수 컴포넌트 자동 할당
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            Debug.LogError("[Background] SpriteRenderer 컴포넌트를 찾을 수 없습니다.");

        sr = GetComponent<SpriteRenderer>();
        
        spriteForChaper = new Dictionary<chapter, Sprite>();
        chapter[] c = (chapter[])System.Enum.GetValues(typeof(chapter));

        foreach (chapter ch in c)
        {
            //문자열로 챕터 확인!
            string s = ch.ToString();
            if (s.Contains("land"))
            {
                spriteForChaper.Add(ch, land);
            }
            else if (s.Contains("spring"))
            {
                spriteForChaper.Add(ch, spring);
            }
            else if (s.Contains("summer"))
            {
                spriteForChaper.Add(ch, summer);
            }
            else if (s.Contains("autumn"))
            {
                spriteForChaper.Add(ch, autumn);
            }
            else if (s.Contains("winter"))
            {
                spriteForChaper.Add(ch, winter);
            }
            else if (s.Contains("space"))
            {
                spriteForChaper.Add(ch, space);
            }
        }
    }

    void Start()
    {   if (ChapterManager.Instance != null)
        {
            ChapterManager.Instance.onChapterChage += ChangeBackGround;
            ChangeBackGround(this, EventArgs.Empty);          // ← 추가
        }
        else
        {
            Debug.LogError("background 챕터변환 등록 실패 - ChapterManager가 null입니다.");
        }
        StartCoroutine(RegisterWhenReady());
    }

    IEnumerator RegisterWhenReady()
    {
        yield return new WaitUntil(() => ChapterManager.Instance != null);
        ChapterManager.Instance.onChapterChage += ChangeBackGround;
        Debug.Log("background 챕터변환 등록 완료");
        
        yield return new WaitUntil(() => AnimationManager.Instance != null);
        AnimationManager.Instance.changeSpaceBackGround += ChangeSpaceBackGround;
        Debug.Log("background 스페이스 배경 변경 등록 완료");
    }

    void ChangeBackGround(object sender, EventArgs eventArgs)
    {
        // ChapterManager가 null이면 처리하지 않음
        if (ChapterManager.Instance == null) return;
        
        sr.sprite = spriteForChaper[ChapterManager.Instance.chapter];
    }
    void ChangeSpaceBackGround(object sender, EventArgs eventArgs)
    {
        sr.sprite = space2;
    }

    void OnDestroy()
    {
        // 이벤트 해제
        if (ChapterManager.Instance != null)
        {
            ChapterManager.Instance.onChapterChage -= ChangeBackGround;
        }
        
        if (AnimationManager.Instance != null)
        {
            AnimationManager.Instance.changeSpaceBackGround -= ChangeSpaceBackGround;
        }
    }
}
