# Exercises the production removal coroutine with deterministic Animator stand-ins.
# Does not launch Unity or change saved game data.
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$source = Get-Content -LiteralPath (Join-Path $projectPath 'Assets/Scripts/Stone/StoneFixer.cs') -Raw
$start = $source.IndexOf('    IEnumerator RemoveSavePointAfterFade(GameObject sp)')
$end = $source.IndexOf('    #endregion', $start)
if ($start -lt 0 -or $end -lt 0) { throw 'Production removal coroutine not found.' }
$method = $source.Substring($start, $end - $start)
$clip = Get-Content -LiteralPath (Join-Path $projectPath 'Assets/Animations/SavePointFadeout.anim') -Raw
if ($clip -notmatch 'm_LoopTime: 0' -or $clip -match 'm_LoopTime: 1') {
    throw 'Checkpoint fade must not loop.'
}
$fixture = @'
using System;
using System.Collections;
using System.Collections.Generic;
public class GameObject
{
    public bool destroyed, active = true;
    public Animator animator;
    public readonly List<string> operations = new List<string>();
    public static implicit operator bool(GameObject obj) => obj != null && !obj.destroyed;
    public T GetComponent<T>() where T : class => animator as T;
    public void SetActive(bool value) { active = value; operations.Add("hide"); }
}
public class RuntimeAnimatorController
{
    public static implicit operator bool(RuntimeAnimatorController obj) => obj != null;
}
public class Animator
{
    public bool isActiveAndEnabled = true, destroyed;
    public RuntimeAnimatorController runtimeAnimatorController = new RuntimeAnimatorController();
    public float speed, progress;
    public string state = "SavePointFadeout";
    public static implicit operator bool(Animator obj) => obj != null && !obj.destroyed;
    public void Play(string name, int layer, float normalizedTime) { progress = normalizedTime; }
    public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer) =>
        new AnimatorStateInfo { name = state, normalizedTime = progress };
}
public struct AnimatorStateInfo
{
    public string name;
    public float normalizedTime;
    public bool IsName(string value) => name == value;
}
public static class Time { public static float deltaTime = 1f / 60f; }
public static class Debug { public static void Log(object message) {} }
public class WaitForSeconds { public float seconds; public WaitForSeconds(float value) { seconds = value; } }
public class RemovalFixture
{
    static void Destroy(GameObject obj) { obj.operations.Add("destroy"); obj.destroyed = true; }
    public IEnumerator Run(GameObject obj) => RemoveSavePointAfterFade(obj);
__METHOD__
}
public static class CheckpointFadeTests
{
    static int assertions;
    static void Check(bool condition, string label)
    { assertions++; if (!condition) throw new Exception("FAIL: " + label); }
    static void Removed(GameObject obj, string label)
    {
        Check(obj.destroyed && !obj.active, label);
        Check(obj.operations.Count == 2 && obj.operations[0] == "hide"
            && obj.operations[1] == "destroy", "hide always precedes deferred destruction");
    }
    static int Finish(IEnumerator routine, int limit = 1000)
    {
        int frames = 0;
        while (routine.MoveNext()) if (++frames > limit) throw new Exception("Removal did not finish.");
        return frames;
    }
    public static string Run()
    {
        var fixture = new RemovalFixture();
        var normal = new GameObject { animator = new Animator() };
        var routine = fixture.Run(normal);
        Check(routine.MoveNext() && normal.active, "fade remains visible while starting");
        normal.animator.progress = 0.99f;
        Check(routine.MoveNext() && normal.active, "non-looping fade waits for completion");
        normal.animator.progress = 1f;
        Check(!routine.MoveNext(), "completed fade removes in the current coroutine step");
        Removed(normal, "normal fade is hidden and removed");

        var slow = new GameObject { animator = new Animator() };
        Time.deltaTime = 0.4f;
        routine = fixture.Run(slow);
        routine.MoveNext();
        slow.animator.progress = 1.3f;
        Check(!routine.MoveNext(), "low-FPS progress skipping past the end still removes");
        Removed(slow, "low-FPS removal does not leave a visible frame");

        foreach (string state in new[] { "SavePointFadeout", "MissingState" })
        {
            var stalled = new GameObject { animator = new Animator { state = state } };
            Time.deltaTime = 0.5f;
            Check(Finish(fixture.Run(stalled)) <= 4, "stalled or missing state has a bounded wait");
            Removed(stalled, "timeout clears a stalled checkpoint");
        }

        var paused = new GameObject { animator = new Animator() };
        routine = fixture.Run(paused);
        Time.deltaTime = 0f;
        for (int frame = 0; frame < 30; frame++)
            Check(routine.MoveNext() && paused.active, "pause yields without consuming the fade timeout");
        Time.deltaTime = 0.5f;
        Finish(routine);
        Removed(paused, "removal resumes after pause");

        var disabled = new GameObject { animator = new Animator { isActiveAndEnabled = false } };
        Check(!fixture.Run(disabled).MoveNext(), "disabled animator does not wait forever");
        Removed(disabled, "disabled animator checkpoint is removed");

        var missing = new GameObject();
        routine = fixture.Run(missing);
        Check(routine.MoveNext() && routine.Current is WaitForSeconds
            && ((WaitForSeconds)routine.Current).seconds == 1f, "no animator retains the one-second fallback");
        Check(!routine.MoveNext(), "fallback then finishes");
        Removed(missing, "fallback hides before removing");

        var external = new GameObject { animator = new Animator() };
        routine = fixture.Run(external);
        routine.MoveNext(); external.destroyed = true;
        Check(!routine.MoveNext() && external.operations.Count == 0,
            "externally destroyed checkpoint is not accessed again");
        Check(!fixture.Run(null).MoveNext(), "null checkpoint exits safely");
        return "PASS: " + assertions + " assertions. Non-looping asset, completion, low FPS, timeout, pause and immediate-hide ordering verified. Unity/device playback not launched.";
    }
}
'@
Add-Type -TypeDefinition $fixture.Replace('__METHOD__', $method) -WarningAction SilentlyContinue
[CheckpointFadeTests]::Run()
