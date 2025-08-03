using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera cam;
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private float fixedOrthoSize = 5f;

    [Header("UI Roots")]
    public GameObject pausepanel; // 실제로 켜고 끌 루트

    // 외부에서 호출: GuideManager 전용
    public void Openpausepanel()
    {
        Debug.Log($"Openpausepanel 호출됨 - pausepanel: {(pausepanel != null ? pausepanel.name : "null")}, activeSelf: {(pausepanel != null ? pausepanel.activeSelf.ToString() : "N/A")}");

        if (pausepanel != null && !pausepanel.activeSelf)
        {
            pausepanel.SetActive(true);
            Debug.Log("PausePanel 활성화 완료");
        }
        else
        {
            Debug.LogWarning($"PausePanel 활성화 실패 - pausepanel이 null이거나 이미 활성화됨");
        }
    }

    public void Closepausepanel()
    {
        if (pausepanel != null && pausepanel.activeSelf)
            pausepanel.SetActive(false);
    }

    // (선택) 종횡비 설정: 기존 코드 유지
    public void Set16by9() => ApplyAspect(16f / 9f, new Vector2(1920, 1080));
    public void Set2by1() => ApplyAspect(2f / 1f, new Vector2(2160, 1080));

    void ApplyAspect(float targetAspect, Vector2 refRes)
    {
        cam.orthographicSize = fixedOrthoSize;
        float windowAspect = (float)Screen.width / Screen.height;

        if (Mathf.Abs(windowAspect - targetAspect) < 0.01f)
        {
            cam.rect = new Rect(0, 0, 1, 1);
        }
        else if (windowAspect > targetAspect)
        {
            float scale = targetAspect / windowAspect;
            float offset = (1f - scale) * 0.5f;
            cam.rect = new Rect(offset, 0, scale, 1);
        }
        else
        {
            float scale = windowAspect / targetAspect;
            float offset = (1f - scale) * 0.5f;
            cam.rect = new Rect(0, offset, 1, scale);
        }

        BosalManager.Instance.TextAlign(cam);

        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = refRes;
            scaler.matchWidthOrHeight = 1f; // Height 기준
        }
    }
}