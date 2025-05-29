using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BirdPoopDespawner : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<BirdPoop>(out _))
        {
            Destroy(other.gameObject);
        }
    }
}
