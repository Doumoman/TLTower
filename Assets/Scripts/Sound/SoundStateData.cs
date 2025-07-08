using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SoundStateData : MonoBehaviour
{
    public enum Stage
    {
        Ground,
        Spring,
        Summer,
        Autumn,
        Winter,
        Space,
        Count
    }

    [System.Serializable]
    public class ClipData
    {
        public string clipName;
        public int cycle;
        public int delay;
    }

    [System.Serializable]
    public class State
    {
        [Tooltip("동시에 재생할 사운드클립 정보")]
        public List<ClipData> clipDataList = new();
    }

    [System.Serializable]
    public class StageSoundData
    {
        public List<State> States = new();
    }

    [CreateAssetMenu(fileName = "SoundStateDB", menuName = "Sound State Database", order = 1)]
    public class SoundStateDB : ScriptableObject
    {
        public StageSoundData[] stageData = new StageSoundData[(int)Stage.Count];

        private void OnValidate()
        {
            int stageCount = System.Enum.GetValues(typeof(Stage)).Length;
            if (stageData == null || stageData.Length != stageCount)
                stageData = new StageSoundData[stageCount];

            for (int i = 0; i < stageCount; i++)
                if (stageData[i] == null)
                    stageData[i] = new StageSoundData();
        }
    }
}
