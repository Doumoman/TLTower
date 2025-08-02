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
    public void LoadGame()
    {
        SceneManager.LoadScene("TLTower");
        Time.timeScale = 1.0f;
        SoundManager.Instance.PlaySFX("game_start");
    }
}
