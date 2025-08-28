using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class RiseStoneAndSceneChange : MonoBehaviour
{
    /* ────────── 움직일 대상 ────────── */
    [Header("▼ 움직일 뭉치들(여러 개 가능)")]
    [Tooltip("천천히 → 가속해 올라갈 오브젝트들")]
    public List<Transform> chunks = new();   // Inspector에 여러 개 등록

    /* ────────── 1단계 : 느린 상승 ────────── */
    [Header("▼ 1단계 : 느린 상승")]
    public float slowMoveDistance = 1f;
    public float slowMoveTime = 4f;

    /* ────────── 2단계 : 가속 상승 ────────── */
    [Header("▼ 2단계 : 가속 상승")]
    public float fastMoveDistance = 6f;
    public float fastMoveTime = 1.5f;

    /* ────────── 페이드 아웃 ────────── */
    [Header("▼ 페이드 아웃(검은 화면)")]
    public SpriteRenderer blackout;  // 알파 0 → 1
    public float fadeDuration = 1f;
    public float blackHoldTime = 1f;

    /* ────────── 씬 전환 ────────── */
    [Header("▼ 씬 전환")]
    public string nextScene = "SpaceAnimation";

    Coroutine routine;

    void Awake()
    {
        /* 리스트가 비어 있으면 자기 자신을 기본으로 추가 */
        if (chunks.Count == 0) chunks.Add(transform);

        if (!blackout)
            Debug.LogWarning("[ChunkLiftAndFade] blackout 스프라이트가 지정되지 않았습니다!");
    }

    public void Run() => routine = StartCoroutine(PlaySequence());
    void OnDisable() { if (routine != null) StopCoroutine(routine); }

    /* ────────── 핵심 시퀀스 ────────── */
    IEnumerator PlaySequence()
    {
        SoundManager.Instance.PlaySFX("buddha_rise");
        /* 1단계 ─ 느린 상승 */
        yield return MoveChunksBy(slowMoveDistance, slowMoveTime,
                                  AnimationCurve.EaseInOut(0, 0, 1, 1));

        /* 2단계 ─ 가속 상승 */
        yield return MoveChunksBy(fastMoveDistance, fastMoveTime,
                                  AnimationCurve.EaseInOut(0, 0, 1, 1.2f));

        /* 페이드 아웃 */
        if (blackout)
            yield return FadeToBlack();

        yield return new WaitForSeconds(blackHoldTime);

        /* 씬 교체 */
        SceneManager.LoadScene(nextScene);
    }

    /* ────────── 유틸 함수 ────────── */
    IEnumerator MoveChunksBy(float deltaY, float totalTime, AnimationCurve curve)
    {
        var starts = new Vector3[chunks.Count];
        var ends = new Vector3[chunks.Count];

        for (int i = 0; i < chunks.Count; ++i)
        {
            starts[i] = chunks[i].position;
            ends[i] = starts[i] + Vector3.up * deltaY;
        }

        for (float t = 0; t < totalTime; t += Time.deltaTime)
        {
            float p = curve.Evaluate(t / totalTime);
            for (int i = 0; i < chunks.Count; ++i)
                chunks[i].position = Vector3.LerpUnclamped(starts[i], ends[i], p);

            yield return null;
        }
        /* 끝값 보정 */
        for (int i = 0; i < chunks.Count; ++i)
            chunks[i].position = ends[i];
    }

    IEnumerator FadeToBlack()
    {
        Color c = blackout.color;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            c.a = t / fadeDuration;
            blackout.color = c;
            yield return null;
        }
        c.a = 1f;
        blackout.color = c;
    }
}