using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public float Tick = 4f; // 이벤트 발생시킬 시간 설정
    private float timer = 0f;
    public event EventHandler OnTickEvent;

    public static TickManager instance;
    public static TickManager Instance
    {
        get
        {
            if (instance == null) instance = new TickManager();
            return instance;
        }
    }
    public void StartTick()
    {
        StartCoroutine(EventEveryTick(Tick));
    }

    private IEnumerator EventEveryTick(float sec)
    {
        float nextTime = Time.time;
        while (true)
        {
            float now = Time.time + sec;
            float waitTime = nextTime - now;

            if (waitTime > 0) yield return new WaitForSeconds(waitTime);
            else yield return null;

            OnTickEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public void StopTick()
    {
        StopCoroutine(EventEveryTick(Tick));
    }
}
