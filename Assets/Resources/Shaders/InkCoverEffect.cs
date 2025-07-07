using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class InkCoverEffect : MonoBehaviour
{
    [Header("Ink Cover Material")]
    public Material inkMat;              // InkCover 셰이더 머티리얼
    [Header("진행 시간")]
    public float duration = 1.0f;        // 1초 동안 덮이기
    public bool playOnStart = true;

    float progress = 0f;
    static readonly int ProgressID = Shader.PropertyToID("_Progress");

    void Start()
    {
        if (playOnStart) StartCoroutine(PlayInkCover());
    }

    public IEnumerator PlayInkCover()
    {
        progress = 0f;
        while (progress < 1f)
        {
            progress += Time.unscaledDeltaTime / duration;
            inkMat.SetFloat(ProgressID, progress);
            yield return null;
        }
        inkMat.SetFloat(ProgressID, 1f);
    }

    // ▶▶ 후처리 핵심
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (inkMat == null) { Graphics.Blit(src, dest); return; }
        inkMat.SetFloat(ProgressID, progress);
        Graphics.Blit(src, dest, inkMat);
    }
}