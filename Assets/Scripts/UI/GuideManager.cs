using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GuideManager : Singleton<GuideManager>
{

    [SerializeField] GuidePanel guidePanelRoot;
    protected override void Awake()
    {
        guidePanelRoot.gameObject.SetActive(false);
    }
    void SetGuide(string str) //chapterManager 등에서 개별 실행
    {
        bool open = guidePanelRoot.gameObject.activeSelf;
        guidePanelRoot.gameObject.SetActive(true);
        guidePanelRoot.PlayGuide(str);
        Time.timeScale = open ? 1f : 0f;
        SoundManager.Instance.PlaySFX("pause");
    }
}
