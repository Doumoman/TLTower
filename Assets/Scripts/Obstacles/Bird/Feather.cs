using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Feather : MonoBehaviour
{
    public float duration = 5f;
    public float fadeOutSpeed = 0.1f;
    float lifeSpan = 7f;

    Coroutine co = null;

    private void Start()
    {
        co = StartCoroutine(Life());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.transform.CompareTag("PlacedStone")) return;
        //transform.parent.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        GetComponent<Collider2D>().isTrigger = false;

        if (TryGetComponent<Animator>(out Animator animator)) Destroy(animator);
        if (co != null) StopCoroutine(co);
        co = null;
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(duration); //잠깐 기다렸다가

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color color = sr.color;

        while (color.a > 0f)
        {
            color.a -= Time.deltaTime * fadeOutSpeed;
            color.a = Mathf.Max(0f, color.a);
            sr.color = color;

            yield return null;
        }

        Destroy(transform.parent.gameObject);
    }

    IEnumerator Life()
    {
        yield return new WaitForSeconds(duration);
        StartCoroutine(FadeOut());
    }
}
