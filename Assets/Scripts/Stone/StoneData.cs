using UnityEngine;

[CreateAssetMenu(fileName = "StoneData", menuName = "Stone/New StoneData")]
public class StoneData : ScriptableObject
{
    [Header("고유 ID (중복 금지)")]
    public int typeId;

    [Header("기본 정보")]
    public string stoneName;
    public Sprite[] sprites;

    public Sprite GetRandomSprite() =>
        sprites != null && sprites.Length > 0
            ? sprites[Random.Range(0, sprites.Length)]
            : null;

    public Sprite GetSprite(int idx) =>
        sprites != null && idx >= 0 && idx < sprites.Length
            ? sprites[idx] : null;

    [Header("Prefab")]
    public GameObject backgroundPrefab;   // Stub 용
    public GameObject playablePrefab;     // 실제 돌

    [Header("물리 속성")]
    public float mass = 1f;
    public float angularDrag = 0.05f; // 회전마찰력 계수 클수록 회전이 잘 안됨
    public float gravityScale = 1f;
    public Vector2 scale;

    [Tooltip("PhysicsMaterial2D를 연결하면 마찰·반발계수를 자동으로 적용")]
    public PhysicsMaterial2D material2D;

    [Header("스폰 확률 (전체 합 = 1이면 직관적, 아니어도 상관없음)")]
    [Range(0f, 1f)] public float spawnChance = 0.2f;

    public static implicit operator Sprite(StoneData v)
    {
        throw new System.NotImplementedException();
    }
}