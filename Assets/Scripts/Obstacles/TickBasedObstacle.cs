using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class TickBasedObstacle : MonoBehaviour
{
    protected int spanCount;
    protected int span;

    [Header("TickSettings")]
    public int cycleTickMin = 3;
    public int cycleTickMax = 6;

    public void OnTick(object sender, System.EventArgs e)
    {
        if (spanCount-- <= 0)
        {
            MakeObstacle();

            span = UnityEngine.Random.Range(cycleTickMin, cycleTickMax + 1);
            spanCount = span;
        }
    }

    public abstract void MakeObstacle(bool autoStop = true);

    protected virtual void OnEnable()
    {
        span = UnityEngine.Random.Range(cycleTickMin, cycleTickMax + 1);
        spanCount = span;
        TickManager.Instance.OnTickEvent += OnTick;
    }
    protected virtual void OnDisable()
    {
        TickManager.Instance.OnTickEvent -= OnTick;
    }
}
