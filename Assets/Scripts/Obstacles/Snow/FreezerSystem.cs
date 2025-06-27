using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezerSystem : MonoBehaviour
{
    int stoneCount = 0;
    Coroutine co;
    FreezerSettler fs;

    [Header("Settings")]
    [Range(0, 1)] public float winterChance = 0.1f;
    public float span = 30;
    public int count = 5;

    public ParticleSystem ps;

    // Start is called before the first frame update
    void Start()
    {
        fs = FreezerSettler.Instance;
    }

    private void AddStone(object sender, EventArgs eventArgs)
    {
        stoneCount++;
        if (stoneCount >= count)
        {
            stoneCount = 0;
            if (UnityEngine.Random.value > winterChance) return; //확률 벗어나면 생성 안함
            MakeSnow();
        }
    }

    void MakeSnow(bool autoStop = true)
    {
        fs.freezeOnStart = true;

        if (!autoStop) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(StopDelay());
    }

    IEnumerator StopDelay()   //일정 시간 후 끄기
    {
        yield return new WaitForSeconds(span);
        StopSnow();
        co = null;
    }

    void StopSnow()
    {
        fs.freezeOnStart = false;
    }

    private void OnEnable() 
    { 
        ps.Play();
        ChapterManager.Instance.onSetteled += AddStone;
    }
    private void OnDisable() 
    { 
        StopSnow(); 
        ps.Stop();
        ChapterManager.Instance.onSetteled -= AddStone;
    }
}
