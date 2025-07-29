using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.EventSystems;

public enum GuideUIState
{
    None,
    GuidePrimary,
    GuideThenPause,
    GuideReturn,
    PauseOnly,
    PauseThenGuide
}

public class GuideManager : Singleton<GuideManager>
{
    [Header("Panel Roots")]
    [SerializeField] private PausePanel pausePanel;       // Pause 루트 + UI
    [SerializeField] private GameObject guidePanelRoot;    // Guide 루트 오브젝트
    [SerializeField] private GuidePanel guidePanel;       // 루트 안의 스크립트

    /* 내부 상태 */
    private readonly List<string> played = new();
    private GuideUIState current = GuideUIState.None;
    private GuideUIState prev    = GuideUIState.None;

    /* ───────── 초기화 ───────── */
    protected override void Awake()
    {
        base.Awake();
        pausePanel.Closepausepanel();
        guidePanelRoot.SetActive(false);
    }

    /* ───────── 매 프레임 ───────── */
    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
#else
        if (Input.GetKeyDown(KeyCode.Escape))
#endif
            HandleEsc();

        // 카메라 드래그 제어
        if (CameraController.Instance != null)
            CameraController.Instance._userMoveInput = (current == GuideUIState.None);

        // 사운드 · 타임스케일 (상태 변화 시만)
        if (prev != current)
        {
            Time.timeScale = (current == GuideUIState.None) ? 1f : 0f;
            if (current == GuideUIState.None) SoundManager.Instance.Resume();
            else SoundManager.Instance.PauseBGM();
            prev = current;
        }
    }

    /* ───────── ESC 흐름 ───────── */
    public void HandleEsc()
    {
        Debug.Log($"ESC, state={current}");
        switch (current)
        {
            case GuideUIState.GuidePrimary:
                HideGuideRoot();
                ShowPause();
                current = GuideUIState.GuideThenPause;
                break;

            case GuideUIState.GuideThenPause:
                HidePause();
                ShowGuideRoot();
                current = GuideUIState.GuideReturn;
                break;

            case GuideUIState.GuideReturn:
                HideGuideRoot();
                current = GuideUIState.None;
                break;

            case GuideUIState.PauseOnly:
                HidePause();
                current = GuideUIState.None;
                break;

            case GuideUIState.PauseThenGuide:
                HideGuideRoot();
                ShowPause();
                current = GuideUIState.PauseOnly;
                break;
        }
        SoundManager.Instance.PlaySFX("pause");
        ClearSelection();
    }

    /* ───────── Pause 토글 버튼 ───────── */
    public void TogglePause()
    {
        if (current == GuideUIState.None)
        {
            ShowPause();
            current = GuideUIState.PauseOnly;
            SoundManager.Instance.PlaySFX("pause");
        }
        else if (current == GuideUIState.PauseOnly)
        {
            HidePause();
            current = GuideUIState.None;
            SoundManager.Instance.PlaySFX("pause");
        }
    }

    /* ───────── 자동 가이드 ───────── */
    public void PlayGuide(string key, float delay = 0f)
    {
        if (played.Contains(key)) return;
        if (delay <= 0f) StartPlay(key);
        else StartCoroutine(DelayPlay(key, delay));
    }
    private IEnumerator DelayPlay(string key, float d)
    {
        yield return new WaitForSecondsRealtime(d);
        StartPlay(key);
    }
    private void StartPlay(string key)
    {
        ShowGuideRoot();               // 루트 먼저 활성화
        guidePanel.PlayGuide(key);     // 코루틴/로직 실행
        HidePause();                   // Pause 끔

        current = GuideUIState.GuidePrimary;
        SoundManager.Instance.PlaySFX("pause");
        played.Add(key);
        ClearSelection();
    }

    /* ───────── Pause → ButtonGuide ───────── */
    public void ButtonGuide()          // Pause 버튼에서 호출
    {
        if (current != GuideUIState.PauseOnly) return;
        HidePause();                   // Pause 닫고
        ShowGuideRoot();
        guidePanel.ButtonGuide();

        current = GuideUIState.PauseThenGuide;
        SoundManager.Instance.PlaySFX("pause");
        ClearSelection();
    }

    /* ───────── Helper : Root 표시/숨김 ───────── */
    private void ShowGuideRoot()
    {
        if (!guidePanelRoot.activeSelf) guidePanelRoot.SetActive(true);
    }
    private void HideGuideRoot()
    {
        if (guidePanelRoot.activeSelf) guidePanelRoot.SetActive(false);
    }
    private void ShowPause()  => pausePanel.Openpausepanel();
    private void HidePause()  => pausePanel.Closepausepanel();

    private static void ClearSelection()
    {
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
    }
}