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

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        ChapterManager.Instance.onChapterChage += ChangeBackGround;
    }

    void ChangeBackGround(object sender, EventArgs eventArgs)
    {
        switch (ChapterManager.Instance.chapter)
        {
            case chapter.spring:
                sr.sprite = spring;
                break;
            case chapter.summer:
                sr.sprite = summer;
                break;
            case chapter.autumn:
                sr.sprite = autumn;
                break;
            case chapter.winter:
                sr.sprite = winter;
                break;
            case chapter.space:
                sr.sprite = space;
                break;
        }

    }
}
