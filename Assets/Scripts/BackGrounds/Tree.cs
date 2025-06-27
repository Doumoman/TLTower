using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Tree : MonoBehaviour
{
    [Header("References")]
    public GameObject[] stem;
    public Sprite[] spring;
    public Sprite[] summer;
    public Sprite[] autumn;
    public Sprite[] winter;

    float Z;
    Dictionary<chapter, Sprite[]> seasons;
    Sprite[] currentSp;
    GameObject lastStem;
    ChapterManager cm;


    void Start()
    {
        Z = gameObject.transform.position.z;
        cm = ChapterManager.Instance;
        cm.onChapterChage += ChageSprite;

        seasons = new Dictionary<chapter, Sprite[]>()
        {
            { chapter.land, spring },
            { chapter.spring, spring },
            { chapter.summer, summer },
            { chapter.autumn, autumn },
            { chapter.winter, winter },
            { chapter.space, winter }
        };
        currentSp = spring;

        if (!lastStem)
        {
            lastStem = Instantiate(stem[0], gameObject.transform);
            lastStem.transform.position = new Vector3(0, -7, Z);
        }
    }

    //나무를 계속 생성(space에선 생성x)
    void Update()
    {
        if (ChapterManager.Instance.chapter == chapter.space) return;
        float lastY = lastStem.transform.position.y;
        if (StoneFixer.Instance.HighestSettledY > lastY)
        {
            int idx = UnityEngine.Random.Range(0, spring.Length-1);
            lastStem = Instantiate(stem[idx], gameObject.transform);
            lastStem.transform.position = new Vector3(0, lastY + 21.6f, Z);
            lastStem.GetComponent<SpriteRenderer>().sprite = seasons[cm.chapter][idx];
        }
    }

    //챕터가 바뀌면 챕터에 맞춰 스프라이트 전부 변경
    void ChageSprite(object sender, EventArgs eventArgs)
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        Sprite[] sp = spring;

        //챕터에 따라 스프라이트 선택
        sp = seasons[ChapterManager.Instance.chapter];
        
        //이전 스프라이트 번호에 맞게 스프라이트 전환
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (currentSp == sp) break;
            int index = Array.FindIndex(currentSp, x => x == spriteRenderer.sprite);
            spriteRenderer.sprite = sp[index];
        }
        currentSp = sp;
    }
}
