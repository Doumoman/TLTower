using UnityEngine;
using UnityEngine.Playables;

public class MaterialDissolvePlayableBehaviour : PlayableBehaviour
{
    public Material targetMat;      // 머티리얼 (InkDissolve)
    public float from = 0f;         // 클립 시작 시 값
    public float to = 1f;         // 클립 끝   시 값

    static readonly int ProgressID = Shader.PropertyToID("_Progress");

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (targetMat == null) return;

        // 클립 안에서 0~1 시간값
        float normTime = (float)(playable.GetTime() / playable.GetDuration());
        float value = Mathf.Lerp(from, to, normTime);
        targetMat.SetFloat(ProgressID, value);
    }
}