using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class CountBasedObstacle : MonoBehaviour
{
    protected int stoneCount = 0;
    public int count = 5;

    protected void AddStone(object sender, EventArgs eventArgs)
    {
        stoneCount++;
        if (stoneCount >= count)
        {
            stoneCount = 0;
            RandomlyMake();
        }
    }

    public abstract void RandomlyMake();
    public abstract void MakeObstacle(bool autoStop = true);

    protected virtual void OnEnable()
    {
        ChapterManager.Instance.onSetteled += AddStone;
    }

    protected virtual void OnDisable()
    {
        ChapterManager.Instance.onSetteled -= AddStone;
    }

}
