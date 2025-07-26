using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
}
public class GuidePanel : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private PanelFader panelFader;
    [SerializeField] private List<ImageDict> guideImages;
    [SerializeField] private float fadeTime = 0.5f; //가이드 켰을 때 FadeIn 시간
    [SerializeField] private float lockTime = 2f; //오작동을 막기 위함

    [Header("Buttons")]
    [SerializeField] private GameObject exitButton;
    [SerializeField] private GameObject prevButton, nextButton, returnButton;
    [SerializeField] private Sprite defaultImage; //list에 아무 것도 없을 때 사용할 기본 이미지

    [Header("misc")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject blackPanel;
    [SerializeField] private GameObject root;
    private Image displayImage;
    private const string KeyName = "GuideKeys";
    private List<Sprite> guideBuffer = new();
    private int guideBufferIdx = 0;
    void Awake()
    {
        if (!displayImage) displayImage = GetComponent<Image>();

        Clear();
        SetImage(defaultImage);  //테스트용! 출시하기 전에 PlayerPrefs를 비우고 이 줄은 지울것!!!!!
        Load();
        currentIdx = 0;

        prevButton.SetActive(false);
        nextButton.SetActive(false);
        exitButton.SetActive(false);
        returnButton.SetActive(false);
    }

    void OnEnable()
    {
        SoundManager.Instance.PauseBGM();
        canExit = false;
        Invoke(nameof(EnableExit), lockTime);
    }
    void OnDisable()
    {
        guideBuffer.Clear();
    }
    void Update()
    {
        if (pausePanel.activeInHierarchy) blackPanel.SetActive(false);
        else blackPanel.SetActive(true);
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
            SetImage(Current());
        }
        else
        {
            prevButton.SetActive(true);
            nextButton.SetActive(true);
            SetImage(Current());
        }

        //FadeIn
        //StartCoroutine(panelFader.FadeIn(fadeTime));
        UpdateButtonsForButtonGuide(currentIdx, imageList.Count);
    }
    public void PlayGuide(string key) // 가이드가 자동으로 나와야 할 때 : 해당하는 가이드 호출 후 저장
    {
        //guideImages에서 key로 오브젝트를 찾기
        var sprite = guideImages.Find(s => s.key == key);
        if (sprite == null || sprite.sprite == null || sprite.sprite.Count == 0)
        {
            Debug.LogWarning("가이드가 맛탱이가 갔어!");
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
        }
        SoundManager.Instance.PlaySFX("stamp_button");
    }
    private bool canExit = false;
    private void EnableExit()
    {
        canExit = true;

        // 현재 상황에 맞춰 exit 버튼을 업데이트
        if (guideBuffer.Count > 0)
            UpdateButtons(guideBufferIdx, guideBuffer.Count);
        else
            UpdateButtons(currentIdx, imageList.Count);
    }
    public void ExitButton()
    {
        if (!canExit) return;
        returnButton.SetActive(false);
        root.SetActive(false);
        SoundManager.Instance.PlaySFX("stamp_button");
        SoundManager.Instance.Resume();
    }
    public void ReturnButton()
    {
        if (!canExit) return;
        returnButton.SetActive(false);
        root.SetActive(false);
        SoundManager.Instance.PlaySFX("stamp_button");
    }

    private void UpdateButtons(int idx, int count)
    {
        bool canGoPrev = idx > 0;
        bool canGoNext = idx < count - 1;

        prevButton.SetActive(canGoPrev);
        nextButton.SetActive(canGoNext);

        // Exit은 마지막 요소에서만 활성화 + lockTime 체크
        exitButton.SetActive(!canGoNext && canExit);
    }
    private void UpdateButtonsForButtonGuide(int idx, int count)
    {
        bool canGoPrev = idx > 0;
        bool canGoNext = idx < count - 1;

        prevButton.SetActive(canGoPrev);
        nextButton.SetActive(canGoNext);

        exitButton.SetActive(false);
        returnButton.SetActive(true);
    }
    /*===================세이브/로드 기능======================*/
    public void Save()
    {
        List<string> keys = new();
        foreach (var sprite in imageList)
            foreach (var spr in guideImages)
                foreach (var sp in spr.sprite)
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
            var spr = guideImages.Find(s => s.key == key);
            if (spr != null)
                foreach (var sprite in spr.sprite)
                    if (!imageList.Contains(sprite))
                        imageList.Add(sprite);
        }
    }

    public void Clear()
    {
        PlayerPrefs.SetString(KeyName, "");
        Debug.LogWarning("GuidePanel Buffer Cleard. Clear는 테스트용이므로 출시 전에 지우기!");
    }

    /*===================circular linked list imageList 구현======================*/
    private List<Sprite> imageList = new();
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