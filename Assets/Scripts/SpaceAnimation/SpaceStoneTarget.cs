using UnityEngine;

/// ‘맞춤 위치’ 오브젝트: 돌이 근처로 오면 자동으로 끼워 맞춰줍니다.
public class SpaceStoneTarget : MonoBehaviour
{
    [Tooltip("맞춰야 할 돌의 presetIndex (0~4)")]
    public int expectedIndex = 0;

    [Tooltip("스냅 허용 거리")]
    public float snapRange = 0.4f;

    [Tooltip("위치·회전 기준이 될 Transform(비워두면 자기 자신)")]
    public Transform snapPoint;

    [Tooltip("스냅 후 적용할 Z축 회전값(°)")]
    public float snapRotationZ;

    void Reset()
    {
        snapPoint = transform;
    }
}