using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadOrQuit : MonoBehaviour
{
    [Header("References")]
    public GameObject load;
    public GameObject quit;

    //엔딩을 본 상태면 종료버튼 활성화, 아니면 계속버튼을 활성화
    void Start()
    {
        if (PlayerPrefs.HasKey("sawEnding"))
        {
            if (PlayerPrefs.GetInt("sawEnding") == 1)
            {
                load.SetActive(false);
                quit.SetActive(true);
            }
            else
            {
                load.SetActive(true);
                quit.SetActive(false);
            }
        }
        else
        {
            load.SetActive(true);
            quit.SetActive(false);
        }
    }
}
