using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraLetterBox : MonoBehaviour
{
    [SerializeField] float targetWidth = 2160f;
    [SerializeField] float targetHeight = 1080f;

    Camera _cam;
    int _lastW, _lastH;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        SafeUpdateLetterbox(true);
    }

    void OnEnable()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        SafeUpdateLetterbox(true);
    }

    void OnValidate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        SafeUpdateLetterbox(true);
    }

    void Update()
    {
        SafeUpdateLetterbox(false);
    }

    void SafeUpdateLetterbox(bool force)
    {
        // 1) 입력값 가드
        if (targetWidth <= 0f || !float.IsFinite(targetWidth)) return;
        if (targetHeight <= 0f || !float.IsFinite(targetHeight)) return;

        // 2) 해상도 유효성 가드 (0 프레임 방지)
        int w = Mathf.Max(Screen.width, 1);
        int h = Mathf.Max(Screen.height, 1);

        // 해상도 변했을 때만 재계산 (에디터/런타임 부담↓)
        if (!force && w == _lastW && h == _lastH) return;

        _lastW = w; _lastH = h;

        float targetAspect = targetWidth / targetHeight;
        float windowAspect = (float)w / h;
        float scaleHeight = windowAspect / targetAspect;

        var rect = _cam.rect;

        if (scaleHeight < 1f)
        {
            rect.width = 1f;
            rect.height = Mathf.Clamp01(scaleHeight);
            rect.x = 0f;
            rect.y = (1f - rect.height) * 0.5f;
        }
        else
        {
            float scaleWidth = 1f / scaleHeight;      // scaleHeight>=1 → 안전
            rect.width = Mathf.Clamp01(scaleWidth);
            rect.height = 1f;
            rect.x = (1f - rect.width) * 0.5f;
            rect.y = 0f;
        }

        _cam.rect = rect;
    }
}