using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FreezerSettler : MonoBehaviour
{
    public static FreezerSettler Instance { get; private set; }
    public bool freezeOnStart = false;
    private List<StoneData> sdl = new List<StoneData>();

    [Header("Settings")]
    [Tooltip("stoneSpawner의 stoneData개수 및 순서와 맞출 것")] public float[] timeSetting = { 0.3f, 0.8f, 0.5f, 0.2f };
    public float unfreezeTime = 3f;
    public float timerResetTime = 0.1f;
    public Dictionary<StoneData, float> freezeTime = new Dictionary<StoneData, float>();
    public Color color = Color.cyan;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        sdl = StoneSpawner.Instance.stoneDataList;

        //stoneSpawner에 있는 stoneData가 key값, timeSetting이 value인 딕셔너리 생성
        int i = 0;
        foreach (StoneData data in sdl)
        {
            freezeTime.Add(data, timeSetting[i++]);
            Debug.Log(data.name + $"{timeSetting[i - 1]}");
        }

        //오류 방지
        if (freezeTime.Count != sdl.Count)
        {
            Debug.Log("stoneData 수와 freezeTime수가 맞지 않음!");
            Destroy(this);
        }
    }

    //sprite가 stoneData중 하나인 data안에 있으면, data에 맞는 timeSetting 리턴(+해동 시간, 리셋타임도)
    public float[] GetTime(Sprite spr)
    {
        float value = 0;
        foreach (StoneData data in sdl)
        {
            Sprite[] sprites = data.sprites;
            if (sprites.Contains<Sprite>(spr))
            {
                value = freezeTime[data];
                break;
            }
        }
        float[] arr = { value, unfreezeTime, timerResetTime };
        return arr;
    }
}
