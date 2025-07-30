using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSavePoint : MonoBehaviour
{
    bool istouched = false;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!istouched && collision.TryGetComponent<JointMaker>(out JointMaker jm))
        {
            istouched = true;
            CloudSystem.Instance.SetSavePoint(gameObject);
            this.GetComponent<Collider2D>().isTrigger = false;
            SkyCloudSpawner.Instance.Changedirection();
            ChapterManager.Instance.ChangeChapter();
            Destroy(this);
        }
    }
}
