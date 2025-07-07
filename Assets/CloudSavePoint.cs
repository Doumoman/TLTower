using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSavePoint : MonoBehaviour
{
    SkyChapterWaveSystem waveSystem;
    // Start is called before the first frame update
    void Start()
    {
        waveSystem = SkyChapterWaveSystem.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<JointMaker>(out JointMaker jm)) waveSystem.NextWave(); 
    }
}
