using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickManager : Singleton<TickManager>
{
    public float Tick = 4f; // 이벤트 발생시킬 시간 설정
    public event EventHandler OnTickEvent;
    public int tickCount = 0;

    protected override void Awake()
    {
        base.Awake();
        StartTick();
    }

    public void StartTick()
    {
        StartCoroutine(EventEveryTick(Tick));
    }

    private IEnumerator EventEveryTick(float sec)
    {
        float remainingTime = sec;
        float realTime = Time.realtimeSinceStartup;

        while (true)
        {
            if (Time.timeScale == 0f)
            {
                yield return null;
                continue; // 여기서 아래 로직 안 타게 함
            }

            float currentRealTime = Time.realtimeSinceStartup;
            float delta = currentRealTime - realTime;
            remainingTime -= delta;
            realTime = currentRealTime;

            if (remainingTime <= 0f)
            {
                tickCount++;
                Debug.Log("틱! 현재 틱 카운트: " + tickCount);
                OnTickEvent?.Invoke(this, EventArgs.Empty);

                remainingTime = sec;
                realTime = Time.realtimeSinceStartup;
            }

            yield return null;
        }
    }

    public void StopTick()
    {
        StopAllCoroutines();
    }

    public IEnumerator TickWait(int ticks)
    {
        int startTick = tickCount;
        yield return new WaitUntil
        (
            () =>
            tickCount >= startTick + ticks
        );
    }
}
