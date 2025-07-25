using UnityEngine;

public class GuidePanelRoot : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GuidePanel guidePanel;
    string key;
    void Awake()
    {
    }
    public void PlayGuide(string str)
    {
        gameObject.SetActive(false);
        key = str;
        gameObject.SetActive(true);
        Debug.Log($"GuidePanel is {gameObject.activeInHierarchy}");
        Time.timeScale = 0f;
        guidePanel.PlayGuide(str);
    }
    void OnDisable()
    {
        key = "";
        Time.timeScale = 1f;
    }
    public void ButtonGuide()
    {
        Debug.Log("ButtonGuide");
        gameObject.SetActive(true);
        guidePanel.ButtonGuide();
        Time.timeScale = 0f;
        SoundManager.Instance.PlaySFX("stamp_button");
    }
}
