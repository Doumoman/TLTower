using UnityEngine;

[CreateAssetMenu(menuName = "Stone/StoneData")]
public class StoneData : ScriptableObject
{
    public string stoneName;
    public Sprite icon;
    [TextArea] public string comment;

    [Header("Physics")]
    public float size = 1f;
    public float mass = 1f;
    public float friction = 1f;

    [Header("Prefab")]
    public GameObject prefab;
}