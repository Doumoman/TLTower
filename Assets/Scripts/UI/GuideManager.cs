using System.Collections;
using UnityEngine;

public class GuideManager : Singleton<GuideManager>
{

    [SerializeField] GuidePanelRoot guidePanelRoot;
    protected override void Awake()
    {
        base.Awake();
        if (guidePanelRoot != null)
            guidePanelRoot.gameObject.SetActive(false);
    }
    public void Play(string str)
    {
        Debug.Log($"GuideManager.PlayGuide({str}) called!");

        guidePanelRoot.PlayGuide(str);
        bool open = guidePanelRoot.gameObject.activeSelf;
        Time.timeScale = open ? 1f : 0f;
        SoundManager.Instance.PlaySFX("pause");
    }

    IEnumerator WaitPlay(string str, float waitTimes)
    {
        yield return new WaitForSeconds(waitTimes);

        Play(str);
    }
    public void PlayGuide(string str, float waitTimes = 2f)
    {
        StartCoroutine(WaitPlay(str, waitTimes));
    }
}
