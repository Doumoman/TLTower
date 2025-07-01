using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

public class RainSystem : CountBasedObstacle
{
    private List<StoneData> stoneDatas = new List<StoneData>();

    Dictionary<chapter, float> seasonChances = new Dictionary<chapter, float>();
    Coroutine co;

    [Range(0, 1)] public float summerChance = 0.1f;
    [Range(0, 1)] public float autumnChance = 0.1f;
    public float duration = 30;

    [Header("References")]
    public ParticleSystem ps;
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D rainy;

    public override void MakeObstacle(bool autoStop = true)
    {
        //돌 데이터마다 마찰데이터 변경
        stoneDatas = StoneSpawner.Instance.stoneDataList;
        foreach (var item in stoneDatas)
        {
            if (item.material2D != normal) continue;
            item.material2D = rainy;
        }
        ps.Play();

        if (!autoStop) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(StopDelay());
    }

    public override void RandomlyMake()
    {
        if (UnityEngine.Random.value > seasonChances[ChapterManager.Instance.chapter]) return; //확률 벗어나면 생성 안함
        MakeObstacle();
    }

    IEnumerator StopDelay()   //일정 시간 후 끄기
    {
        yield return new WaitForSeconds(duration);
        ps.Stop();
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
                if (col.sharedMaterial == rainy) col.sharedMaterial = normal;
            }
        }
        ps.Stop();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        seasonChances = new Dictionary<chapter, float>   //챕터별 확률 설정
        {
            {chapter.summer, summerChance}, {chapter.autumn, autumnChance}
        };
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        StopRain();
    }
}
