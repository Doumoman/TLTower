using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StoneDespawnZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        var stone = other.GetComponent<StoneController>();
        if (!stone) return;

        bool counted = stone.state is StoneState.Settled or StoneState.Dropping;

        // 파괴 전에 Fixer에 보고해서 카운터 조정
        if (counted)
            StoneFixer.Instance?.NotifyStoneLost(stone);

        Destroy(other.gameObject);
    }
}