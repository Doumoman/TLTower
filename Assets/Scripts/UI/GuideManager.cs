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
    private GuideUIState prev = GuideUIState.None;

    protected override void Awake()
    {
        base.Awake();
        pausePanel.Closepausepanel();
        guidePanelRoot.SetActive(false);

        guidePanel.GuideClosed.AddListener(OnGuideClosed); 
    }

    private void ShowGuide() => guidePanelRoot.SetActive(true);
    private void HideGuide() => guidePanelRoot.SetActive(false);

    private void ShowPause() => pausePanel.Openpausepanel();
    private void HidePause() => pausePanel.Closepausepanel();

    private void OnGuideClosed()
    {
        if (current == GuideUIState.PauseThenGuide)
        {
            HideGuide();
            ShowPause();
            current = GuideUIState.PauseOnly;
        }
        else
        {
            HideGuide();
            current = GuideUIState.None;
        }
        ClearSelection();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) HandleEsc();

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
            case GuideUIState.None:
                ShowPause();
                current = GuideUIState.PauseOnly;
                break;
            case GuideUIState.GuidePrimary:
                HideGuide();
                ShowPause();
                current = GuideUIState.GuideThenPause;
                break;

            case GuideUIState.GuideThenPause:
                HidePause();
                ShowGuide();
                current = GuideUIState.GuideReturn;
                break;

            case GuideUIState.GuideReturn:
                HideGuide();
                current = GuideUIState.None;
                break;

            case GuideUIState.PauseOnly:
                HidePause();
                current = GuideUIState.None;
                break;

            case GuideUIState.PauseThenGuide:
                HideGuide();
                ShowPause();
                current = GuideUIState.PauseOnly;
                break;
        }
        SoundManager.Instance.PlaySFX("pause");
        ClearSelection();
    }

    /* ───────── Pause 토글 버튼 ───────── */

    [SerializeField] private GameObject pauseButton;
    public void TogglePause()
    {
        UnityEngine.Debug.Log($"TogglePause, state={current}");
        if (current == GuideUIState.None) // 게임 화면에서 Pause 버튼 눌렀을 때
        {
            ShowPause();
            pauseButton.SetActive(false); // Pause 버튼 숨김
            current = GuideUIState.PauseOnly;
            SoundManager.Instance.PlaySFX("pause");
        }
        else if (current == GuideUIState.PauseOnly) // Pause 상태에서 주변 화면 눌렀을 때
        {
            HidePause();
            pauseButton.SetActive(true); // Pause 버튼 다시 보임
            current = GuideUIState.None;
            SoundManager.Instance.PlaySFX("pause");
        }
        else if (current == GuideUIState.GuidePrimary) // PlayGuide 호출 후 Pause 버튼 눌렀을 때
        {
            pauseButton.SetActive(false); // Pause 버튼 숨김
            HideGuide();
            ShowPause();
            current = GuideUIState.GuideThenPause;
        }
        else if (current == GuideUIState.GuideThenPause) // PlayGuide 호출 후 Pause 상태에서 주변 화면 눌렀을 때
        {
            pauseButton.SetActive(true); // Pause 버튼 보임
            HidePause();
            ShowGuide();
            current = GuideUIState.GuideReturn;
        }
        else if (current == GuideUIState.GuideReturn) // Guide 상태에서 Pause 버튼 눌렀을 때
        {
            pauseButton.SetActive(false); // Pause 버튼 숨김
            HideGuide();
            ShowPause();
            current = GuideUIState.GuideThenPause; // esc는 그냥 끄지만 Pause 버튼은 다시 Pause로 돌아감
        }
        else if (current == GuideUIState.PauseThenGuide) // Pause 상태에서 가이드 호출 시 주변 화면 눌렀을 때
        {
            pauseButton.SetActive(false); // Pause 버튼 숨김 (PauseThenGuide 상태에서도 숨어 있음)
            HideGuide();
            ShowPause();
            current = GuideUIState.PauseOnly;
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
        ShowGuide();               // 루트 먼저 활성화
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
        HidePause();                   // Pause 닫고
        ShowGuide();
        guidePanel.ButtonGuide();

        current = GuideUIState.PauseThenGuide;
        pauseButton.SetActive(false); // Pause 버튼 숨김
        SoundManager.Instance.PlaySFX("pause");
        ClearSelection();
    }

    /* ───────── Root 표시/숨김 ───────── */
    private static void ClearSelection()
    {
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(null);
    }
}