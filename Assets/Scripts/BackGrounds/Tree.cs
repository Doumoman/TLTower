using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;

public class Tree : MonoBehaviour
{
    [Header("References")]
    public GameObject[] stem;
    public Sprite[] spring;
    public Sprite[] springFlowerFront;
    public Sprite[] summer;
    public Sprite[] summerFlowerFront;
    public Sprite[] autumn;
    public Sprite[] winter;
    public Sprite[] winterFlowerFront;

    float Z;
    Dictionary<chapter, Sprite[]> seasons;
    Dictionary<chapter, Sprite[]> seasonFlower;
    Sprite[] currentSp;
    Sprite[] currentFlowerSp;
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
        seasonFlower = new Dictionary<chapter, Sprite[]>()
        {
            { chapter.land, springFlowerFront },
            { chapter.spring, springFlowerFront },
            { chapter.summer, summerFlowerFront },
            { chapter.autumn, springFlowerFront },
            { chapter.winter, winterFlowerFront },
            { chapter.space, winterFlowerFront }
        };
        currentSp = spring;
        currentFlowerSp = springFlowerFront;

        if (!lastStem)
        {
            lastStem = Instantiate(stem[0], gameObject.transform);
            lastStem.transform.position = new Vector3(0, -7, Z);
        }
    }

    //나무를 계속 생성(space에선 생성x)
    void Update()
    {
        if (cm.chapter == chapter.space) return;
        float lastY = lastStem.transform.position.y;
        if (StoneFixer.Instance.HighestSettledY > lastY)
        {
            int idx = UnityEngine.Random.Range(0, spring.Length-1);
            lastStem = Instantiate(stem[idx], gameObject.transform);
            lastStem.transform.position = new Vector3(0, lastY + 21.6f, Z);
            lastStem.GetComponent<SpriteRenderer>().sprite = seasons[cm.chapter][idx];
            SpriteRenderer sr = Array.Find(lastStem.GetComponentsInChildren<SpriteRenderer>(),x => x.sortingLayerName == "flower"); //꽃잎 spriteRenderer
            if (cm.chapter == chapter.autumn) sr.enabled = false;
            else sr.sprite = seasonFlower[cm.chapter][idx];
        }
    }

    //챕터가 바뀌면 챕터에 맞춰 스프라이트 전부 변경
    void ChageSprite(object sender, EventArgs eventArgs)
    {
        List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
        Sprite[] sp1;
        List<SpriteRenderer> suhangmokSp = spriteRenderers.FindAll(x => x.sortingLayerName == "suhangmok");
        List<SpriteRenderer> flowerSp = spriteRenderers.FindAll(x => x.sortingLayerName == "flower");

        //챕터에 따라 스프라이트 선택
        sp1 = seasons[cm.chapter];
        //이전 스프라이트 번호에 맞게 스프라이트 전환
        foreach (SpriteRenderer spriteRenderer in suhangmokSp)
        {
            if (currentSp == sp1) break;
            int index = Array.FindIndex(currentSp, x => x == spriteRenderer.sprite);
            spriteRenderer.sprite = sp1[index];
        }
        currentSp = sp1;

        sp1 = seasonFlower[cm.chapter];
        foreach (SpriteRenderer spriteRenderer in flowerSp)
        {
            //가을 챕터는 꽃잎 안보이기
            if (cm.chapter == chapter.autumn) { spriteRenderer.enabled = false; continue; }
            else spriteRenderer.enabled = true;

            if (currentFlowerSp == sp1) break;
            int index = Array.FindIndex(currentFlowerSp, x => x == spriteRenderer.sprite);
            spriteRenderer.sprite = sp1[index];
        }
        currentFlowerSp = sp1;
    }
}
