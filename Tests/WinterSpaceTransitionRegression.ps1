# Runs the production transition coroutines with deterministic UI/audio stand-ins.
# No Unity process or audible FMOD playback is launched by this test.
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
function Read-Source([string] $relativePath) {
    Get-Content -LiteralPath (Join-Path $projectPath $relativePath) -Raw
}
function Get-Method([string] $source, [string] $signature) {
    $start = $source.IndexOf($signature)
    if ($start -lt 0) { throw "Missing production method: $signature" }
    $open = $source.IndexOf('{', $start)
    $depth = 1
    $end = $open + 1
    while ($depth -gt 0 -and $end -lt $source.Length) {
        if ($source[$end] -eq '{') { $depth++ }
        if ($source[$end] -eq '}') { $depth-- }
        $end++
    }
    if ($depth -ne 0) { throw "Unbalanced method: $signature" }
    $source.Substring($start, $end - $start)
}
$chapterSource = Read-Source 'Assets/Scripts/Managers/ChapterManager.cs'
$audioSource = Read-Source 'Assets/SpaceAnimationAssets/SpaceEndingAudioController.cs'
$resetSource = Read-Source 'Assets/Scripts/Stone/ResetStone.cs'
$fixerSource = Read-Source 'Assets/Scripts/Stone/StoneFixer.cs'
$cloudSource = (Read-Source 'Assets/SpaceAnimationAssets/WinterSpaceCloudTransition.cs') -replace '(?m)^using [^;]+;\r?\n', ''
$audioMethods = @(
    'public void BeginWinterTransition()',
    'public void SetWinterTransitionProgress(float progress)',
    'public void BeginSilentIntro()',
    'public IEnumerator PlaySpaceIntro()',
    'void EndSilence()',
    'void EndWinterTransition()',
    'void LateUpdate()'
) | ForEach-Object { Get-Method $audioSource $_ }
$fixture = @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using STOP_MODE = TestStopMode;
public enum TestStopMode { IMMEDIATE }
namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static implicit operator bool(Object obj) => obj != null && !obj.destroyed;
        public static void Destroy(Object obj) { obj.destroyed = true; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public bool TryGetComponent<T>(out T result) where T : Component => gameObject.TryGetComponent(out result);
    }
    public class Transform : Component
    {
        public Transform parent;
        public Vector3 position;
        public Quaternion localRotation;
        public void SetParent(Transform value, bool worldPositionStays) { parent = value; }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchoredPosition, anchorMin, anchorMax, sizeDelta;
    }
    public class GameObject : Object
    {
        public static List<GameObject> all = new();
        public string name;
        public bool activeSelf = true;
        readonly List<Component> components = new();
        public Transform transform;
        public GameObject(string name, params Type[] types)
        {
            this.name = name;
            transform = types.Contains(typeof(RectTransform)) ? new RectTransform() : new Transform();
            transform.gameObject = this;
            components.Add(transform);
            foreach (Type type in types.Where(t => t != typeof(RectTransform)))
            {
                var component = (Component)Activator.CreateInstance(type);
                component.gameObject = this;
                components.Add(component);
            }
            all.Add(this);
        }
        public T GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();
        public bool TryGetComponent<T>(out T result) where T : Component
        { result = GetComponent<T>(); return result != null; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => all
            .Where(obj => !obj.destroyed && (obj == this || obj.transform.parent == transform))
            .SelectMany(obj => obj.components.OfType<T>()).ToArray();
        public void SetActive(bool active) { activeSelf = active; }
        public static GameObject Find(string name) => all.LastOrDefault(obj => !obj.destroyed && obj.name == name);
    }
    public class Sprite : Object { public Rect rect = new Rect { width = 2306f, height = 1354f }; }
    public class SpriteRenderer : Component { public Sprite sprite = new(); public Color color = new(0.65f, 0.72f, 0.74f, 0.9f); public bool enabled = true; }
    public class CanvasRenderer : Component {}
    public class Collider2D : Component { public Bounds bounds; }
    public struct Bounds { public Vector3 max; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public class Canvas : Component { public RenderMode renderMode; public int sortingOrder; }
    public enum RenderMode { ScreenSpaceOverlay }
    public struct Rect
    {
        public float x, y, width, height;
        public float xMax => x + width;
        public float yMax => y + height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    public class Camera : Object { public static Camera main = new(); public Rect rect = new(0f, 0f, 1f, 1f); }
    public struct Color
    {
        public float r, g, b, a;
        public static Color black => new(0f, 0f, 0f, 1f);
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new(0, 0);
        public static Vector2 one => new(1, 1);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float value) => new(a.x * value, a.y * value);
    }
    public struct Quaternion { public static Quaternion identity => new(); public static Quaternion Euler(float x, float y, float z) => new(); }
    public static class Mathf
    {
        public const float Deg2Rad = (float)(Math.PI / 180d);
        public static float Cos(float value) => (float)Math.Cos(value);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
        public static float SmoothStep(float from, float to, float t) => from + (to - from) * t * t * (3f - 2f * t);
    }
    public static class Screen { public static int width = 2160, height = 1080; }
    public static class Time { public static float deltaTime = 1f / 60f, timeScale = 1f; public static double unscaledTimeAsDouble; }
    public static class Debug { public static void Log(object message) {} public static void LogWarning(object message) {} }
    public class WaitForSeconds { public float seconds; public WaitForSeconds(float value) { seconds = value; } }
}
namespace UnityEngine.UI
{
    public class Image : Component
    {
        public Sprite sprite; public Color color; public bool raycastTarget;
        public RectTransform rectTransform => (RectTransform)transform;
    }
    public class CanvasScaler : Component
    { public enum ScaleMode { ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight; }
    public class GraphicRaycaster : Component {}
    public class RectMask2D : Component {}
}
public class TestBus
{
    public float volume;
    public int stops;
    public TestBus(float volume) { this.volume = volume; }
    public void getVolume(out float value) { value = volume; }
    public void setVolume(float value) { volume = value; }
    public void stopAllEvents(TestStopMode mode) { stops++; }
}
public static class RuntimeManager
{
    public static TestBus master = new(0.81f), bgm = new(0.37f);
    public static TestBus GetBus(string path) => path == "bus:/BGM" ? bgm : master;
}
public class SoundManager
{
    public static SoundManager Instance = new(); public int stops;
    public void StopBGM() { stops++; }
}
public class TestMusic { public bool isValid() => false; public void setPaused(bool value) {} }
public class SpaceEndingAudioController
{
    public static SpaceEndingAudioController Instance = new();
    public const float IntroSilenceSeconds = 2f;
    TestBus masterBus, bgmBus;
    bool introSilent, winterTransition, clockStarted, clockPaused;
    float previousMasterVolume, previousBGMVolume;
    double clockOrigin, pauseStarted;
    int dispatchedBeat;
    TestMusic music = new();
    public event Action<int> OnMusicBeat;
    public int suppressed, musicStarts;
    public double introStart, musicStart;
    public bool Silent => introSilent;
    public bool Fading => winterTransition;
    public double MusicSeconds => !clockStarted ? 0d : Math.Max(0d, (clockPaused ? pauseStarted : Time.unscaledTimeAsDouble) - clockOrigin);
    public int MusicBeat => (int)Math.Floor(MusicSeconds / 4d);
    void CacheBGMEvents() {}
    void CancelCreditsMusic() {}
    public void StopMusic() {}
    void SuppressOtherBGM() { suppressed++; }
    void StartMusic(string name, int state, bool resetClock)
    {
        musicStarts++; musicStart = Time.unscaledTimeAsDouble;
        clockOrigin = musicStart; clockStarted = true;
    }
    public void Step() { LateUpdate(); }
__AUDIO_METHODS__
}
public enum chapter { spring, summer, autumn, winter, winter4, space }
public class ResetStone
{
    public static ResetStone Instance = new();
    public GameObject lastPlatform = new("Platform", typeof(SpriteRenderer));
    public GameObject startingPlatform = new("StartingPlatform", typeof(SpriteRenderer));
    readonly Dictionary<SpriteRenderer, bool> hiddenPlatformRenderers = new();
__VISIBILITY_METHOD__
__STARTING_HAND_METHOD__
}
public class CameraController
{
    public static CameraController Instance = new(); public int rises; public double riseStart;
    public int centers;
    public void CenterOnY(float y) { centers++; }
    public void RaiseCameraY() { rises++; riseStart = Time.unscaledTimeAsDouble; }
}
public class AnimationManager
{
    public static AnimationManager Instance = new();
    public List<RectTransform> artwork = new();
    public IEnumerator PlayWinterToSpaceTransition(Action covered, Action<float> progress) =>
        WinterSpaceCloudTransition.Play(artwork, ChapterManager.Instance.transform, covered, progress);
    public void Play1() { throw new Exception("Shared seasonal clouds must not run for space."); }
}
public class SaveSystem
{ public static SaveSystem Instance = new(); public void SaveGame() {} }
public class ChapterManager : UnityEngine.Object
{
    public static ChapterManager Instance = new();
    public Transform transform = new GameObject("ChapterManager").transform;
    public chapter chapter = chapter.winter4;
    int stoneCount;
    public int Counts => stoneCount;
    public void AddCount() { stoneCount++; }
    int[] stonesForChapter = new int[6];
    public IEnumerator started;
    public bool spaceVisuals;
    public event EventHandler onChapterChage, removeYumju;
    void ConfigureCurrentStage() {}
    void SetObstacle() { spaceVisuals = true; }
    void StartCoroutine(IEnumerator routine) { started = routine; }
    IEnumerator WaitAndChange() { yield break; }
__CHANGE_METHOD__
__INTRO_METHOD__
}
public enum StoneState { Settled, Fixed, Dropping }
public class StoneController : Component { public StoneState state; }
public class SavePoint : Component { public void Init(object fixer) {} }
__FIXER_CLASSES__
public static class WinterSpaceRegressionTests
{
    static int assertions;
    static void Check(bool value, string label)
    { assertions++; if (!value) throw new Exception("FAIL: " + label); }
    static bool Near(double a, double b) => Math.Abs(a - b) < 0.0001;
    sealed class Runner
    {
        readonly Stack<IEnumerator> stack = new();
        float waiting;
        public Runner(IEnumerator routine) { stack.Push(routine); }
        public bool Step()
        {
            if (waiting > 0f) { waiting -= Time.deltaTime; if (waiting > 0f) return true; }
            for (int guard = 0; guard < 32 && stack.Count > 0; guard++)
            {
                var routine = stack.Peek();
                if (!routine.MoveNext()) { stack.Pop(); continue; }
                if (routine.Current is IEnumerator child) { stack.Push(child); continue; }
                if (routine.Current is WaitForSeconds delay) waiting = delay.seconds;
                return true;
            }
            if (stack.Count > 0) throw new Exception("Coroutine nesting did not yield.");
            return false;
        }
    }
    public static string Run()
    {
        var chapterManager = ChapterManager.Instance;
        var audio = SpaceEndingAudioController.Instance;
        var hand = ResetStone.Instance.lastPlatform.GetComponent<SpriteRenderer>();
        var lowerHand = ResetStone.Instance.startingPlatform.GetComponent<SpriteRenderer>();
        var disabled = new GameObject("OriginallyDisabled", typeof(SpriteRenderer));
        disabled.transform.SetParent(ResetStone.Instance.lastPlatform.transform, false);
        disabled.GetComponent<SpriteRenderer>().enabled = false;
        var art = new GameObject("OriginalCloud", typeof(RectTransform), typeof(SpriteRenderer));
        var artRect = (RectTransform)art.transform;
        artRect.anchoredPosition = new Vector2(1234f, 567f);
        AnimationManager.Instance.artwork.Add(artRect);
        int backgroundCalls = 0;
        chapterManager.onChapterChage += (sender, args) =>
        {
            backgroundCalls++;
            Check(Near(GameObject.Find("CloudCover").GetComponent<Image>().color.a, 1d), "background changes under opaque clouds");
            Check(!hand.enabled && audio.Fading && !audio.Silent, "hand still hidden and winter still fading at background swap");
        };
        chapterManager.ChangeChapter(checkpointReached: true);
        Check(chapterManager.chapter == chapter.space && !chapterManager.spaceVisuals, "logical space entry keeps winter visuals initially");
        Check(!hand.enabled, "checkpoint-created hand hidden before first transition frame");
        Check(lowerHand.enabled, "original lower hand remains until cloud cover");
        var firstRoutine = chapterManager.started;
        chapterManager.ChangeChapter(checkpointReached: true);
        Check(ReferenceEquals(firstRoutine, chapterManager.started), "duplicate checkpoint does not start another transition");

        var runner = new Runner(firstRoutine);
        bool observedRush = false, observedTail = false, observedSilence = false, pauseChecked = false;
        float lastVolume = RuntimeManager.bgm.volume;
        int frames = 0;
        while (runner.Step())
        {
            if (++frames > 1000) throw new Exception("Transition did not finish.");
            audio.Step();
            var overlay = GameObject.Find("WinterToSpaceClouds");
            if (overlay != null && overlay.activeSelf)
            {
                var count = GameObject.all.Count(obj => !obj.destroyed && obj.activeSelf && obj.name.StartsWith("Cloud_"));
                if (!chapterManager.spaceVisuals && count == 21) observedRush = true;
                if (chapterManager.spaceVisuals && count == 3) observedTail = true;
                Check(audio.Fading && !audio.Silent && audio.musicStarts == 0 && SoundManager.Instance.stops == 0,
                    "cloud frames preserve fading winter and never start space music or silence early");
                Check(RuntimeManager.bgm.volume <= lastVolume + 0.00001f, "winter fade is monotonic");
                lastVolume = RuntimeManager.bgm.volume;
                if (!pauseChecked && Time.unscaledTimeAsDouble > 0.5d)
                {
                    float volume = RuntimeManager.bgm.volume;
                    var position = GameObject.Find("Cloud_0").GetComponent<RectTransform>().anchoredPosition;
                    Time.deltaTime = 0f; Time.timeScale = 0f;
                    for (int paused = 0; paused < 30; paused++) { runner.Step(); audio.Step(); }
                    var after = GameObject.Find("Cloud_0").GetComponent<RectTransform>().anchoredPosition;
                    Check(Near(volume, RuntimeManager.bgm.volume) && Near(position.x, after.x), "paused transition yields without advancing clouds or fade");
                    Time.deltaTime = 1f / 60f; Time.timeScale = 1f; pauseChecked = true;
                }
            }
            if (audio.Silent)
            {
                if (!observedSilence) audio.introStart = Time.unscaledTimeAsDouble;
                observedSilence = true;
                Check(overlay == null || !overlay.activeSelf, "clouds are removed before silence begins");
                Check(hand.enabled && chapterManager.spaceVisuals && !audio.Fading, "silent reveal already contains space and hand");
                Check(!lowerHand.enabled && ResetStone.Instance.startingPlatform.activeSelf, "obsolete lower hand removed visually without disabling platform physics");
                Check(Near(RuntimeManager.master.volume, 0d) && Near(RuntimeManager.bgm.volume, 0.37d), "silence mutes master and restores user BGM volume");
                Check(audio.musicStarts == 0 && CameraController.Instance.rises == 0, "space music and camera wait through silence");
            }
            Time.unscaledTimeAsDouble += Time.deltaTime;
        }
        audio.Step(); // Unity also runs LateUpdate in the frame that starts space music.
        Check(observedRush && observedTail && observedSilence, "dense rush, sparse tail, then silent reveal all occur");
        Check(backgroundCalls == 1, "background changes exactly once");
        Check(audio.musicStart - audio.introStart >= 2d - 0.001d && audio.musicStart - audio.introStart < 2.04d, "space starts after exactly two seconds of silence within one frame");
        Check(audio.musicStarts == 1 && CameraController.Instance.rises == 1 && Near(audio.musicStart, CameraController.Instance.riseStart), "space music and camera animation start together");
        Check(Near(audio.MusicSeconds, 0d), "space music grid starts at zero after transition, not at checkpoint");
        Check(Near(RuntimeManager.master.volume, 0.81d) && Near(RuntimeManager.bgm.volume, 0.37d), "original master and BGM volumes restored");
        Check(!disabled.GetComponent<SpriteRenderer>().enabled, "reveal preserves originally disabled platform renderers");
        Check(Near(artRect.anchoredPosition.x, 1234d) && Near(artRect.anchoredPosition.y, 567d), "shared cloud artwork positions untouched");
        Check(audio.suppressed > 0 && RuntimeManager.master.stops > 0, "late-BGM blocking resumes after transition and silence clears sound tails");

        int covered = 0;
        var cancelled = WinterSpaceCloudTransition.Play(AnimationManager.Instance.artwork, chapterManager.transform, () => covered++, null);
        cancelled.MoveNext(); ((IDisposable)cancelled).Dispose();
        Check(covered == 0 && GameObject.Find("WinterToSpaceClouds") == null, "cancelled cloud coroutine cleans up its overlay");
        var failed = WinterSpaceCloudTransition.Play(AnimationManager.Instance.artwork, chapterManager.transform, () => throw new InvalidOperationException("test"), null);
        bool caught = false;
        try { for (int guard = 0; guard < 300 && failed.MoveNext(); guard++) {} }
        catch (InvalidOperationException) { caught = true; }
        Check(caught && GameObject.Find("WinterToSpaceClouds") == null, "midpoint callback failure also cleans up overlay");

        var late = new GameObject("LateSettled", typeof(StoneController)).GetComponent<StoneController>();
        late.state = StoneState.Settled;
        late.transform.position = new Vector3(0f, 10f, 0f);
        var fixer = new ProductionStoneFixer();
        int counts = chapterManager.Counts, centers = CameraController.Instance.centers;
        fixer.RegisterSettled(late);
        fixer.SpawnForTest();
        Check(fixer.spawns == 0 && fixer.BatchCount == 0, "space blocks late settled registration and direct checkpoint spawning");
        Check(chapterManager.Counts == counts && CameraController.Instance.centers == centers
            && Near(fixer.HighestSettledY, 0d), "late winter stone cannot change space counts, heights or camera");
        var original = new OriginalStoneFixer();
        original.RegisterSettled(late);
        Check(original.spawns == 1, "old zero-threshold path reproduces an extra checkpoint after space entry");
        chapterManager.chapter = chapter.winter4;
        fixer.RegisterSettled(late);
        Check(fixer.spawns == 1 && fixer.BatchCount == 1, "normal winter zero-threshold checkpoint behavior stays intact");
        fixer.CompleteForTest();
        chapterManager.chapter = chapter.space;
        fixer.RegisterSettled(late);
        Check(fixer.spawns == 1 && fixer.BatchCount == 0, "completed winter stage never recreates its checkpoint during clouds");

        foreach (var view in new[] { new Rect(0f, 0.1f, 1f, 0.8f), new Rect(0.1f, 0f, 0.8f, 1f) })
        {
            Camera.main.rect = view;
            var clipped = WinterSpaceCloudTransition.Play(AnimationManager.Instance.artwork, chapterManager.transform, null, null);
            clipped.MoveNext();
            var root = GameObject.Find("CloudViewport");
            var rect = root.GetComponent<RectTransform>();
            Check(root.GetComponent<RectMask2D>() != null, "viewport has a UI clipping mask");
            Check(Near(rect.anchorMin.x, view.x) && Near(rect.anchorMin.y, view.y)
                && Near(rect.anchorMax.x, view.xMax) && Near(rect.anchorMax.y, view.yMax), "cloud mask matches camera viewport for both letterbox directions");
            Check(GameObject.Find("CloudCover").transform.parent == root.transform
                && GameObject.Find("Cloud_0").transform.parent == root.transform, "cover and moving clouds are both inside viewport mask");
            foreach (var name in new[] { "BottomLetterbox", "TopLetterbox", "LeftLetterbox", "RightLetterbox" })
            {
                var bar = GameObject.Find(name).GetComponent<Image>();
                Check(Near(bar.color.r, 0d) && Near(bar.color.g, 0d) && Near(bar.color.b, 0d)
                    && Near(bar.color.a, 1d), "letterbox backing is opaque black");
            }
            ((IDisposable)clipped).Dispose();
        }
        Camera.main.rect = new Rect(0f, 0f, 1f, 1f);
        return "PASS: " + assertions + " assertions. Production cloud, checkpoint, visibility and audio phase methods verified with stand-ins; Unity visual/audio playback not launched.";
    }
}
'@
$fixture = $fixture.Replace('__AUDIO_METHODS__', ($audioMethods -join "`n"))
$fixture = $fixture.Replace('__VISIBILITY_METHOD__', (Get-Method $resetSource 'public void SetPlatformVisualsVisible(bool visible)'))
$fixture = $fixture.Replace('__STARTING_HAND_METHOD__', (Get-Method $resetSource 'public void HideStartingPlatformVisualsForSpace()'))
$fixture = $fixture.Replace('__CHANGE_METHOD__', (Get-Method $chapterSource 'public void ChangeChapter(bool checkpointReached = false)'))
$fixture = $fixture.Replace('__INTRO_METHOD__', (Get-Method $chapterSource 'IEnumerator WaitAndStartSpace()'))
$fixerMethods = (Get-Method $fixerSource 'public void RegisterSettled(StoneController sc)') + "`n" + (Get-Method $fixerSource 'void SpawnSavePoint()')
$fixerTemplate = @'
public class __TYPE__
{
    int threshold = 0, wave;
    GameObject currentSavePoint, savePointPrefab = new("CheckpointPrefab");
    readonly List<StoneController> batch = new();
    public float HighestSettledY, HighestFixedY;
    public int spawns;
    public int BatchCount => batch.Count;
    void UpdateUI() {}
    void SetProgressUIVisible(bool visible) {}
    void CheckAndSound() {}
    GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
    { spawns++; return new GameObject("SpawnedCheckpoint", typeof(SavePoint)); }
    public void SpawnForTest() { SpawnSavePoint(); }
    public void CompleteForTest() { batch.Clear(); currentSavePoint = null; }
__METHODS__
}
'@
$originalFixerMethods = $fixerMethods.Replace('if (ChapterManager.Instance && ChapterManager.Instance.chapter == chapter.space) return;', '')
$fixerClasses = $fixerTemplate.Replace('__TYPE__', 'ProductionStoneFixer').Replace('__METHODS__', $fixerMethods) + "`n" + $fixerTemplate.Replace('__TYPE__', 'OriginalStoneFixer').Replace('__METHODS__', $originalFixerMethods)
$fixture = $fixture.Replace('__FIXER_CLASSES__', $fixerClasses)
Add-Type -TypeDefinition ($fixture + "`n" + $cloudSource) -WarningAction SilentlyContinue
[WinterSpaceRegressionTests]::Run()
