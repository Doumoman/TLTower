#if ENABLE_INPUT_SYSTEM   // Input System 패키지가 켜져 있을 때만
using UnityEngine.InputSystem;
#endif
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    [SerializeField] GameObject pausePanelRoot;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pausePanelRoot.SetActive(false);
        Input.backButtonLeavesApp = false;   // 뒤로가기 → 바로 종료 방지
    }

    void Update()
    {
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
    }
}