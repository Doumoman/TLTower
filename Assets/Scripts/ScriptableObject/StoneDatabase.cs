using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stone/StoneDatabase")]
public class StoneDatabase : ScriptableObject
{
    public List<StoneData> stones = new();
    public StoneData RandomStone() => stones[Random.Range(0, stones.Count)];
    public StoneData Find(string name) => stones.Find(s => s.stoneName == name);
}
