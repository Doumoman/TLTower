using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public float Tick = 4f; // 이벤트 발생시킬 시간 설정
    private float timer = 0f;
    public event EventHandler OnTickEvent;

    public static TickManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 중복 방지
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
                Debug.Log($"TickManager: Waited {waitTime} seconds for next tick.");
            }
            else yield return null;
        }
    }

    public void StopTick()
    {
        StopCoroutine(EventEveryTick(Tick));
    }
}
