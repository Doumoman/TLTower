using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindSystem : MonoBehaviour
{
    private const float Z_SIZE = 8.5f;
    private const float X_POSITION = 15;

    [Header("Settings")]
    public float followSpeed = 1f;
    [Range(0, 1)] public float summerChance = 0.1f;
    [Range(0, 1)] public float autumnChance = 0.1f;
    [Range(0, 1)] public float winterChance = 0.1f;
    public float span = 30;
    public int count = 5;

    [Header("References")]
    public ParticleSystem wind;
    public ParticleSystem windcol;
    public float force = 100f;
    public float width = 6f;
    public bool left = false;

    int stoneCount = 0;
    Dictionary<chapter, float> seasonChances = new Dictionary<chapter, float>();
    Coroutine co;

    private void Start()
    {
        seasonChances = new Dictionary<chapter, float>   //챕터별 확률 설정
        {
            {chapter.summer, summerChance}, {chapter.autumn, autumnChance}, {chapter.winter, winterChance}
        };
    }

    private void Update()
    {
        if (Mathf.Abs(wind.transform.position.y - StoneFixer.Instance.HighestSettledY) < 0.07f) return;
        //왼쪽 오르쪽 설정
        if (left)
        {
            Vector2 goal = new Vector2(-X_POSITION, StoneFixer.Instance.HighestSettledY);
            wind.transform.position = Vector2.Lerp(wind.transform.position, goal, Time.deltaTime * followSpeed);
            wind.transform.rotation = Quaternion.Euler(0, 90, 90);
        }
        else
        {
            Vector2 goal = new Vector2(X_POSITION, StoneFixer.Instance.HighestSettledY);
            wind.transform.position = Vector2.Lerp(wind.transform.position, goal, Time.deltaTime * followSpeed);
            wind.transform.rotation = Quaternion.Euler(0, -90, 90);
        }
    }

    public void MakeWind(bool autoStop = true)
    {
        //크기 설정
        ParticleSystem.ShapeModule shape = wind.shape;
        shape.scale = new Vector3(width, 1, Z_SIZE);
        shape = windcol.shape;
        shape.scale = new Vector3(width, 1, Z_SIZE);
        //힘 설정
        ParticleSystem.CollisionModule collision = windcol.collision;
        collision.colliderForce = force;

        wind.Play();

        if (!autoStop) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(StopDelay());
    }

    IEnumerator StopDelay()   //일정 시간 후 끄기
    {
        yield return new WaitForSeconds(span);
        StopWind();
        co = null;
    }
    public void StopWind() { wind.Stop(); }

    private void AddStone(object sender, EventArgs eventArgs)
    {
        stoneCount++;
        if (stoneCount >= count)
        {
            stoneCount = 0;
            if (UnityEngine.Random.value > seasonChances[ChapterManager.Instance.chapter]) return; //확률 벗어나면 생성 안함
            MakeWind();
        }
    }
    private void OnEnable()
    {
        ChapterManager.Instance.onSetteled += AddStone;
    }
    private void OnDisable()
    {
        StopWind();
        ChapterManager.Instance.onSetteled -= AddStone;
    }

}
