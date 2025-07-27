using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideManager : Singleton<GuideManager>
{
    [SerializeField] GuidePanelRoot guidePanelRoot;
    private List<string> played = new(); //가이드는 한 번만 보여주도록
    protected override void Awake()
    {
        base.Awake();
        if (guidePanelRoot != null)
            guidePanelRoot.gameObject.SetActive(false);
    }
    public void Play(string str)
    {
        Debug.Log($"GuideManager.PlayGuide({str}) called!");

        if (played.Contains(str)) return;
        bool open = guidePanelRoot.gameObject.activeSelf;
        guidePanelRoot.PlayGuide(str);
        Time.timeScale = open ? 1f : 0f;
        if(open)
        {
            SoundManager.Instance.Resume();
        }
        else
        {
            SoundManager.Instance.PauseBGM();
        }
        CameraController.Instance._userMoveInput = !open; // 드래그 입력 제어
        SoundManager.Instance.PlaySFX("pause");
        played.Add(str);
    }

    IEnumerator WaitPlay(string str, float waitTimes)
    {
        yield return new WaitForSeconds(waitTimes);

        Play(str);
    }
    public void PlayGuide(string str, float waitTimes = 2f)
    {
        if (waitTimes <= 0f)
        {
            Play(str);
            return;
        }
        StartCoroutine(WaitPlay(str, waitTimes));
    }
}
