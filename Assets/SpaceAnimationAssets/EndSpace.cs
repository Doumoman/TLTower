using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndSpace : MonoBehaviour
{
    public static EndSpace Instance { get; private set; }
    public float returnTime = 10f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    public void EndOfSpace() => StartCoroutine(ReturnToMain());

    IEnumerator ReturnToMain()
    {
        yield return new WaitForSeconds(returnTime);
        PlayerPrefs.SetInt("sawEnding", 1); //엔딩 봤다는 사실 기록
        PlayerPrefs.Save();
        SceneManager.LoadScene("main");
    }
}
