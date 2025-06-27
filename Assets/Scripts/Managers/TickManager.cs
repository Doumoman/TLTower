using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public float Tick = 4f; // 이벤트 발생시킬 시간 설정
    public event EventHandler OnTickEvent;

    public static TickManager instance;
    public static TickManager Instance;

    private void Awake()
    {
        if (instance == null)
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
                yield return new WaitForSeconds(waitTime);
                Debug.Log("틱!");
            }
            else
            {
                yield return null;
            }
        }
    }

    public void StopTick()
    {
        StopCoroutine(EventEveryTick(Tick));
    }

    //ticks만큼 대기하는 메서드, TickManager.Instance.WaitForTicks(2, () => 다음 동작); 과 같이 호출
    public void WaitForTicks(int ticks, Action onComplete)
{
    int count = 0;

    void Handler()
    {
        count++;
        if (count >= ticks)
        {
            OnTickEvent -= (sender, e) => Handler();
            onComplete?.Invoke();
        }
    }

    EventHandler handler = null;
    handler = (sender, e) =>
    {
        count++;
        if (count >= ticks)
        {
            OnTickEvent -= handler;
            onComplete?.Invoke();
        }
    };
    OnTickEvent += handler;
}
}
