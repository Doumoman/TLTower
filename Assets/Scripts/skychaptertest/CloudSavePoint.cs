using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSavePoint : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.TryGetComponent<JointMaker>(out JointMaker jm))
        {
            CloudSystem.Instance.SetSavePoint(gameObject);
            this.GetComponent<Collider2D>().isTrigger = false;
            Destroy(this);
        }
    }
}
