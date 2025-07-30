using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarnigPanel : MonoBehaviour
{
    public GameObject Panel;
    public void OpenWarning()
    {
        Panel.SetActive(true);
    }
    public void ResetGame()
    {
        SaveSystem.Instance.ResetGame();
    }
    public void CloseWarning()
    {
        Panel.SetActive(false);
    }
}
