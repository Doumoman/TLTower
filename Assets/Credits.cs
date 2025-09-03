using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Credits : MonoBehaviour
{

    // Start is called before the first frame update
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private int nameSize = 30;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private int roleSize = 15;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float quickFadeDuration = 0.2f; // 터치 시 빠르게 넘기기.
    [SerializeField] private float waitDuration = 5f;
    [SerializeField] private KeyValuePair<string, string>[] credits = new KeyValuePair<string, string>[0];
    private int currentCreditIndex = 0;
    private bool fadingIn = false;
    private bool fadingOut = false;
    void Start()
    {
        nameText.text = "";
        nameText.fontSize = nameSize;
        roleText.text = "";
        roleText.fontSize = roleSize;
        currentCreditIndex = 0;
    }
    /*
    터치 없을 시 8초 주기로, 틱 시작 -> 1초 FI -> 5초 대기 -> 1초 FO
    */
    public void Show()
    {
        while (currentCreditIndex < credits.Length) //skip 시에도 늘어날 수 있음
        {
            StartCoroutine(ShowAndFade(credits[currentCreditIndex].Key));
            currentCreditIndex++;
        }
    }
    private IEnumerator ShowAndFade(string key)
    {
        TickManager.Instance.TickWait(1); // 틱에 맞춰서 실행
        nameText.text = key;
        roleText.text = credits[currentCreditIndex].Value;
        yield return FadeIn(fadeDuration, nameText);
        yield return FadeIn(fadeDuration, roleText); // t +1s
        yield return WaitCoroutine(waitDuration); // 5초간 띄우기 -> t +6s
        yield return FadeOut(fadeDuration, nameText); // t +7s
        yield return FadeOut(fadeDuration, roleText);
    }
    private IEnumerator WaitCoroutine(float seconds = -1f)
    {
        if (seconds == -1f)
            seconds = waitDuration;
        yield return new WaitForSeconds(seconds);
    }
    private IEnumerator FadeIn(float duration = -1f, TextMeshProUGUI text = null)
    {
        if (duration == -1f)
            duration = fadeDuration;
        Color originalColor = text.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1);
        float elapsed = 0;

        fadingIn = true;

        while (elapsed < duration)
        {
            text.color = Color.Lerp(originalColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.color = targetColor;
        fadingIn = false;
    }
    private IEnumerator FadeOut(float duration = -1f, TextMeshProUGUI text = null)
    {
        if (duration == -1f)
            duration = fadeDuration;
        Color originalColor = text.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        float elapsed = 0;

        fadingOut = true;

        while (elapsed < duration)
        {
            text.color = Color.Lerp(originalColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.color = targetColor;
        fadingOut = false;
    }

    /*
    Skip
    text가 없을 때 : QuickFadeIn 후 WaitCoroutine -> FadeOut
    FadeIn 중일 때 : StopCoroutine(FadeIn), 투명도 1로 즉시 전환, WaitCoroutine -> FadeOut 실시
    WaitCoroutine 중일 때 : StopAllCoroutines, QuickFadeOut, 다음 틱에서 그냥 FadeIn
    FadeOut 중일 때 : StopCoroutine(FadeOut), 투명도 0으로 즉시 전환, 다음 틱에서 그냥 FadeIn
    */


    public void Skip()
    {
        if (nameText.color == Color.clear)
        {
            StartCoroutine(QuickFadeIn(credits[currentCreditIndex].Key));
        }
        else if (nameText.color.a == 1f)
        {
            StopAllCoroutines();
            StartCoroutine(QuickFadeOut());
        }
        else if (fadingIn)
        {
            StopAllCoroutines();
            Color c = nameText.color;
            c.a = 1f;
            nameText.color = c;
            c = roleText.color;
            c.a = 1f;
            roleText.color = c;
            StartCoroutine(WaitCoroutine());
            StartCoroutine(FadeOut(fadeDuration, nameText));
            StartCoroutine(FadeOut(fadeDuration, roleText));
        }
        else if (fadingOut)
        {
            StopAllCoroutines();
            Color c = nameText.color;
            c.a = 0f;
            nameText.color = c;
            c = roleText.color;
            c.a = 0f;
            roleText.color = c;
        }
    }
    private IEnumerator QuickFadeIn(string key)
    {
        nameText.text = key;
        roleText.text = credits[currentCreditIndex].Value;
        yield return StartCoroutine(FadeIn(quickFadeDuration, nameText));
        yield return StartCoroutine(FadeIn(quickFadeDuration, roleText));
        yield return StartCoroutine(WaitCoroutine());
        yield return StartCoroutine(FadeOut(fadeDuration, nameText));
        yield return StartCoroutine(FadeOut(fadeDuration, roleText));
    }
    private IEnumerator QuickFadeOut()
    {
        yield return StartCoroutine(FadeOut(quickFadeDuration, nameText));
        yield return StartCoroutine(FadeOut(quickFadeDuration, roleText));
    }
}
