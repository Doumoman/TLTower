using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezerSystem : TickBasedObstacle
{

    Coroutine co;
    FreezerSettler fs;

    [Header("Settings")]
    public float durationTick = 7; //지속시간

    public ParticleSystem snowPs;
    public List<ParticleSystem> psList;  //그냥 눈에 보이는 용도의 파티클들

    public override void MakeObstacle(bool autoStop = true)
    {
        SoundManager.Instance.PlaySFX("blizzard_ambient");
        fs.freezeOnStart = true;  //생성시 부터 얼려서 돌 생성하기
        foreach (ParticleSystem p in psList) p.Play();

        TickManager.Instance.OnTickEvent -= OnTick;   //지속시간 동안은 틱 카운트x
        if (!autoStop) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(StopDelay());
    }

    IEnumerator StopDelay()   //일정 시간 후 끄기
    {
        yield return new WaitForSeconds(durationTick * TickManager.Instance.Tick);
        StopSnow();
        co = null;
    }

    public void StopSnow()
    {
        fs.freezeOnStart = false;
        TickManager.Instance.OnTickEvent += OnTick;  //틱카운트 재개
    }

    protected override void OnEnable() 
    { 
        base.OnEnable();

        foreach (ParticleSystem p in psList) p.Play();
        snowPs.Play();
        fs = FreezerSettler.Instance;
    }
    protected override void OnDisable() 
    {
        base.OnDisable();

        foreach (ParticleSystem p in psList) p.Stop();
        StopSnow();
        TickManager.Instance.OnTickEvent -= OnTick;  //틱카운트 끄기
        snowPs.Stop();
    }
}
