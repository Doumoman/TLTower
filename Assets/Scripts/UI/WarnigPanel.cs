using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarnigPanel : MonoBehaviour
{
    public GameObject Panel;
    public SceneLoader sceneLoader;
    public void OpenWarning()
    {
        if (PlayerPrefs.GetInt("CurrentChapter", -1) == -1 || PlayerPrefs.GetInt("CurrentChapter", -1) != (int)chapter.spring)
        {
            SaveSystem.Instance.ResetGame();
            sceneLoader.LoadGame();
        }
        else
        {
            Panel.SetActive(true);
            SoundManager.Instance.PlaySFX("pause");
        }
    }
    public void ResetGame()
    {
        SaveSystem.Instance.ResetGame();
        SoundManager.Instance.PlaySFX("stamp_button");
        sceneLoader.LoadGame();
    }
    public void CloseWarning()
    {
        PlayerPrefs.SetInt("sawEnding", 0);
        PlayerPrefs.Save();
        SoundManager.Instance.PlaySFX("pause");
        Panel.SetActive(false);
    }
}
