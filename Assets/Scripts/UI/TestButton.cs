using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestButton : MonoBehaviour
{
    [SerializeField] bool createChapterClearButtons;

    private void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!createChapterClearButtons) return;

        CreateChapterClearButton("Spring Clear", chapter.spring4, 1);
        CreateChapterClearButton("Summer Clear", chapter.summer5, 2);
        CreateChapterClearButton("Autumn Clear", chapter.autumn11, 3);
        CreateChapterClearButton("Winter Clear", chapter.winter4, 4);
#endif
    }

    private void CreateChapterClearButton(string label, chapter finalStage, int row)
    {
        GameObject buttonObject = Instantiate(gameObject, transform.parent, false);
        buttonObject.name = $"Test {label}";

        TestButton testButton = buttonObject.GetComponent<TestButton>();
        testButton.createChapterClearButtons = false;

        RectTransform sourceRect = (RectTransform)transform;
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        float spacing = sourceRect.sizeDelta.y * Mathf.Abs(sourceRect.localScale.y) + 12f;
        buttonRect.anchoredPosition = sourceRect.anchoredPosition + Vector2.down * spacing * row;

        TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (buttonText)
        {
            buttonText.text = label;
            buttonText.enableAutoSizing = true;
            buttonText.fontSizeMin = 18f;
            buttonText.fontSizeMax = 30f;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => ChapterManager.Instance?.TestClearFromFinalStage(finalStage));
    }

    public void Play()
    {
        AnimationManager.Instance.Play1();
    }
    public void PlayStA()
    {
        AnimationManager.Instance.PlaySummertoAutumn();
    }
    public void Lower()
    {
        CameraController.Instance.RaiseCameraY();
    }
}
