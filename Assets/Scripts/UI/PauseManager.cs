using UnityEngine;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI 루트")]
    [SerializeField] GameObject pausePanelRoot;   // ← 비활성화 대상

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 처음엔 UI만 꺼 두고, 이 스크립트 오브젝트는 켜 둔다
        pausePanelRoot.SetActive(false);

        // ‘뒤로가기’로 바로 앱이 종료되지 않도록
        Input.backButtonLeavesApp = false;
    }

#if UNITY_ANDROID || UNITY_EDITOR   // ▶ 에디터·안드로이드에서만 동작
    void Update()
    {
        // 안드로이드 ‘뒤로가기’ 버튼 == KeyCode.Escape
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }
#endif

    /* ---------- 외부에서도 호출 가능 ---------- */
    public void TogglePause()
    {
        bool nowOpen = pausePanelRoot.activeSelf;
        pausePanelRoot.SetActive(!nowOpen);
        Time.timeScale = nowOpen ? 1f : 0f;   // 일시정지/재개
    }
}