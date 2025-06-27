using UnityEngine;


[RequireComponent(typeof(Collider2D))]
public class StoneDespawnZone : MonoBehaviour
{
    public PenaltyManager penaltyManager;
    void OnTriggerEnter2D(Collider2D other)
    {
        var stone = other.GetComponent<StoneController>();
        if (!stone || stone.DespawnerCheck) return;
        bool counted = stone.state is StoneState.Settled or StoneState.Dropping;

        // 파괴 전에 Fixer에 보고해서 카운터 조정, PenaltyManager에서 번뇌돌 발생
        if (counted)
        {
            StoneFixer.Instance?.NotifyStoneLost(stone);
        }

        if (stone.state == StoneState.Dropping && stone.stoneTypeIndex != 99) // 번뇌돌이 아닌 경우
        {
            stone.DespawnerCheck = true;
            penaltyManager.PenaltyCount();
        }

        //stone이 penaltystone이라면 PenaltyManager에서 PenaltyTrash 호출
        if (stone.state == StoneState.Dropping && stone.stoneTypeIndex == 99)
        {
            stone.DespawnerCheck = true;
            penaltyManager.PenaltyTrash();
        }
        Destroy(other.gameObject);
    }
}