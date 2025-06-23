using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenaltyManager : MonoBehaviour
{
    public StoneSpawner StoneSpawner;

    [Header("페널티 설정")]
    [SerializeField] private float PenaltyTimer = 0.5f; // 여러 개의 돌이 떨어짐을 한 번으로 인식
    [SerializeField] private int BosalWarning = 3; // 보살이 알려줌
    [SerializeField] private int TreeCount = 5; // 수행목 색 변화
    [SerializeField] private int PenaltyStone = 7; // 번뇌돌 발생

    private int counter = 0;
    private bool canCount = true; // PenaltyTimer 동안 카운트 방지
    public void PenaltyCount()
    {
        if (!canCount)
        {
            Debug.Log("돌 굴러가유... 번뇌돌 카운트 잠깐 멈춰볼게유.");
            return;
        }

        counter++;
        if (counter >= BosalWarning)
        {
            //Bosal.Speak("그 돌도 나쁜 돌은 아니었겠지.")
            Debug.Log($"보살등장! 현재 카운트: {counter}");
        }
        if (counter >= TreeCount)
        {
            //Bosal.Speak("무언가가 마음에 쌓이고 있구나.")
            //TreeColorChange();
            Debug.Log($"수행목 색 변화! 현재 카운트: {counter}");
        }

        if (counter >= PenaltyStone)
        {
            //PenaltyStoneSpawn();
            //Bosal.Speak("버린 마음은 다시 돌아오는 법이라.")
            Debug.Log($"번뇌돌 발생! 현재 카운트: {counter}");
            StoneSpawner.Penalty = true; // StoneSpawner에서 번뇌돌 스폰 호출
            counter = 0; // 리셋
        }

        StartCoroutine(PenaltyCooldown());
    }

    public void PenaltyTrash()//번뇌돌을 버렸을 때
    {
        counter = 7;
        //Bosal.Speak("번뇌는 버리려 할 수록 늘어나는 법.");
    }

    private IEnumerator PenaltyCooldown()
    {
        canCount = false;
        yield return new WaitForSeconds(PenaltyTimer);
        canCount = true;
    }
}
