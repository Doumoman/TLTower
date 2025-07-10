using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackGround : MonoBehaviour
{

    public Sprite land;
    public Sprite spring;
    public Sprite summer;
    public Sprite autumn;
    public Sprite winter;
    public Sprite space;

    SpriteRenderer sr;
    Dictionary<chapter, Sprite> spriteForChaper;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ChapterManager.Instance.onChapterChage += ChangeBackGround;

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

    void ChangeBackGround(object sender, EventArgs eventArgs)
    {
        sr.sprite = spriteForChaper[ChapterManager.Instance.chapter];

    }
}
