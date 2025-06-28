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
    public BosalManager Bosal;
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
        Rain.MakeRain(false); // 비 활성화
        Bosal.Speak("번뇌는 받아들이지 않으면 비처럼 스며드나니.");
    }
    public void PenaltyCount()
    {
        counter++;
        if (counter == BosalWarning)
        {
            Bosal.Speak("그 돌도 나쁜 돌은 아니었겠지.");
            Debug.Log($"보살등장! 현재 카운트: {counter}");
        }
        if (counter == TreeCount)
        {
            Bosal.Speak("무언가가 마음에 쌓이고 있구나.");
            //TreeColorChange();
            Debug.Log($"수행목 색 변화! 현재 카운트: {counter}");
        }

        if (counter >= PenaltyStone)
        {
            Bosal.Speak("버린 마음은 다시 돌아오는 법이라.");
            Debug.Log($"번뇌돌 발생! 현재 카운트: {counter}");
            Penalty();
            counter = 0; // 리셋
        }

    }
    public void Penalty()
    {
        StoneSpawner.Penalty = true; // 다음 돌은 번뇌돌
        BnStone = true;
        StartCoroutine(WaitTicksUntilRain(RainTicks)); // RainTicks 만큼 대기
    }
    public void PenaltyTrash()//번뇌돌을 버렸을 때
    {
        Penalty();
        Debug.Log($"번뇌돌 버림! 현재 카운트: {counter}");
        Bosal.Speak("번뇌는 버리려 할수록 늘어나는 법.");
    }

    public void PenaltyStoneSettled()
    {
        Debug.Log($"PenaltyStoneSettled called.");
        StoneSpawner.Penalty = false; // 번뇌돌이 정착되면 다음 돌은 일반 돌
        counter = 0;
        Rain.StopRain(); // 비 비활성화
        Debug.Log($"번뇌돌 정화! 현재 카운트: {counter}");
        Bosal.Speak("받아들였으니, 이제 그 무게는 너를 짓누르지 않을 것이다.");
        //TreeColorReset();
    }
}
