using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraLetterBox : MonoBehaviour
{
    // 목표 비율 (가로 / 세로)
    [SerializeField] float targetWidth  = 2160f;
    [SerializeField] float targetHeight = 1080f;

    Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        UpdateLetterbox();
    }

    void OnValidate()
    {
        // 에디터에서 값이 바뀔 때 즉시 반영
        if (_cam == null) _cam = GetComponent<Camera>();
        UpdateLetterbox();
    }

    void UpdateLetterbox()
    {
        float targetAspect = targetWidth / targetHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Rect rect = _cam.rect;

        if (scaleHeight < 1.0f)
        {
            // 창이 더 좁아서 상하에 검은 바 생김
            rect.width  = 1.0f;
            rect.height = scaleHeight;
            rect.x      = 0;
            rect.y      = (1.0f - scaleHeight) / 2.0f;
        }
        else
        {
            // 창이 더 넓어서 좌우에 검은 바 생김
            float scaleWidth = 1.0f / scaleHeight;
            rect.width  = scaleWidth;
            rect.height = 1.0f;
            rect.x      = (1.0f - scaleWidth) / 2.0f;
            rect.y      = 0;
        }

        _cam.rect = rect;
    }

    void Update()
    {
        // 빌드된 화면 크기가 바뀔 때마다 레터박스 재계산
        UpdateLetterbox();
    }
}