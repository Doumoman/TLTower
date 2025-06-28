using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class StoneInfo
{
    public int typeId;
    public float x, y;
    public float rot;
}

[System.Serializable]
public class StoneSaveData
{
    public int wave;                 // 저장 당시 웨이브
    public List<StoneInfo> stones = new();
}