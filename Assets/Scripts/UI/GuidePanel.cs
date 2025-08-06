using System.Collections;
using System.Collections.Generic;
using FMOD;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;

// 여기서는 띄워야 할 이미지의 데이터 를! 관리.

/*
- Dictionary로 이름(key)와 이미지 object를 저장, Inspector 입력용
- PlayerPrefs에 패널에 띄울 이미지의 key 순서를 저장, 불러오기 시 불러옴
- 불러온 이미지를 Circular Linked List에 불러와서 저장, 버튼을 불러올 때마다 로드?
*/
[System.Serializable]
public class ImageDict
{
    public string key;
    public List<Sprite> sprite;
    public Sprite bookmark; 
}
public class GuidePanel : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private PanelFader panelFader;
    [SerializeField] private List<ImageDict> guideImages;
    [SerializeField] private float fadeTime = 0.5f; //가이드 켰을 때 FadeIn 시간
    [SerializeField] private float lockTime = 12f; //오작동을 막기 위함

    [Header("Buttons")]
    [SerializeField] private GameObject exitButton;
    [SerializeField] private GameObject prevButton, nextButton, returnButton;
    [SerializeField] private Sprite defaultImage; //list에 아무 것도 없을 때 사용할 기본 이미지

    [Header("misc")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject root;
    [HideInInspector] public UnityEvent GuideClosed = new();
    private Image displayImage;
    private const string KeyName = "GuideKeys";
    private List<Sprite> guideBuffer = new();
    private int guideBufferIdx = 0;
    void Awake()
    {
        if (!displayImage) displayImage = gameObject.GetComponent<Image>();

        Clear();
        SetImage(defaultImage);  //테스트용! 출시하기 전에 PlayerPrefs를 비우고 이 줄은 지울것!!!!!
        Load();
        currentIdx = 0;

        prevButton.SetActive(false);
        nextButton.SetActive(false);
        exitButton.SetActive(false);
        returnButton.SetActive(false);
    }

    void Start()
    {
        ClearBookmarks();
        SetBookmark();
        DisableBookmark();
    }

    void OnDisable()
    {
        guideBuffer.Clear();
        DisableBookmark();
    }
    void Update()
    {
        if(gameObject.activeSelf
        && Input.GetKeyDown(KeyCode.Escape)
        && (exitButton.activeSelf|| returnButton.activeSelf))
        {
            // 가이드 패널이 열려있고, Exit 또는 Return 버튼이 활성화되어 있다면
            if (canExit) ExitButton(); // Exit 버튼을 누른다
            else ReturnButton(); // Return 버튼을 누른다
        }
    }

    public void ButtonGuide() //일시정지 패널에서 가이드 버튼을 누를 때 : 지금까지 봤던 모든 가이드 호출
    {
        canExit = true;
        //가이드가 여러 개라면 Prev, Next 버튼 활성화
        //없다면 default 이미지 활성화
        if (imageList.Count == 0) SetImage(defaultImage);
        else if (imageList.Count == 1)
        {
            prevButton.SetActive(false);
            nextButton.SetActive(false);
            SetImage(imageList[0]);
        }
        else
        {
            prevButton.SetActive(true);
            nextButton.SetActive(true);
            SetImage(imageList[0]);
        }

        ResetBookMark();
        EnableBookmark(); // 북마크 활성화

        //FadeIn
        //StartCoroutine(panelFader.FadeIn(fadeTime));
    }
    public void PlayGuide(string key) // 가이드가 자동으로 나와야 할 때 : 해당하는 가이드 호출 후 저장
    {
        DisableBookmark(); // 북마크 비활성화
        canExit = false;
        StartCoroutine(EnableExit(lockTime));

        //guideImages에서 key로 오브젝트를 찾기
        var sprite = guideImages.Find(s => s.key == key);
        if (sprite == null || sprite.sprite == null || sprite.sprite.Count == 0)
        {
            UnityEngine.Debug.LogWarning("가이드가 맛탱이가 갔어!");
            return;
        }

        //guideBuffer에 순서대로 추가
        guideBuffer.AddRange(sprite.sprite);
        guideBufferIdx = 0;

        //가이드가 하나뿐이라면 Prev, Next 버튼 비활성화
        bool multi = guideBuffer.Count > 1;
        prevButton.SetActive(multi);
        nextButton.SetActive(multi);

        //첫 이미지 표시
        SetImage(guideBuffer[guideBufferIdx]);
        //StartCoroutine(panelFader.FadeIn(fadeTime));

        //해당 key가 없다면 imageList와 PlayerPrefs에 저장
        foreach (var s in sprite.sprite)
            if (!imageList.Contains(s))
                imageList.Add(s);

        ResetBookMark();
        UpdateButtons(guideBufferIdx, guideBuffer.Count);
        Save();
    }
    private void SetImage(Sprite newImage)
    {
        displayImage.sprite = newImage;
    }
    /*===================버튼 기능======================*/
    public void PrevButton()
    {
        if (guideBuffer.Count > 0)
        {
            guideBufferIdx--;
            SetImage(guideBuffer[guideBufferIdx]);
            UpdateButtons(guideBufferIdx, guideBuffer.Count);
        }
        else if (imageList.Count > 0)
        {
            currentIdx--;
            SetImage(imageList[currentIdx]);
            UpdateButtonsForButtonGuide(currentIdx, imageList.Count);
            UpdateBookmarkVisual();
        }
        SoundManager.Instance.PlaySFX("stamp_button");
    }

    public void NextButton()
    {
        if (guideBuffer.Count > 0)
        {
            guideBufferIdx++;
            SetImage(guideBuffer[guideBufferIdx]);
            UpdateButtons(guideBufferIdx, guideBuffer.Count);
        }
        else if (imageList.Count > 0)
        {
            currentIdx++;
            SetImage(imageList[currentIdx]);
            UpdateButtonsForButtonGuide(currentIdx, imageList.Count);
            UpdateBookmarkVisual();
        }
        SoundManager.Instance.PlaySFX("stamp_button");
    }
    private bool canExit = false;
    private IEnumerator EnableExit(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        canExit = true;
        // 현재 상황에 맞춰 exit 버튼을 업데이트
        if (guideBuffer.Count > 0)
            UpdateButtons(guideBufferIdx, guideBuffer.Count);
        else
            UpdateButtonsForButtonGuide(currentIdx, imageList.Count);
    }
    public void ExitButton()
    {
        if (!canExit) return;
        SoundManager.Instance.PlaySFX("stamp_button");
        returnButton.SetActive(false);
        root.SetActive(false);
        UnityEngine.Debug.Log($"ExitButton Called! Current state == {GuideManager.Instance.current}, forceStop == {VoiceManager.Instance.forceStop}");

        GuideClosed.Invoke();
    }
    public void ReturnButton()
    {
        SoundManager.Instance.PlaySFX("stamp_button");
        returnButton.SetActive(false);
        root.SetActive(false);

        GuideClosed.Invoke();
    }

    private void UpdateButtons(int idx, int count)
    {
        bool canGoPrev = idx > 0;
        bool canGoNext = idx < count - 1;

        prevButton.SetActive(canGoPrev);
        nextButton.SetActive(canGoNext);

        // Exit은 마지막 요소에서만 활성화 + lockTime 체크
        exitButton.SetActive(!canGoNext && canExit);
        returnButton.SetActive(false);
    }
    private void UpdateButtonsForButtonGuide(int idx, int count)
    {
        bool canGoPrev = idx > 0;
        bool canGoNext = idx < count - 1;

        prevButton.SetActive(canGoPrev);
        nextButton.SetActive(canGoNext);

        exitButton.SetActive(false);
        returnButton.SetActive(true);

        UnityEngine.Debug.Log($"UpdateButtonsForButtonGuide: idx={idx}, count={count}, canGoPrev={canGoPrev}, canGoNext={canGoNext}");
    }

    /*===================북마크 기능==================*/

    private List<GameObject> bookmarkObj = new();
    private List<GameObject> bookmarkListeners = new();
    public void SetBookmark()
    {
        bookmarkObj.Clear();
        bookmarkListeners.Clear();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform) continue; // 자기 자신은 제외
            if (child.CompareTag("bookmark"))
            {
                GameObject go = child.gameObject;
                bookmarkObj.Add(go);
            }
        }
        bookmarkObj.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x)); // x좌표 기준으로 정렬

        for (int i = 0; i < bookmarkObj.Count; i++)
        {
            GameObject go = bookmarkObj[i];

            // Image
            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                img = go.AddComponent<Image>();
                img.preserveAspect = true;
            }

            // Button + 리스너는 한 번만
            if (!bookmarkListeners.Contains(go))
            {
                Button btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
            }
        }
    }

    public void ResetBookMark()
    {
        Load();
        HashSet<string> uniqueKeys = new();
        int bookmarkIdx = 0;

        for (int i = 0; i < imageList.Count; i++)
        {
            Sprite sprite = imageList[i];
            string key = GetKeyForSprite(sprite);
            UnityEngine.Debug.Log($"Bookmark: {key} for sprite {sprite.name}");

            if (!uniqueKeys.Contains(key) && bookmarkIdx < Mathf.Min(bookmarkObj.Count, bookmarks.Count))
            {
                uniqueKeys.Add(key);

                GameObject bm = bookmarkObj[bookmarkIdx];
                if (bm == null) continue;

                //이미지 설정
                Image img = bm.GetComponent<Image>();
                if (img == null) img = bm.AddComponent<Image>();
                img.sprite = bookmarks[bookmarkIdx];
                img.preserveAspect = true;

                //버튼 리스너 설정
                Button btn = bm.GetComponent<Button>();
                if (btn == null) btn = bm.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();

                int imageIdx = i;
                btn.onClick.AddListener(() => ButtonClicked(imageIdx));

                bookmarkIdx++;
            }
        }
        
        UpdateBookmarkVisual();
    }

    private void ButtonClicked(int idx)
    {
        currentIdx = idx;
        SetImage(imageList[currentIdx]);
        UpdateButtonsForButtonGuide(currentIdx, imageList.Count);
        UpdateBookmarkVisual();
        SoundManager.Instance.PlaySFX("stamp_button");
    }

    private string GetKeyForSprite(Sprite sprite)
    {
        foreach (var imgDict in guideImages)
        {
            if (imgDict.sprite.Contains(sprite))
            {
                return imgDict.key;
            }
        }
        return null; // 해당하는 키가 없을 경우
    }

    public void UpdateBookmarkVisual()
    {
        for (int i = 0; i < bookmarkObj.Count; i++)
        {
            if (i >= bookmarks.Count) break;

            GameObject go = bookmarkObj[i];
            Image img = go.GetComponent<Image>();
            if (img == null) continue;

            string key = GetKeyForBookmark(bookmarks[i]);
            Sprite current = Current();
            bool isActive = guideImages.Find(g => g.key == key)?.sprite[0] == current;

            // 시각 효과 적용
            img.color = isActive ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.5f);
            go.transform.localScale = isActive ? Vector3.one * 0.9f : Vector3.one;
            if (go.activeInHierarchy == true)
                go.transform.SetAsFirstSibling();
            else
                go.transform.SetAsLastSibling();
        }
    }

    private string GetKeyForBookmark(Sprite sprite)
    {
        foreach (var imgDict in guideImages)
        {
            if (imgDict.bookmark == sprite)
            {
                return imgDict.key;
            }
        }
        return null; // 해당하는 키가 없을 경우
    }

    public void ClearBookmarks()
    {
        foreach (GameObject bookmark in bookmarkObj)
        {
            Destroy(bookmark);
        }
        bookmarkObj.Clear();
    }

    public void EnableBookmark()
    {
        if (bookmarkObj.Count == 0) return;

        foreach (GameObject bookmark in bookmarkObj)
        {
            if (bookmark.GetComponent<Image>().sprite != null)
                bookmark.SetActive(true);
        }
        UpdateButtonsForButtonGuide(currentIdx, imageList.Count);
    }
    public void DisableBookmark()
    {
        UnityEngine.Debug.Log("DisableBookmark called");
        foreach (GameObject bookmark in bookmarkObj)
        {
            bookmark.SetActive(false);
        }
    }
    /*===================세이브/로드 기능======================*/
    public void Save()
    {
        List<string> keys = new();
        foreach (Sprite sprite in imageList)
            foreach (ImageDict spr in guideImages)
                foreach (Sprite sp in spr.sprite)
                    if (sp == sprite)
                    {
                        keys.Add(spr.key);
                        break;
                    } // 4단루프가 더티해 보이지만 실제로 도는 횟수는 sprite 수만큼임 ㅋㅋ;;
        string joined = string.Join(",", keys);
        PlayerPrefs.SetString(KeyName, joined);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        string saved = PlayerPrefs.GetString(KeyName, "");
        if (string.IsNullOrEmpty(saved)) return;

        string[] keys = saved.Split(',');

        foreach (string key in keys)
        {
            ImageDict spr = guideImages.Find(s => s.key == key);
            if (spr.sprite != null)
                foreach (Sprite sprite in spr.sprite)
                    if (!imageList.Contains(sprite))
                        imageList.Add(sprite);
            if (!bookmarks.Contains(spr.bookmark))
                bookmarks.Add(spr.bookmark);
        }
    }

    public void Clear()
    {
        PlayerPrefs.SetString(KeyName, "");
        UnityEngine.Debug.LogWarning("GuidePanel Buffer Cleard. Clear는 테스트용이므로 출시 전에 지우기!");
    }

    /*===================circular linked list imageList 구현======================*/
    private List<Sprite> imageList = new();
    private List<Sprite> bookmarks = new();
    private int currentIdx = 0;

    private Sprite Current()
    {
        if (imageList.Count == 0) return null;
        return imageList[currentIdx];
    }

    /*
    private Sprite Next()
    {
        if (imageList.Count == 0) return null;
        currentIdx = (currentIdx + 1) % imageList.Count;
        return imageList[currentIdx];
    }

    private Sprite Prev()
    {
        currentIdx = (currentIdx - 1 + imageList.Count) % imageList.Count;
        return imageList[currentIdx];
    }
    */
}