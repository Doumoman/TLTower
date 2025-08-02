using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarnigPanel : MonoBehaviour
{
    public GameObject Panel;
    public void OpenWarning()
    {
        Panel.SetActive(true);
        SoundManager.Instance.PlaySFX("pause");
    }
    public void ResetGame()
    {
        SaveSystem.Instance.ResetGame();
        SoundManager.Instance.PlaySFX("stamp_button");
    }
    public void CloseWarning()
    {
        SoundManager.Instance.PlaySFX("pause");
        Panel.SetActive(false);
    }
}
