using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneCounter : MonoBehaviour
{
    bool issettled = false;
   
    private void Update()
    {
        if (!issettled)
        {
            StoneController sc = GetComponent<StoneController>();
            if (sc.state == StoneState.Settled)
            {
                ChapterManager.Instance.AddCount();
                issettled = true;
            }
        }
    }
    private void OnDestroy()
    {
        if (!issettled) return;
        ChapterManager.Instance.RemoveCount();
    }
}
