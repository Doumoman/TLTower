using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BirdPoopDespawner : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<BirdPoop>(out BirdPoop _))
        { 
            Destroy(collision.gameObject);
        }
    }
}
