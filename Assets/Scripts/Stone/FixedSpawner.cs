using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class FixedSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TickManager.Instance.OnTickEvent += TickEvent;
    }

    private void TickEvent(object sender, System.EventArgs eventArgs)
    {
        GameObject.Find("StoneSpawner").GetComponent<StoneSpawner>().CreateStubAtSlot(this.transform);
    }
    // Update is called once per frame
    void Update()
    {

    }
}
