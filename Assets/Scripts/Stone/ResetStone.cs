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
    }
    private IEnumerator FocusCameraNextFrame(float offsetY = 2f, float duration = 0.35f)
    {
        // 플랫폼 트랜스폼들이 세팅될 때까지 한 프레임 양보
        yield return null; // 또는 new WaitForEndOfFrame();

        if (lastPlatform && CameraController.Instance)
        {
            float targetY = lastPlatform.transform.position.y + offsetY;
            CameraController.Instance.CenterOnY(targetY, duration);
        }
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
