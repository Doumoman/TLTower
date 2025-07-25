using UnityEngine;

public class GuidePanelRoot : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private GuidePanel guidePanel;
    string key;
    bool isButtonGuide = false;
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
        isButtonGuide = true;
        Debug.Log("ButtonGuide");
        gameObject.SetActive(true);
        guidePanel.ButtonGuide();
        bool open = gameObject.activeSelf;
        Time.timeScale = open ? 1f : 0f;
        SoundManager.Instance.PlaySFX("pause");
        isButtonGuide = false;
    }
}
