using UnityEngine;
using UnityEngine.Playables;

public class title : MonoBehaviour
{
    [Header("Playback")]
    public float speed = 1f;

    [Header("Debug / Trace")]
    public bool enableTrace = true;
    [Tooltip("몇 프레임마다 한 번씩만 로그 찍을지")]
    public int traceEveryNFrames = 30;

    private PlayableDirector director;
    private Playable rootPlayable;
    private int frameCount;
    private double lastDirectorTime;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
        // GameTime이면 SetSpeed가 직관적으로 동작함 (Unscaled도 OK, DSPClock는 오디오 시계라 다름)
        director.timeUpdateMode = DirectorUpdateMode.GameTime;

        // 그래프 재생/정지/리빌드에 반응해서 루트 다시 잡기
        director.played += OnDirectorPlayed;
        director.stopped += OnDirectorStopped;
    }

    void Start()
    {
        // Play()가 그래프를 빌드함. 즉시 루트 획득 시도.
        director.Play();
        EnsureRootPlayable(fastLog: true);
        lastDirectorTime = director.time;
    }

    void Update()
    {
        // 매 프레임: 루트 유효성 보장 → 속도 설정
        if (!EnsureRootPlayable())
            return;

        rootPlayable.SetSpeed(speed);

        // (선택) 재생 중이 아닐 때도 속도값은 저장되지만, trace는 찍어두자
        if (enableTrace && (++frameCount % traceEveryNFrames == 0))
        {
            var g = director.playableGraph;
            double dt = director.time - lastDirectorTime;
            lastDirectorTime = director.time;

            Debug.Log(
                $"[TitleTrace] state={director.state} " +
                $"graphValid={g.IsValid()} roots={g.GetRootPlayableCount()} " +
                $"rootValid={rootPlayable.IsValid()} speedSet={speed} " +
                $"dirTimeΔ={dt:F4}");
        }
    }

    void OnDisable()
    {
        // Component 비활성화 시 Update가 더는 안 돌도록 하고, 다음 활성화 때 다시 보장
        // 여긴 일부러 그래프는 건드리지 않음(다른 곳에서 쓸 수도 있으니)
    }

    void OnDestroy()
    {
        // 이벤트 정리
        if (director != null)
        {
            director.played -= OnDirectorPlayed;
            director.stopped -= OnDirectorStopped;
        }
    }

    private void OnDirectorPlayed(PlayableDirector _)
    {
        // 재생 시작 시점에 그래프가 (재)빌드되므로 루트 갱신
        EnsureRootPlayable(fastLog: true);
    }

    private void OnDirectorStopped(PlayableDirector _)
    {
        // 멈췄을 때도 루트가 무효가 될 수 있음. 다음 프레임에 다시 잡도록 비워둠.
        rootPlayable = default;
    }

    /// <summary>
    /// 루트 플레이어블을 유효하게 보장. 필요하면 재획득.
    /// 반환값: 유효하면 true, 아니면 false (Update에서 조용히 리턴)
    /// </summary>
    private bool EnsureRootPlayable(bool fastLog = false)
    {
        if (director == null)
        {
            if (fastLog || enableTrace) Debug.LogWarning("[TitleTrace] No PlayableDirector found.");
            return false;
        }

        var graph = director.playableGraph;

        if (!graph.IsValid())
        {
            // 아직 그래프가 안 만들어졌거나 파괴됨
            if (fastLog || enableTrace) Debug.LogWarning("[TitleTrace] Graph is invalid. Will try again next frame.");
            rootPlayable = default;
            return false;
        }

        // root가 없을 수 있음 (0개) → 이때는 아직 그래프가 완전히 준비 안 된 상태
        int rootCount = graph.GetRootPlayableCount();
        if (rootCount <= 0)
        {
            if (fastLog || enableTrace) Debug.LogWarning("[TitleTrace] No root playables yet. (rootCount=0)");
            rootPlayable = default;
            return false;
        }

        // 캐시가 유효하면 OK
        if (rootPlayable.IsValid())
            return true;

        // 캐시가 무효면 즉시 재획득
        var freshRoot = graph.GetRootPlayable(0);
        if (!freshRoot.IsValid())
        {
            if (fastLog || enableTrace) Debug.LogWarning("[TitleTrace] Fresh root acquired but still invalid.");
            rootPlayable = default;
            return false;
        }

        rootPlayable = freshRoot;

        if (fastLog || enableTrace)
        {
            Debug.Log($"[TitleTrace] Root reacquired. state={director.state} " +
                      $"roots={rootCount} timeUpdate={director.timeUpdateMode}");
        }

        return true;
    }

    void Short1()
    {
        SoundManager.Instance.PlaySFX("stroke_short");
    }
    void Short2()
    {
        SoundManager.Instance.PlaySFX("stroke_short_2");
    }
    void Medium()
    {
        SoundManager.Instance.PlaySFX("stroke_medium");
    }
    void Long()
    {
        SoundManager.Instance.PlaySFX("stroke_long");
    }
    void Bend()
    {
        SoundManager.Instance.PlaySFX("stroke_bend");
    }
}