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
        SoundManager.Instance.PlaySFX("pause");
        pausepanel.SetActive(true);
    }
    void Update()
    {
        // Android Back(PC·에디터에선 Esc) 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausepanel.activeSelf)
                Closepausepanel();   // 이미 열려 있으면 닫기
            else
                Openpausepanel();    // 닫혀 있으면 열기
        }
    }
    public void Closepausepanel()
    {
        SoundManager.Instance.PlaySFX("pause");
        pausepanel.SetActive(false);
    }
    void ApplyAspect(float targetAspect, Vector2 refRes)
    {
        SoundManager.Instance.PlaySFX("stamp_button"); //Nigger, Smaller에도 넣으면 됨

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
