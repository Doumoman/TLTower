using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class ResetStone : MonoBehaviour
{
    public static ResetStone Instance { get; private set; }
    public GameObject platform;

    StoneFixer sf;
    int currentWave = 0;
    StoneController sc = null;
    ChapterManager cm;
    GameObject lastPlatform;
    private void Awake()
    {
        cm = ChapterManager.Instance;
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        cm = ChapterManager.Instance;
        sf = StoneFixer.Instance;


    }
    public void DestroyStones()
    {
        SoundManager.Instance.PlaySFX("reset_button");
        BosalManager.Instance.Speak("PushReset");
        foreach (var s in sf.GetBatch().ToList())
        {
            sf.NotifyStoneLost(s);
            Destroy(s.gameObject);
        }

        if (sc != null)
        {
            Vector2 highPos = new Vector2(0, sc.transform.position.y);
            sf.SetY(highPos.y);
            if (currentWave > 0)
            {
                CreatePlatform();
            }
        }
        StartCoroutine(FocusCameraToPlatform(1f, 0.4f));
    }
    private float GetPlatformTopY(GameObject go)
    {
        if (!go) return 0f;

        float top = go.transform.position.y;

        // 1) Collider2D 우선
        var cols = go.GetComponentsInChildren<Collider2D>();
        foreach (var c in cols) top = Mathf.Max(top, c.bounds.max.y);
        if (cols.Length > 0) return top;
        Debug.Log(top);
        // 2) SpriteRenderer 보조
        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs) top = Mathf.Max(top, sr.bounds.max.y);

        return top;
    }

    // 플랫폼 기준으로 카메라 이동(부드럽게)
    private IEnumerator FocusCameraToPlatform(float extra = 1f, float duration = 0.4f)
    {
        // 같은 프레임에 CreatePlatform/SpawnPlatformAt이 호출될 수 있으니 한 프레임 양보
        yield return null;

        if (CameraController.Instance == null) yield break;

        float topY = GetPlatformTopY(lastPlatform);
        float targetY = topY + extra;

        // 카메라 상한에 걸리면 상한까지만 이동 (외부 수정 없이 안전)
        float limit = CameraController.Instance.CurrentTopLimit;
        if (targetY > limit) targetY = limit;
        CameraController.Instance.CenterOnY(targetY, duration);
        Debug.Log($"[ResetStone] Camera -> PlatformTop+{extra} (targetY={targetY:F2})");
    }

    //초기화시 위치 기준이 되는 돌의 stonecontroller를 얻음
    public void GetSc(StoneController s)
    {
        sc = s;
        currentWave = sf.GetWave();
    }
    public void ColToSc(Collider2D col)
    {
        sc = col.GetComponent<StoneController>();
        currentWave = sf.GetWave();
    }
    void SpawnPlatformAt(Vector3 pos)
    {
        // 기존 플랫폼이 있으면 없애기
        if (lastPlatform != null) Destroy(lastPlatform);

        // 새 플랫폼 생성 & 기록
        lastPlatform = Instantiate(platform, pos, Quaternion.identity);

        /* ───── space 챕터면 외형 변경 ───── */
        if (ChapterManager.Instance && ChapterManager.Instance.chapter == chapter.space)
        {
            // 전체 스케일 1.21배
            lastPlatform.transform.localScale = Vector3.one * 1.21f;

            // 본체 색상 #676767
            if (lastPlatform.TryGetComponent(out SpriteRenderer bodySr))
                bodySr.color = new Color32(0x67, 0x67, 0x67, 0xFF);

            // 자식 “checkpoint_hand” 색상 #CFCFCF
            var hand = lastPlatform.transform.Find("platform");
            if (hand && hand.TryGetComponent(out SpriteRenderer handSr))
                handSr.color = new Color32(0xCF, 0xCF, 0xCF, 0xFF);
        }
        /* ──────────────────────────────── */
    }

    public void CreatePlatform()
    {

        float y = StoneFixer.Instance.HighestFixedY;
        Vector3 pos = new Vector3(0f, y, 0f);

        currentWave = -1;
        SpawnPlatformAt(pos);
    }
    public void CreatePlatform(Vector2 spawnPos)
    {
        Vector3 pos = new Vector3(spawnPos.x, spawnPos.y, -8f);
        SpawnPlatformAt(pos);
    }

}
