using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class CountBasedObstacle : MonoBehaviour
{
    protected Dictionary<chapter, float> seasonChances = new Dictionary<chapter, float>();
    [Range(0, 1)] public float[] Chance = new float[(int)chapter.space];
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
        foreach (chapter c in Enum.GetValues(typeof(chapter)))
        {
            seasonChances[c] = Chance[(int)c];
        }
    }

    protected virtual void OnDisable()
    {
        ChapterManager.Instance.onSetteled -= AddStone;
    }

}
