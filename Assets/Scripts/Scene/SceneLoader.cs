using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader: MonoBehaviour
{
    public void LoadMain()
    {
        SceneManager.LoadScene("main");
        SoundManager.Instance.PlaySFX("reset_button");
        Time.timeScale = 1.0f;
    }

    public void LoadMainWithoutSFX()
    {
        SceneManager.LoadScene("main");
        Time.timeScale = 1.0f;
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("TLTower");
        Time.timeScale = 1.0f;
        SoundManager.Instance.PlaySFX("game_start");
        if (PlayerPrefs.GetInt("CurrentChapter", -1) == (int)chapter.space)
            SoundManager.Instance.spaceLoaded = true;
    }
}
