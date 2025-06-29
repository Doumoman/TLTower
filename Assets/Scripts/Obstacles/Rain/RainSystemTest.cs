using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

public class RainSystemTest : MonoBehaviour
{
    private List<StoneData> stoneDatas = new List<StoneData>();
    int stoneCount = 0;
    Dictionary<chapter, float> seasonChances = new Dictionary<chapter, float>();
    Coroutine co;

    [Range(0, 1)] public float summerChance = 0.1f;
    [Range(0, 1)] public float autumnChance = 0.1f;
    public float span = 30;
    public int count = 5;

    [Header("References")]
    public ParticleSystem ps;
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D rainy;

    private void Awake()
    {
        seasonChances = new Dictionary<chapter, float>   //챕터별 확률 설정
        {
            {chapter.summer, summerChance}, {chapter.autumn, autumnChance}
        };
    }

    public void MakeRain(bool autoStop = true)
    {
        stoneDatas = StoneSpawner.Instance.stoneDataList;
        foreach (var item in stoneDatas)
        {
            if (item.material2D != normal) continue;
            item.material2D = rainy;
        }

        GameObject[] stones = GameObject.FindGameObjectsWithTag("PlacedStone");
        foreach (var stone in stones)
        {
            PolygonCollider2D[] colliders = stone.GetComponents<PolygonCollider2D>();
            foreach (var col in colliders)
            {
                if (col.sharedMaterial != normal) continue;
                col.sharedMaterial = rainy;
            }
        }

        ps.Play();

        if (!autoStop) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(StopDelay());
    }

    private void AddStone(object sender, EventArgs eventArgs)
    {
        stoneCount++;
        if (stoneCount >= count)
        {
            stoneCount = 0;
            if (UnityEngine.Random.value > seasonChances[ChapterManager.Instance.chapter]) return; //확률 벗어나면 생성 안함
            MakeRain();
        }
    }

    IEnumerator StopDelay()   //일정 시간 후 끄기
    {
        yield return new WaitForSeconds(span);
        StopRain();
        co = null;
    }
    public void StopRain()
    {
        stoneDatas = StoneSpawner.Instance.stoneDataList;
        foreach (var item in stoneDatas)
        {
            if (item.material2D != rainy) continue;
            item.material2D = normal;
        }

        GameObject[] stones = GameObject.FindGameObjectsWithTag("PlacedStone");
        foreach (var stone in stones)
        {
            PolygonCollider2D[] colliders = stone.GetComponents<PolygonCollider2D>();
            foreach (var col in colliders)
            {
                if (col.sharedMaterial != rainy) continue;
                col.sharedMaterial = normal;
            }
        }

        ps.Stop();
    }
    private void OnEnable()
    {
        ChapterManager.Instance.onSetteled += AddStone;
    }
    private void OnDisable()
    { 
        StopRain();
        ChapterManager.Instance.onSetteled -= AddStone;
    }
}