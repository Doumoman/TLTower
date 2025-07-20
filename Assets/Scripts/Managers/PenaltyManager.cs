using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenaltyManager : MonoBehaviour
{
    private int counter; // 페널티 카운트
    public bool BnStone; // 정화되지 않은 번뇌돌 카운트
    public StoneSpawner StoneSpawner;
    public RainSystemTest Rain;
    public static PenaltyManager Instance { get; private set; }
    void Awake()
    {
        counter = 0;
    }

    [Header("페널티 설정")]
    [SerializeField] private int BosalWarning = 3; // 보살이 알려줌
    [SerializeField] private int TreeCount = 5; // 수행목 색 변화
    [SerializeField] private int PenaltyStone = 7; // 번뇌돌 발생

    [SerializeField] private int RainTicks = 1; // 해당 시간을 넘어가면 비 페널티 발생

    //싱글톤 구현
    private void Start()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    IEnumerator WaitTicksUntilRain(int ticks)
    {
        yield return TickManager.Instance.TickWait(ticks);
        Rain.MakeRain(true); // 비 활성화

    }
    public void PenaltyCount()
    {
        counter++;
        if (counter == BosalWarning)
        {
            BosalManager.Instance.Speak("FirstWarning");
        }
        if (counter == TreeCount)
        {
            BosalManager.Instance.Speak("SecondWarning");
            //TreeColorChange();
        }

        if (counter >= PenaltyStone)
        {
            BosalManager.Instance.Speak("버린 마음은 다시 돌아오는 법이라.");
            Penalty();
            counter = 0; // 리셋
        }

    }
    public void Penalty()
    {
        StoneSpawner.Penalty = true; // 다음 돌은 번뇌돌
        BnStone = true;
        BosalManager.Instance.Speak("KarmaStone");
        StartCoroutine(WaitTicksUntilRain(RainTicks)); // RainTicks 만큼 대기
    }
    public void PenaltyTrash()//번뇌돌을 버렸을 때
    {
        Penalty();
        BosalManager.Instance.Speak("SecondWarning");
    }

    public void PenaltyStoneSettled()
    {
        Debug.Log($"PenaltyStoneSettled called.");
        StoneSpawner.Penalty = false; // 번뇌돌이 정착되면 다음 돌은 일반 돌
        counter = 0;
        Rain.StopRain(); // 비 비활성화
        BosalManager.Instance.Speak("Purify");
        SoundManager.Instance.PlaySFX("affliction_purified");
        //TreeColorReset();
    }
}
