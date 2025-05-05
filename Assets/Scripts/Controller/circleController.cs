using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;


//해결해야할 과제 - 회전 중간에 돌 개수 변경시 끊김(못하겠음), 색깔 추가하기
public class circleController : MonoBehaviour
{
    public GameObject twentySeventh;
    public int rock;        //쌓은 돌의 개수
    public float duration = 0.7f;
    RectTransform rect;
    public float seta0;     //회전 애니메이션에서 사용할 현재 각도
    public float seta1;     //회전 애니메이션에서 사용할 목표 각도
    private int rock0;      //돌 개수 변화 감지를 위한 이전 돌 개수
    Coroutine corutine;

    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        seta1 = -90f / 7 * rock;
        rect.eulerAngles = new Vector3(0, 0, seta1);        
        rock0 = rock;
    }

    public void OnAnimationExit()
    {
        GetComponent<Animator>().enabled = false;   //등장 이후 애니메이션은 스크립트로 작동
    }
    // Update is called once per frame
    private void Update()
    {
        //돌 개수에 변화가 생길시 코루틴 호출
        if (rock != rock0/*GameManager.instance.rockNum*/)
        {
            rock0 = rock;               //돌 개수 업데이트
            if (corutine != null)       //기존 회전을 멈추고 새로운 회전을 시작
                StopCoroutine(corutine);
            corutine = StartCoroutine(Turn(rock));
        }
    }

    //회전하는 동안 다른 작업도 가능하게 코루틴 함수로 구현
    public IEnumerator Turn(int n)
    {
        float t = 0f;
        seta0 = seta1;              //   **회전중에 다시 회전시작하면 끊김 발생**
        seta1 = -90f / 7 * n;
        while (true)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
            float newZ = Mathf.Lerp(seta0, seta1, t / duration);
            rect.eulerAngles = new Vector3(0, 0, newZ);
            if((Mathf.Abs(newZ - seta1)) < 0.05f)   //오차 범위 이내라면 종료
            {
                yield break;
            }
        }
    }

    public void testUP()
    {
        rock++;
    }
    public void testDown()
    {
        rock--;
    }
}
