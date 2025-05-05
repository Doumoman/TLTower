using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LodingSceneController : MonoBehaviour
{
    static string nextScene = "main";

    [SerializeField]
    Image progressbar;
  
    void Start()
    {
        StartCoroutine(LoadSceneProcess());     //main 씬을 불러우는 함수 호출
    }

    /*씬 로드 진행도 만큼 이미지를 채우는 함수*/
    IEnumerator LoadSceneProcess()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);        //main 씬을 불러오는 동시에 진행도를 op에 대입
        op.allowSceneActivation = false;        //씬을 넘기는 속도가 너무 빠르지 않게 씬 전환 비활성화

        float timer = 0f;
        while(!op.isDone)
        {
            yield return null;

            if (op.progress < 0.9f) //main씬 로드가 진행도가 90% 이하일 때, 씬 로드 진행도 만큼 채우기
            {
                progressbar.fillAmount = op.progress;
            }
            else    //씬 로드 진행도가 90% 이상일 떄 2초 동안 이미지 채운 후 씬 전환
            {
                timer += Time.unscaledDeltaTime;
                progressbar.fillAmount = Mathf.Lerp(0.9f, 1f, timer* 0.5f);
                
                if(progressbar.fillAmount >= 1f)
                {
                    op.allowSceneActivation = true;
                    yield break;
                }
            }
        }
    }
}
