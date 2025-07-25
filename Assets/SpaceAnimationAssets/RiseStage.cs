using System.Collections;
using UnityEngine;

public class RiseStage : MonoBehaviour
{
    [Header("Targets")]
    public Transform[] stones;   // 돌들
    [Header("Motion")]
    public float riseHeight = 0.5f;
    public float duration    = 1f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0,0,1,1);

    public IEnumerator Run()
    {
        Vector3[] startPos = new Vector3[stones.Length];
        for (int i = 0; i < stones.Length; i++)
            startPos[i] = stones[i].position;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float k = curve.Evaluate(t / duration);         // 0→1 커브
            for (int i = 0; i < stones.Length; i++)
                stones[i].position = startPos[i] + Vector3.up * (riseHeight * k);
            yield return null;
        }
        // 보정
        for (int i = 0; i < stones.Length; i++)
            stones[i].position = startPos[i] + Vector3.up * riseHeight;
    }
}