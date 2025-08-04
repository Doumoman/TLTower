using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Tree : MonoBehaviour
{
    
    SpriteRenderer sr;         // Tree 오브젝트의 SpriteRenderer
    Transform childTransform;  // 'Child'라는 이름의 자식 Transform

    [Header("References")]
    public GameObject[] stem;
    public Sprite[] spring;
    public Sprite[] springFlowerFront;
    public Sprite[] summer;
    public Sprite[] summerFlowerFront;
    public Sprite[] autumn;
    public Sprite[] winter;
    public Sprite[] winterFlowerFront;

    [Header("Fade-out Settings")]
    [SerializeField] float fadeDuration = 1f;
    static readonly List<Tree> _allTrees = new();   // 씬의 모든 Tree 인스턴스
    static bool _globalFadeStarted;
    bool _registered;

    float Z;
    Dictionary<chapter, Sprite[]> seasons;
    Dictionary<chapter, Sprite[]> seasonFlower;
    Sprite[] currentSp;
    Sprite[] currentFlowerSp;
    GameObject lastStem;
    ChapterManager cm;
    void Awake()
    {   
        // 빌드 테스트용용
        Debug.Log($"[Tree] spriteRenderer = {cm}");
        Debug.Log($"[Tree] someChild    = {seasons}");
        // ChapterManager는 싱글톤으로 접근
        cm = ChapterManager.Instance;
        if (cm == null)
            Debug.LogError("[Tree] ChapterManager 인스턴스를 찾을 수 없습니다.");

        // 2) 자식 트랜스폼 자동 할당 (선택적)
        childTransform = transform.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(t => t.name.Contains("Child"));
        if (childTransform == null)
            Debug.LogWarning("[Tree] 'Child' 이름의 자식이 없습니다. (선택적)");

        // 3) SpriteRenderer는 자식에서 찾기 (Tree 자체에 없을 수 있음)
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
            Debug.LogWarning("[Tree] SpriteRenderer 컴포넌트를 찾을 수 없습니다. (자식에서 찾음)");

        // 자신을 정적 리스트에 등록
        _allTrees.Add(this);
        _registered = true;

        Z = gameObject.transform.position.z;

        seasons = new Dictionary<chapter, Sprite[]>();
        seasonFlower = new Dictionary<chapter, Sprite[]>();
        chapter[] c = (chapter[])System.Enum.GetValues(typeof(chapter));

        foreach (chapter ch in c)
        {
            //문자열로 챕터 확인!
            string s = ch.ToString();
            if (s.Contains("land"))
            {
                seasons.Add(ch, spring);
                seasonFlower.Add(ch, springFlowerFront);
            }
            else if (s.Contains("spring"))
            {
                seasons.Add(ch, spring);
                seasonFlower.Add(ch, springFlowerFront);
            }
            else if (s.Contains("summer"))
            {
                seasons.Add(ch, summer);
                seasonFlower.Add(ch, summerFlowerFront);
            }
            else if (s.Contains("autumn"))
            {
                seasons.Add(ch, autumn);
                seasonFlower.Add(ch, springFlowerFront);
            }
            else if (s.Contains("winter"))
            {
                seasons.Add(ch, winter);
                seasonFlower.Add(ch, winterFlowerFront);
            }
            else if (s.Contains("space"))
            {
                seasons.Add(ch, winter);
                seasonFlower.Add(ch, winterFlowerFront);
            }
        }
        currentSp = spring;
        currentFlowerSp = springFlowerFront;

        if (!lastStem)
        {
            lastStem = Instantiate(stem[0], gameObject.transform);
            lastStem.transform.position = new Vector3(0, -7, Z);
        }
    }

    void Start()
    {   
        if (cm != null)
        {
            cm.onChapterChage += AutumnDisable;
            cm.onChapterChage += ChageSprite;
        }
        else
        {
            Debug.LogError("tree 챕터변환 등록 실패 - ChapterManager가 null입니다.");
        }

        AutumnDisable(this, EventArgs.Empty);
        ChageSprite(this, EventArgs.Empty);               // ← 추가

        StartCoroutine(RegisterWhenReady());
    }

    IEnumerator RegisterWhenReady()
    {
        yield return new WaitUntil(() => ChapterManager.Instance != null);
        cm = ChapterManager.Instance;
        cm.onChapterChage += AutumnDisable;
        cm.onChapterChage += ChageSprite;
        Debug.Log("tree 챕터변환 등록 완료");
    }
    void OnDestroy()
    {
        if (_registered) _allTrees.Remove(this);
        
        // 이벤트 해제
        if (cm != null)
        {
            cm.onChapterChage -= AutumnDisable;
            cm.onChapterChage -= ChageSprite;
        }
    }

    //나무를 계속 생성(space에선 생성x)
    void Update()
    {
        // ChapterManager가 null이면 업데이트를 건너뜀
        if (cm == null)
        {
            // ChapterManager를 다시 찾아보기
            cm = ChapterManager.Instance;
            if (cm == null) return;
        }

        if (cm.chapter == chapter.space && !_globalFadeStarted)
        {
            _globalFadeStarted = true;
            StartCoroutine(CoFadeAndDisableAll());
            return;              // 이후 로직은 더 이상 필요 없음
        }
        if (_globalFadeStarted) return;

        float lastY = lastStem.transform.position.y;
        if ((StoneFixer.Instance && StoneFixer.Instance.HighestSettledY > lastY) || (CloudSystem.Instance && CloudSystem.Instance.HighestJointY > lastY))
        {
            int idx = UnityEngine.Random.Range(0, spring.Length-1);
            lastStem = Instantiate(stem[idx], gameObject.transform);
            lastStem.transform.position = new Vector3(0, lastY + 21.6f, Z);
            lastStem.GetComponent<SpriteRenderer>().sprite = seasons[cm.chapter][idx];

            if (cm.chapter.ToString().Contains("autumn"))
            {
                for (int j = 0; j < lastStem.transform.childCount; j++)
                {
                    Transform child = lastStem.transform.GetChild(j);
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    //가을챕터에선 돌 생성되지 않도록
    void AutumnDisable(object sender, EventArgs eventArgs)
    {
        // ChapterManager가 null이면 처리하지 않음
        if (cm == null) return;

        if (cm.chapter.ToString().Contains("autumn"))
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                for (int j = 0; j < child.transform.childCount; j++)
                {
                    Transform childChild = child.transform.GetChild(j);
                    childChild.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                for (int j = 0; j < child.transform.childCount; j++)
                {
                    Transform childChild = child.transform.GetChild(j);
                    childChild.gameObject.SetActive(true);
                }
            }
        }
    }

    //챕터가 바뀌면 챕터에 맞춰 스프라이트 전부 변경
    void ChageSprite(object sender, EventArgs eventArgs)
    {
        // ChapterManager가 null이면 처리하지 않음
        if (cm == null) return;

        List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
        Sprite[] sp1;
        List<SpriteRenderer> suhangmokSp = spriteRenderers.FindAll(x => x.sortingLayerName == "suhangmok");
        List<SpriteRenderer> flowerSp = spriteRenderers.FindAll(x => x.sortingLayerName == "flower");
        if (flowerSp == null || flowerSp.Count == 0) Debug.Log("꽃잎을 찾을수 없네요");

        //챕터에 따라 스프라이트 선택
        sp1 = seasons[cm.chapter];
        //이전 스프라이트 번호에 맞게 스프라이트 전환
        foreach (SpriteRenderer spriteRenderer in suhangmokSp)
        {
            if (currentSp == sp1) break;
            int index = Array.FindIndex(currentSp, x => x == spriteRenderer.sprite);
            spriteRenderer.sprite = sp1[index];
        }
        currentSp = sp1;

        sp1 = seasonFlower[cm.chapter];
        foreach (SpriteRenderer spriteRenderer in flowerSp)
        {
            //가을 챕터는 꽃잎 안보이기
            if (cm.chapter.ToString().Contains("autumn")) { spriteRenderer.enabled = false; continue; }
            else spriteRenderer.enabled = true;

            if (currentFlowerSp == sp1) break;
            int index = Array.FindIndex(currentFlowerSp, x => x == spriteRenderer.sprite);
            spriteRenderer.sprite = sp1[index];
        }
        currentFlowerSp = sp1;
    }
    static IEnumerator CoFadeAndDisableAll()
    {
        // 리스트 스냅샷(페이드 중 Destroy로 빠져도 안전)
        var targets = _allTrees.ToArray();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, targets.First().fadeDuration);

            foreach (var tree in targets)
            {
                if (tree == null) continue;
                foreach (var sr in tree.GetComponentsInChildren<SpriteRenderer>())
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    sr.color = c;
                }
            }
            yield return null;
        }

        // 완전히 투명 후, 전부 비활성화
        foreach (var tree in targets)
            if (tree) tree.gameObject.SetActive(false);
    }
}
