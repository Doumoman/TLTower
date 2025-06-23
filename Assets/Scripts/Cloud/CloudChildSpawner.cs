/*
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CloudChildSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] CloudChildPrefab;
    public int numberOfChildren = 5;
    public float spawnRadius = 2.5f;
    public float minDistance = 1.1f;

    private List<Rigidbody2D> spawnedBodies = new List<Rigidbody2D>();

    void Start()
    {
        var positions = new List<Vector2>();

        for(int i=0; i<numberOfChildren; i++)
        {
            for(int attempt=0; attempt<100; attempt++)
            {
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

                if(this.IsFarEnough(pos, positions))
                {
                    positions.Add(pos);

                    var prefab = CloudChildPrefab[Random.Range(0, CloudChildPrefab.Length)];
                    var go = Instantiate(prefab, pos, Quaternion.identity, transform);
                    var rb = go.GetComponent<Rigidbody2D>() ?? go.AddComponent<Rigidbody2D>();

                    spawnedBodies.Add(rb);
                    break;
                }
            }
        }
    }

        bool IsFarEnough(Vector2 p, List<Vector2> list)
    {
        foreach (var q in list)
            if (Vector2.Distance(p, q) < this.minDistance) return false;
        return true;
    }
}

*/