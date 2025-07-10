using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

public class RainSystemTest : CountBasedObstacle
{
    private List<StoneData> stoneDatas = new List<StoneData>();
    bool nonStop = false;
    Coroutine co;

    public float duration = 30;

    [Header("References")]
    public ParticleSystem ps;
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D rainy;

    protected override void AddStone(object sender, EventArgs eventArgs)
    {
        if (nonStop || co != null || windOrRain) return; //실행중에는 카운트 안함
        base.AddStone(sender, eventArgs);
    }

    public override void MakeObstacle(bool autoStop = true)
    {
        MakeRain(autoStop);
    }

    public override void RandomlyMake()
    {
        if (UnityEngine.Random.value > seasonChances[ChapterManager.Instance.chapter]) return; //확률 벗어나면 생성 안함
        MakeObstacle();
    }

    public void MakeRain(bool autoStop = true)
    {
        if (windOrRain) return;
        SoundManager.Instance.PlaySfxLoop("Rain_ambient_001");
        BosalManager.Instance.Speak("RainStart");

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
        windOrRain = true;

        if (!autoStop) { nonStop = true; return; }
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(StopDelay());
    }

    IEnumerator StopDelay()   //일정 시간 후 끄기
    {
        yield return new WaitForSeconds(duration);
        StopRain();
        co = null;
    }
    public void StopRain()
    {
        SoundManager.Instance.StopSfxLoop("Rain_ambient_001");

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
        stoneCount = 0;
        nonStop = false;
        windOrRain = false;
    }
    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    { 
        base.OnDisable();
        StopRain();
    }
}