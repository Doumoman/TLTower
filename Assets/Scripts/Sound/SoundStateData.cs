using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SoundStateData : MonoBehaviour
{
    public enum Stage
    {
        Ground, Spring, Summer, Autumn, Winter, Space
    }

    [System.Serializable]
    public class SoundPlayInfo
    {
        public string clipName;
        public int cycle;
        public int delay;
    }

    [System.Serializable]
    public class SoundStates
    {
        public int stateNumber;
        public List<SoundPlayInfo> playInfos;
    }

    [System.Serializable]
    public class StageSoundData
    {
        public Stage stage;
        public List<SoundStates> states;
    }
    public List<StageSoundData> allStageData;
    private Dictionary<Stage, Dictionary<int, SoundStates>> _runtimeData;

    void Awake()
    {
        _runtimeData = new Dictionary<Stage, Dictionary<int, SoundStates>>();
        foreach (var stageData in allStageData)
        {
            var stateDict = new Dictionary<int, SoundStates>();
            foreach (var state in stageData.states)
                stateDict[state.stateNumber] = state;
            _runtimeData[stageData.stage] = stateDict;
        }
    }

    public SoundStates GetStateData(Stage stage, int stateNumber)
    {
        if (_runtimeData.TryGetValue(stage, out var stateDict))
            if (stateDict.TryGetValue(stateNumber, out var stageData))
                return stageData;
        return null;
    }
}
