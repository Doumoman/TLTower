using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 클릭 시 지정한 PlayerPrefs 값을 설정하거나 0↔1 토글.
/// </summary>
[RequireComponent(typeof(Button))]
public class PlayerPrefsButton : MonoBehaviour
{
    [Header("설정할 PlayerPrefs 키/값")]
    public string key = "StoneTimelinePlayed";  // 수정 가능
    public int setValue = 1;                 // toggle=false 일 때 적용할 값


    [Header("챕터 지정")]
    [SerializeField] QuickChapter targetChapter = QuickChapter.spring;
    public SceneLoader sceneLoader;


    [Header("토글 모드")]
    public bool toggle = false;   // true이면 0↔1 자동 전환

    void Awake()
    {
        // 같은 오브젝트에 있는 Button 컴포넌트에 리스너 등록
        GetComponent<Button>().onClick.AddListener(ApplyChange);
    }

    /// 버튼이 눌리면 실행되는 메서드
    void ApplyChange()
    {
        if (toggle)
        {
            // 현재 값 읽어서 0이면 1, 1이면 0으로 전환
            int cur = PlayerPrefs.GetInt(key, 0);
            PlayerPrefs.SetInt(key, cur == 0 ? 1 : 0);
        }
        else
        {
            // 지정된 setValue로 고정 설정
            PlayerPrefs.SetInt(key, setValue);
        }

        PlayerPrefs.Save();  // 디스크에 즉시 저장
        Debug.Log($"PlayerPrefs '{key}' = {PlayerPrefs.GetInt(key)}");
    }
    public void ResetChapter()
    {
        SaveSystem.Instance.ResetGame();
    }
    public void ChapterChange()
    {
        SaveSystem.SetChapter(targetChapter);
        sceneLoader.LoadGame();
    }
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // 에디터에서 종료
#else
        Application.Quit();   // 빌드된 실행 파일에서 종료
#endif
    }
}

public enum QuickChapter
{
    spring = (int)chapter.spring,    // 0
    spring2 = (int)chapter.spring2,    // 1
    spring3 = (int)chapter.spring3,   // 2
    spring4 = (int)chapter.spring4,   // 3
    summer = (int)chapter.summer,    // 4
    summer2 = (int)chapter.summer2,    // 5
    summer3 = (int)chapter.summer3,    // 6
    summer4 = (int)chapter.summer4,    // 7
    summer5 = (int)chapter.summer5,    // 8
    autumn = (int)chapter.autumn,    // 9
    autumn2 = (int)chapter.autumn2,    // 10    
    autumn3 = (int)chapter.autumn3,    // 11
    autumn4 = (int)chapter.autumn4,    // 12
    autumn6 = (int)chapter.autumn6,    // 13
    autumn8 = (int)chapter.autumn8,    // 14
    autumn9 = (int)chapter.autumn9,    // 15
    autumn10 = (int)chapter.autumn10,    // 16
    autumn11 = (int)chapter.autumn11,    // 17
    winter = (int)chapter.winter,    // 18
    winter2 = (int)chapter.winter2,    // 19
    winter3 = (int)chapter.winter3,    // 20
    winter4 = (int)chapter.winter4,    // 21
    space = (int)chapter.space,    // 22
}
