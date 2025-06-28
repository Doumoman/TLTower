using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public float Tick = 4f; // 이벤트 발생시킬 시간 설정
    public event EventHandler OnTickEvent;
    public int tickCount = 0;

    public static TickManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartTick()
    {
        StartCoroutine(EventEveryTick(Tick));
    }

    private IEnumerator EventEveryTick(float sec)
    {
        float nextTime = Time.realtimeSinceStartup;
        while (true)
        {
            nextTime += sec;
            OnTickEvent?.Invoke(this, EventArgs.Empty);
            float waitTime = nextTime - Time.realtimeSinceStartup;

            if (waitTime > 0f)
            {
                tickCount++;
                yield return new WaitForSeconds(waitTime);
                Debug.Log("틱! 현재 틱 카운트: " + tickCount);
            }
            else
            {
                yield return null;
            }
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
