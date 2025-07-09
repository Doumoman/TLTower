using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindSystem : CountBasedObstacle
{
    private const float Z_SIZE = 8.5f;
    private const float X_POSITION = 15;

    [Header("Settings")]
    public float followSpeed = 1f;
    public float duration = 30;

    [Header("References")]
    public ParticleSystem wind;
    public ParticleSystem windcol;
    public float force = 100f;
    public float width = 6f;
    public bool left = false;

    Coroutine co;


    public override void MakeObstacle(bool autoStop = true)
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

    public override void RandomlyMake()
    {
        if (UnityEngine.Random.value > seasonChances[ChapterManager.Instance.chapter]) return; //확률 벗어나면 생성 안함
        MakeObstacle();
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
        MakeObstacle(autoStop);
    }

    IEnumerator StopDelay()   //일정 시간 후 끄기
    {
        yield return new WaitForSeconds(duration);
        StopWind();
        co = null;
    }
    public void StopWind() { wind.Stop(); }

    protected override void OnEnable()
    {
        base.OnEnable();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        StopWind();
    }

}
