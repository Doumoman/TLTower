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
        remainingTime = Tick;
        StartTick();
    }

    public void StartTick()
    {
        StartCoroutine(EventEveryTick(Tick));
    }
    float remainingTime;
    private IEnumerator EventEveryTick(float sec)
    {

        while (true)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                tickCount++;
                Debug.Log("틱! 현재 틱 카운트: " + tickCount);
                OnTickEvent?.Invoke(this, EventArgs.Empty);

                remainingTime += Tick;
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
