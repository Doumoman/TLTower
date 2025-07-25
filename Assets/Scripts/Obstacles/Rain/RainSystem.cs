using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputManagerEntry;

public class RainSystem : CountBasedObstacle
{
    private List<StoneData> stoneDatas = new List<StoneData>();
    Coroutine co;
    bool nonStop = false;

    public float duration = 30;

    [Header("References")]
    public ParticleSystem ps;
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D rainy;

    [Header("Darken")]
    public GameObject darkenBackGround;
    public float speed = 0.1f;
    [Range(0, 1f)] public float opacity = 0.2f;

    protected override void AddStone(object sender, EventArgs eventArgs)
    {
        if (nonStop || co != null || windOrRain) return; //실행중에는 카운트 안함
        base.AddStone(sender, eventArgs);
    }

    public override void MakeObstacle(bool autoStop = true)
    {
        if (windOrRain) return;
        //돌 데이터마다 마찰데이터 변경
        stoneDatas = StoneSpawner.Instance.stoneDataList;
        foreach (var item in stoneDatas)
        {
            if (item.material2D != normal) continue;
            item.material2D = rainy;
        }

        SoundManager.Instance.PlayVoice("Rain");
        GuideManager.Instance.PlayGuide("rain");

        StartCoroutine(BackGroundFadeIn());
        ps.Play();
        windOrRain = true;

        if (!autoStop) { nonStop = true; return; }
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
                if (col.sharedMaterial == rainy) col.sharedMaterial = normal;
            }
        }

        StartCoroutine(BackGroundFadeOut());
        ps.Stop();
        stoneCount = 0;
        nonStop = false;
        windOrRain = false;
    }

    //배경 점점어둡게
    IEnumerator BackGroundFadeIn()
    {
        Image image = darkenBackGround.GetComponent<Image>();
        float a = 0f;

        while (a < opacity)
        {
            a += Time.deltaTime * speed;
            a = Mathf.Min(a, opacity);
            image.color = new Color(0, 0, 0, a);
            yield return null;
        }
    }
    //배경 점점밝게
    IEnumerator BackGroundFadeOut()
    {
        Image image = darkenBackGround.GetComponent<Image>();
        float a = image.color.a;

        while (a > 0)
        {
            a -= Time.deltaTime * speed;
            a = Mathf.Max(a, 0);
            image.color = new Color(0, 0, 0, a);
            yield return null;
        }
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
