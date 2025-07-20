using System.Collections;
using UnityEngine;

public class circleController : MonoBehaviour
{
    public int rock = 0;                // 현재 돌 개수
    private int prevRock = 0;           // 이전 돌 개수
    private float currentRotation = 0f; // 누적 회전 각도
    private Coroutine rotateCoroutine;

    public float speed = 0.7f;
    private RectTransform rect;

    private int sameDirectionCount = 0;
    private int lastDirection = 0;

    void Start()
    {
        rect = GetComponent<RectTransform>();

        float initialRotation = -360f / 11f * rock;
        currentRotation = initialRotation;
        rect.eulerAngles = new Vector3(0, 0, currentRotation);
        prevRock = rock;
    }

    void Update()
    {
        if (rock != prevRock)
        {
            if (rotateCoroutine != null)
                StopCoroutine(rotateCoroutine);

            rotateCoroutine = StartCoroutine(Turn(rock));
            prevRock = rock;
        }
    }

    IEnumerator Turn(int newRock)
    {
        int delta = newRock - prevRock;
        if (delta == 0)
            yield break;

        // 연속 방향 카운터 관리
        int direction = delta > 0 ? 1 : -1;
        if (direction == lastDirection)
        {
            sameDirectionCount++;
        }
        else
        {
            sameDirectionCount = 1;
            lastDirection = direction;
        }

        // 대사 출력
        if (sameDirectionCount >= 3 && direction < 0)
        {
            BosalManager.Instance.Speak("UCanDoIt");
            sameDirectionCount = 0;
        }

        // 회전 각도 계산
        float rotationPerRock = 360f / 11f;
        float deltaRotation = -rotationPerRock * delta;

        float startRotation = currentRotation;
        float targetRotation = currentRotation + deltaRotation;

        // 사운드 재생
        SoundManager.Instance.PlaySFX(delta > 0 ? "yumju" : "yumju_revert");

        float t = 0f;
        while (true)
        {
            yield return null;
            t += Time.unscaledDeltaTime * speed;
            float z = Mathf.LerpAngle(startRotation, targetRotation, t);
            rect.eulerAngles = new Vector3(0, 0, z);

            if (Mathf.Abs(Mathf.DeltaAngle(z, targetRotation)) < 0.5f)
                break;
        }

        currentRotation = targetRotation % 360f;
        rect.eulerAngles = new Vector3(0, 0, currentRotation);
    }

    // 디버그용 수동 증가/감소
    public void testUP() => rock++;
    public void testDown() => rock--;
}
