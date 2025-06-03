using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameStart : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TickManager tickManager = FindObjectOfType<TickManager>();
        if (tickManager != null)
        {
            tickManager.StartTick();
        }
    }
}