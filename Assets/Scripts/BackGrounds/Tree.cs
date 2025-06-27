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

    float Z;
    Dictionary<chapter, Sprite[]> seasons;
    GameObject lastStem;
    ChapterManager cm;


    void Start()
    {
        Z = gameObject.transform.position.z;
        cm = ChapterManager.Instance;
        cm.onChapterChage += ChageSprite;
        seasons = new Dictionary<chapter, Sprite[]>()
        {
            { chapter.ground, spring },
            { chapter.spring, spring },
            { chapter.summer, summer },
            { chapter.autumn, autumn },
            { chapter.winter, autumn }
        };

        if (!lastStem)
        {
            lastStem = Instantiate(stem[0], gameObject.transform);
            lastStem.transform.position = new Vector3(0, -7, Z);
        }
    }


    void Update()
    {
        float lastY = lastStem.transform.position.y;
        if (StoneFixer.Instance.HighestSettledY > lastY)
        {
            int idx = UnityEngine.Random.Range(0, spring.Length-1);
            lastStem = Instantiate(stem[idx], gameObject.transform);
            lastStem.transform.position = new Vector3(0, lastY + 21.6f, Z);
            lastStem.GetComponent<SpriteRenderer>().sprite = seasons[cm.chapter][idx];
        }
    }

    void ChageSprite(object sender, EventArgs eventArgs)
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        Sprite[] sp = spring;
        Sprite[] preSp= null;

        //챕터에 따라 스프라이트 선택
        switch (cm.chapter)
        {
            case chapter.spring:
                sp = spring;
                break;
            case chapter.summer:
                sp = summer;
                break;
            case chapter.autumn:
                sp = autumn;
                break;
            case chapter.winter:
                break;
        }
        
        if (spring.Contains(spriteRenderers[0].sprite)) preSp = spring;
        else if (summer.Contains(spriteRenderers[0].sprite)) preSp = summer;
        else if (autumn.Contains(spriteRenderers[0].sprite)) preSp = autumn;
        
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (preSp == null) break;
            int index = Array.FindIndex(preSp, x => x == spriteRenderer.sprite);
            spriteRenderer.sprite = sp[index];
        }
    }
}
