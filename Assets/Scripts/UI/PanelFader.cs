using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelFader : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] float fadeTime = 0.4f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    void OnEnable()
    {
        canvasGroup.alpha = 1f;
    }
    public IEnumerator FadeIn(float fadeTime)
    {
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
