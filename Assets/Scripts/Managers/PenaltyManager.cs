using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
번뇌돌 : FirstWarning 없애고 6개면 Penalty
번뇌돌 버리면 바로 Penalty

Penalty : 일정 tick 이후에 RainManager.MakeObstacle()
비 내림. 이미 비 내리고 있으면 바람이 붊. 다음 대사 무시.
*/
public class PenaltyManager : MonoBehaviour
{
    private int counter; // 페널티 카운트
    public bool BnStone; // 정화되지 않은 번뇌돌 카운트
    public StoneSpawner StoneSpawner;
    public RainSystem Rain;
    public WindSystem Wind;
    public bool rainAble = false; //여름에만 활성화
    public bool isRaining = false;
    public static PenaltyManager Instance { get; private set; }
    void Awake()
    {
        counter = 0;

        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("페널티 설정")]
    [SerializeField] private int BosalWarning = 3; // 보살이 알려줌
    [SerializeField] private int PenaltyStone = 6; // 번뇌돌 발생

    [SerializeField] private float obstacleWaitTime = 4f; // 해당 시간을 넘어가면 비 페널티 발생


    IEnumerator WaitTicksUntilRain(float obstacleWaitTime)
    {
        yield return new WaitForSeconds(obstacleWaitTime);
        if (rainAble)
        {
            if (isRaining)
            {
                Wind.MakeObstacle(false);
            }
            else
            {
                Rain.MakeObstacle(false);
            }
        } // 비 활성화
    }
    public void PenaltyCount()
    {
        counter++;
        if (counter == BosalWarning)
        {
            BosalManager.Instance.Speak("SecondWarning");
        }

        if (counter >= PenaltyStone)
        {
            Penalty();
            counter = 0; // 리셋
        }

    }
    public void Penalty()
    {
        if (ChapterManager.Instance.chapter == chapter.summer
        || ChapterManager.Instance.chapter == chapter.summer2
        || ChapterManager.Instance.chapter == chapter.summer3
        || ChapterManager.Instance.chapter == chapter.summer4)
            rainAble = true; //여름에만 활성화
        StoneSpawner.Penalty = true; // 다음 돌은 번뇌돌
        BnStone = true;
        BosalManager.Instance.Speak("KarmaStone");
        if (rainAble) StartCoroutine(WaitTicksUntilRain(obstacleWaitTime));
    }
    public void PenaltyTrash()//번뇌돌을 버렸을 때
    {
        Penalty();
        BosalManager.Instance.Speak("SecondWarning");
    }

    public void PenaltyStoneSettled()
    {
        Debug.Log($"PenaltyStoneSettled called.");
        if(StoneSpawner.Penalty == false) return;
        StoneSpawner.Penalty = false; // 번뇌돌이 정착되면 다음 돌은 일반 돌
        rainAble = false; //이미 큐잉된 비가 있다면 정지
        counter = 0;
        Rain.StopRain(); // 비 비활성화
        Wind.StopWind();
        BosalManager.Instance.Speak("Purify");
        SoundManager.Instance.PlaySFX("affliction_purified");
        //TreeColorReset();
    }
}
