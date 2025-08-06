using System.Collections;
using UnityEngine;

public class circleController : MonoBehaviour
{
    public int rock = 0;                // 현재 돌 개수
    private int preRock = 0;           // 이전 돌 개수
    private float currentRotation = 0f; // 누적 회전 각도
    private Coroutine rotateCoroutine;

    public float speed = 0.7f;
    private RectTransform rect;

    private int sameDirectionCount = 0;
    private int lastDirection = 0;
    private float lastDownTime = -1f;

    void Start()
    {
        rect = GetComponent<RectTransform>();

        float initialRotation = -360f / 11f * rock;
        currentRotation = initialRotation;
        rect.eulerAngles = new Vector3(0, 0, currentRotation);
        preRock = rock;

        ChapterManager.Instance.onSetteled += UP;
        ChapterManager.Instance.onDestroyed += Down;
    }

    void Update()
    {
        if (rock != preRock)
        {
            if (rotateCoroutine != null)
                StopCoroutine(rotateCoroutine);

            rotateCoroutine = StartCoroutine(Turn(rock));
            preRock = rock;
        }
    }

    IEnumerator Turn(int newRock)
    {
        int delta = newRock - preRock;
        if (delta == 0)
            yield break;

        // 연속 방향 카운터 관리
        int direction = delta > 0 ? 1 : -1;

        if (direction < 0)
        {
            float now = Time.unscaledTime;

            // 마지막 입력 이후 5초가 넘었다면 카운터 리셋
            if (now - lastDownTime > 5f)
                sameDirectionCount = 0;

            sameDirectionCount++;
            lastDownTime = now;

            if (sameDirectionCount == 3)
            {
                BosalManager.Instance.Speak("UCanDoIt");
            }
        }

        // 회전 각도 계산
        float rotationPerRock = 360f / 11f;
        float deltaRotation = -rotationPerRock * delta;

        float startRotation = -360 * ( rock / 11 + 1 ) + rect.eulerAngles.z;
        float targetRotation = currentRotation + deltaRotation;
        currentRotation = targetRotation % 360f;

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

    //증가/감소
    public void UP(object sender, System.EventArgs eventArgs) => rock++;
    public void Down(object sender, System.EventArgs eventArgs) => rock--;
}