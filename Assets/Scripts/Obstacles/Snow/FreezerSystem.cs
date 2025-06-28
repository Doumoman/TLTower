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
    public List<ParticleSystem> psList;  //그냥 눈에 보이는 용도의 파티클들

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
        fs.freezeOnStart = true;  //생성시 부터 얼려서 돌 생성하기
        foreach (ParticleSystem p in psList) p.Play();

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
        foreach (ParticleSystem p in psList) p.Stop();
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
