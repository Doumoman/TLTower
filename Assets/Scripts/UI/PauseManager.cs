#if ENABLE_INPUT_SYSTEM   // Input System 패키지가 켜져 있을 때만
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using FMOD;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    [SerializeField] GameObject pausePanelRoot;
    [SerializeField] GameObject guidePanelRoot; // 가이드 패널

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pausePanelRoot.SetActive(false);
        Input.backButtonLeavesApp = false;   // 뒤로가기 → 바로 종료 방지
    }

    void Update()
    {
        CameraController.Instance._userMoveInput = (!pausePanelRoot.activeSelf && !guidePanelRoot.activeSelf); // 드래그 입력 제어
#if ENABLE_INPUT_SYSTEM
        // Input System이 켜져 있을 때
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
#else
        // 구 Input Manager
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
#endif
    }

    public void TogglePause()
    {
        bool open = pausePanelRoot.activeSelf;
        pausePanelRoot.SetActive(!open);
        Time.timeScale = open ? 1f : 0f;
        if(open)
        {
            SoundManager.Instance.Resume();
        }
        else
        {
            SoundManager.Instance.PauseBGM();
        }
        SoundManager.Instance.PlaySFX("pause");
    }
}