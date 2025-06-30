using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowFlakeSpawner : CountBasedObstacle
{

    Dictionary<chapter, float> seasonChances = new Dictionary<chapter, float>();

    [Range(0, 1)] public float winterChance = 0.1f;

    public GameObject snowFlake;


    void RandomPoint()
    {
        float highY = StoneFixer.Instance.HighestSettledY;
        transform.position = new Vector2(Random.Range(-8.5f, 8.5f), highY + 10);
    }

    public override void MakeObstacle(bool autoStop = true)
    {
        RandomPoint();
        Instantiate(snowFlake, transform.position, Quaternion.Euler(0, 0, 90));
    }

    public override void RandomlyMake()
    {
        if (UnityEngine.Random.value > seasonChances[ChapterManager.Instance.chapter]) return; //확률 벗어나면 생성 안함
        MakeObstacle();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        seasonChances = new Dictionary<chapter, float> { { chapter.winter, winterChance } };
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }
}
