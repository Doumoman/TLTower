using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class QuitPanel : MonoBehaviour
{
    public GameObject quitpanel;
    public void Openquitpanel()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        quitpanel.SetActive(true);
    }
    public void Closequitpanel()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        quitpanel.SetActive(false);
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
