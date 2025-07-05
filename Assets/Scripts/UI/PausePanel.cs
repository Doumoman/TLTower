using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePanel : MonoBehaviour
{
    public GameObject pausepanel; // 일시정지 팝업
    // Start is called before the first frame update
    public void Openpausepanel()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        pausepanel.SetActive(true);
    }
    public void Closepausepanel()
    {
        //SoundManager.Instance.EffectSoundOn("3");
        pausepanel.SetActive(false);
    }

}
