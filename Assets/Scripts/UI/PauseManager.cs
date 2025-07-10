using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
        Input.backButtonLeavesApp = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    /* ---------- 외부에서도 호출 가능 ---------- */
    public void TogglePause()
    {
        bool nowOpen = pausePanelRoot.activeSelf;
        pausePanelRoot.SetActive(!nowOpen);
        Time.timeScale = nowOpen ? 1f : 0f;   // 일시정지/재개
    }
}