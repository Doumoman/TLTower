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
}
