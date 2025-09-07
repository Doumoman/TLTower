using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadOrQuit : MonoBehaviour
{
    [Header("References")]
    public GameObject button;
    void Start()
    {
        Debug.Log(PlayerPrefs.GetInt("CurrentChapter", -1));
        if (!PlayerPrefs.HasKey("CurrentChapter") || PlayerPrefs.GetInt("CurrentChapter") == (int)chapter.spring)
        {
            button.GetComponent<UnityEngine.UI.Button>().interactable = false;
            button.GetComponent<UnityEngine.UI.Image>().color = new Color(.8f, .8f, .8f, 0.5f);
        }
        else
        {
            button.GetComponent<UnityEngine.UI.Button>().interactable = true;
            button.GetComponent<UnityEngine.UI.Image>().color = new Color(1, 1, 1, 1f);
        }
    }

    public void NewGame()
    {
        button.SetActive(false);
    }
}
