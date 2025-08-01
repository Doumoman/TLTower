using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    const string KEY_CHAPTER = "CurrentChapter"; // PlayerPrefs 키
    public static SaveSystem Instance { get; private set; }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SaveGame()
    {
        int idx = (int)ChapterManager.Instance.chapter;
        PlayerPrefs.SetInt(KEY_CHAPTER, idx);
        PlayerPrefs.Save();
        Debug.Log($"[SaveSystem] 챕터 저장: {ChapterManager.Instance.chapter}({idx})");
    }

    public void LoadGame()
    {
        chapter savedChapter = chapter.spring;
        if (PlayerPrefs.HasKey(KEY_CHAPTER))
        {
            int idx = PlayerPrefs.GetInt(KEY_CHAPTER);
            savedChapter = (chapter)idx;
        }

        ChapterManager.Instance.LoadChapter(savedChapter);
        Debug.Log($"[SaveSystem] 챕터 로드: {savedChapter}");
    }

    public void ResetGame()
    {
        PlayerPrefs.DeleteKey(KEY_CHAPTER);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] 데이터 리셋 → spring 로드");
    }

    public static void SetChapter(chapter ch)
    {
        PlayerPrefs.SetInt(KEY_CHAPTER, (int)ch);
        PlayerPrefs.Save();
        Debug.Log($"[SaveSystem] SetChapter → {ch} 저장 완료");

        // 같은 씬에 ChapterManager가 있으면 바로 적용
        if (ChapterManager.Instance)
            ChapterManager.Instance.LoadChapter(ch);
    }
    public static void SetChapter(QuickChapter quick)
    {
        SetChapter((chapter)quick);   // 캐스팅 후 재사용
    }
}