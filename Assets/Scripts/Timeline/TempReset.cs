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
}
