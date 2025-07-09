using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera cam;
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private float fixedOrthoSize = 5f;
    public void Set16by9() => ApplyAspect(16f / 9f, new Vector2(1920, 1080));
    public void Set2by1() => ApplyAspect(2f / 1f, new Vector2(2160, 1080));


    public GameObject pausepanel; // 일시정지 팝업
    // Start is called before the first frame update
    public void Openpausepanel()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        pausepanel.SetActive(true);
    }
    public void Closepausepanel()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        pausepanel.SetActive(false);
    }
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
            // 기기 가로가 더 넓다 → 좌·우 필러박스
            float scale = targetAspect / windowAspect;
            float offset = (1f - scale) * 0.5f;
            cam.rect = new Rect(offset, 0, scale, 1);
        }
        else   // 기기 세로가 더 길다 → 상·하 레터박스
        {
            float scale = windowAspect / targetAspect;
            float offset = (1f - scale) * 0.5f;
            cam.rect = new Rect(0, offset, 1, scale);
        }

        // UI Canvas 비율도 세로 고정
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = refRes;
            scaler.matchWidthOrHeight = 1f;   // Height 기준
        }
    }

}
