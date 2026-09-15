using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns music only between entering space and leaving the ending.
/// Its four-second music grid never resets or stops the shared TickManager.
/// </summary>
public class SpaceEndingAudioController : MonoBehaviour
{
    public const float IntroSilenceSeconds = 2f;
    public const double BeatSeconds = 4d;

    static SpaceEndingAudioController instance;
    public static SpaceEndingAudioController Instance
    {
        get
        {
            if (!instance)
                instance = new GameObject(nameof(SpaceEndingAudioController))
                    .AddComponent<SpaceEndingAudioController>();
            return instance;
        }
    }

    public static SpaceEndingAudioController Current => instance;
    public event Action<int> OnMusicBeat;

    EventInstance music;
    string musicName;
    EventDescription[] bgmEvents;
    Bus masterBus;
    bool introSilent;
    float previousMasterVolume;
    bool clockStarted;
    bool clockPaused;
    bool quitting;
    double clockOrigin;
    double pauseStarted;
    int dispatchedBeat;
    Coroutine creditsMusicCo;

    public double MusicSeconds => !clockStarted ? 0d : Math.Max(0d,
        (clockPaused ? pauseStarted : Time.unscaledTimeAsDouble) - clockOrigin);
    public int MusicBeat => (int)Math.Floor(MusicSeconds / BeatSeconds);

    void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    public void BeginSilentIntro()
    {
        CancelCreditsMusic();
        StopMusic();
        clockStarted = false;
        SoundManager.Instance.StopBGM();
        CacheBGMEvents();

        if (!introSilent)
        {
            masterBus = RuntimeManager.GetBus("bus:/");
            masterBus.getVolume(out previousMasterVolume);
            masterBus.setVolume(0f);
            introSilent = true;
        }
        // Also remove outgoing one-shot tails/voices during this scene-local silence.
        masterBus.stopAllEvents(STOP_MODE.IMMEDIATE);
    }

    public IEnumerator PlaySpaceIntro()
    {
        yield return new WaitForSeconds(IntroSilenceSeconds);
        EndSilence();
        StartMusic("Space", 0, resetClock: true);
    }

    public void PlayFinalReplay()
    {
        CancelCreditsMusic();
        EndSilence();
        SoundManager.Instance.StopBGM();
        StartMusic("Space 3", -1, resetClock: true);
        SoundManager.Instance.spaceLoaded = false;
    }

    public void EnsureFinalSceneMusic()
    {
        // Normal scene handoff must keep the Space highlight at its existing position.
        if (clockStarted && music.isValid() &&
            (musicName == "Space" || musicName == "Space 3")) return;
        PlayFinalReplay();
    }

    public void SetSpaceState(int state)
    {
        if (music.isValid() && musicName == "Space")
            music.setParameterByName("SpaceState", state);
    }

    public IEnumerator WaitMusicBeats(int beats)
    {
        int target = MusicBeat + Math.Max(0, beats);
        while (MusicBeat < target) yield return null;
    }

    public void PlayCreditsMusic()
    {
        if (!clockStarted) PlayFinalReplay();
        CancelCreditsMusic();
        StopMusic();
        SuppressOtherBGM();
        // Preserve the old next-beat start and Credits' separate 20-second text delay.
        creditsMusicCo = StartCoroutine(StartCreditsOnNextBeat());
    }

    IEnumerator StartCreditsOnNextBeat()
    {
        yield return WaitMusicBeats(1);
        StartMusic("Space 2", -1, resetClock: false);
        creditsMusicCo = null;
    }

    void StartMusic(string name, int state, bool resetClock)
    {
        StopMusic();
        CacheBGMEvents();
        SuppressOtherBGM();
        music = RuntimeManager.CreateInstance("event:/BGM/" + name);
        musicName = name;
        if (state >= 0) music.setParameterByName("SpaceState", state);
        music.start();

        if (resetClock)
        {
            clockOrigin = Time.unscaledTimeAsDouble;
            dispatchedBeat = 0;
            clockStarted = true;
            clockPaused = false;
        }
        music.setPaused(clockPaused);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[SpaceEndingAudio] {name} started; local beat {MusicBeat}.");
#endif
    }

    public void StopMusic()
    {
        if (music.isValid())
        {
            music.stop(STOP_MODE.IMMEDIATE);
            music.release();
            music.clearHandle();
        }
        musicName = null;
    }

    void LateUpdate()
    {
        if (introSilent)
        {
            masterBus.stopAllEvents(STOP_MODE.IMMEDIATE);
            return;
        }

        // Anonymous shared BGM reservations cannot be cancelled here. Stop any
        // late/outgoing instance, without releasing another owner's handle.
        SuppressOtherBGM();
        if (!clockStarted) return;

        double now = Time.unscaledTimeAsDouble;
        bool paused = Time.timeScale <= 0f;
        if (paused != clockPaused)
        {
            if (paused) pauseStarted = now;
            else clockOrigin += now - pauseStarted;
            clockPaused = paused;
            if (music.isValid()) music.setPaused(paused);
        }

        int targetBeat = MusicBeat;
        while (dispatchedBeat < targetBeat)
        {
            // Always advance, even if the final-stone callback removes the last listener.
            dispatchedBeat++;
            OnMusicBeat?.Invoke(dispatchedBeat);
        }
    }

    void CacheBGMEvents()
    {
        if (bgmEvents != null) return;
        if (RuntimeManager.StudioSystem.getBank("bank:/BGM", out Bank bank) == FMOD.RESULT.OK)
            bank.getEventList(out bgmEvents);
    }

    void SuppressOtherBGM()
    {
        CacheBGMEvents();
        if (bgmEvents == null) return;
        foreach (EventDescription description in bgmEvents)
        {
            if (description.getInstanceCount(out int count) != FMOD.RESULT.OK || count == 0)
                continue;
            if (description.getInstanceList(out EventInstance[] playing) != FMOD.RESULT.OK)
                continue;
            foreach (EventInstance other in playing)
            {
                if (other.handle == music.handle) continue;
                // A queued start can still report STOPPED until FMOD's next update.
                // Queue the stop even then, so it cannot become an audible late start.
                other.stop(STOP_MODE.IMMEDIATE);
            }
        }
    }

    void EndSilence()
    {
        if (!introSilent) return;
        masterBus.setVolume(previousMasterVolume);
        introSilent = false;
    }

    void CancelCreditsMusic()
    {
        if (creditsMusicCo != null) StopCoroutine(creditsMusicCo);
        creditsMusicCo = null;
    }

    void OnSceneChanged(Scene previous, Scene next)
    {
        if (next.name != "TLTower" && next.name != "SpaceAnimation")
            Destroy(gameObject);
    }

    void OnApplicationQuit() => quitting = true;

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (instance != this) return;
        if (!quitting && RuntimeManager.IsInitialized)
        {
            CancelCreditsMusic();
            EndSilence();
            StopMusic();
        }
        instance = null;
    }
}
