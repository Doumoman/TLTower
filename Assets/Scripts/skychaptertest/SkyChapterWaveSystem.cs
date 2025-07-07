using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SkyChapterWaveSystem : MonoBehaviour
{

    public GameObject checkPoint;
    [SerializeField] GameObject sp;
    public int wave { get; private set; } = 0;
    public float[] distances;
    public static SkyChapterWaveSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (sp) CloudSystem.Instance.SetSavePoint(sp);
        else NextWave();
    }

    public void NextWave()
    {
        if (CloudSystem.Instance == null) { Debug.Log("cloudsystem 없음"); return; }
        CloudSystem.Instance.DestroyAll();
        GameObject nextSp = Instantiate(checkPoint);

        float yPos;
        if (sp) yPos = sp.transform.position.y;
        else yPos = CloudSystem.Instance.HighestJointY;
        nextSp.transform.position = new Vector2(0, yPos + distances[wave]);

        wave++;
        sp = nextSp;
        CloudSystem.Instance.SetSavePoint(sp);
    }
}
