using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSystem : MonoBehaviour
{
    private List<GameObject> jointedList = new List<GameObject>();
    public static CloudSystem Instance;

    private void Awake()
    {
        if (Instance != null) Destroy(this);
        Instance = this;
    }

    public void NotifyJoint(GameObject go)
    {
        jointedList.Add(go);
    }

    public void NotifyBreak(Rigidbody2D rb)
    {

    }

}
