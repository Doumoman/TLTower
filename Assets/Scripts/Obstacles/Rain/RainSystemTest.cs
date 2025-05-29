using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RainSystemTest : MonoBehaviour
{
    private ParticleSystem ps;
    private List<StoneData> stoneDatas = new List<StoneData>();

    [Header("References")]
    public GameObject rainParticle;
    public PhysicsMaterial2D normal;
    public PhysicsMaterial2D rainy;

    private void Awake()
    {
        if (rainParticle != null)
        {
            ps = rainParticle.GetComponent<ParticleSystem>();
        }
        else
        {
            Debug.Log("파티클 오브젝트를 찾을 수 없습니다.");
        }
    }
    private void OnEnable()
    {
        stoneDatas = StoneSpawner.Instance.stoneDataList;
        foreach (var item in stoneDatas)
        {
            item.material2D = rainy;
        }

        GameObject[] stones = GameObject.FindGameObjectsWithTag("PlacedStone");
        foreach (var stone in stones)
        {
            PolygonCollider2D[] colliders = stone.GetComponents<PolygonCollider2D>();
            foreach (var col in colliders)
            {
                if (col.sharedMaterial == normal) col.sharedMaterial = rainy;
            }
        }

        ps.Play();
    }
    private void OnDisable()
    {
        stoneDatas = StoneSpawner.Instance.stoneDataList;
        foreach (var item in stoneDatas)
        {
            item.material2D = normal;
        }

        GameObject[] stones = GameObject.FindGameObjectsWithTag("PlacedStone");
        foreach (var stone in stones)
        {
            PolygonCollider2D[] colliders = stone.GetComponents<PolygonCollider2D>();
            foreach (var col in colliders)
            {
                if (col.sharedMaterial == rainy) col.sharedMaterial = normal;
            }
        }

        ps.Stop();
    }
}