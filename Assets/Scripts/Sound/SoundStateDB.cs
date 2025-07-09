using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundStateDB", menuName = "Sound State Database", order = 1)]
public class SoundStateDB : ScriptableObject
{
    public List<SoundStateData.StageSoundData> stageData = new();

    private void OnValidate()
    {
        int stageCount = (int)SoundStateData.Stage.Count;
        while (stageData.Count < stageCount)
            stageData.Add(new SoundStateData.StageSoundData());
    }
}