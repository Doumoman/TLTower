# Tests the production LateUpdate beat loop without launching or freezing Unity.
# The iteration guard only exists in this harness, to fail an infinite loop safely.
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$source = Get-Content -LiteralPath (Join-Path $projectPath 'Assets/SpaceAnimationAssets/SpaceEndingAudioController.cs') -Raw
$start = $source.IndexOf('    void LateUpdate()')
$end = $source.IndexOf('    void CacheBGMEvents()', $start)
if ($start -lt 0 -or $end -lt 0) { throw 'Production LateUpdate method not found.' }
$method = $source.Substring($start, $end - $start)
$loop = 'while (dispatchedBeat < targetBeat)'
if (-not $method.Contains($loop)) { throw 'Expected production beat loop not found.' }
$method = $method.Replace($loop, 'while (GuardIteration() && dispatchedBeat < targetBeat)')

$fixture = @'
public class __TYPE__
{
    public event Action<int> OnMusicBeat;
    public int MusicBeat;
    public int DispatchedBeat { get { return dispatchedBeat; } }
    bool introSilent = false;
    bool clockStarted = true;
    bool clockPaused = false;
    double clockOrigin, pauseStarted;
    int dispatchedBeat, iterations;
    FakeBus masterBus = new FakeBus();
    FakeMusic music = new FakeMusic();
    void SuppressOtherBGM() {}
    bool GuardIteration()
    {
        if (++iterations > 32) throw new InvalidOperationException("Beat loop did not terminate.");
        return true;
    }
    public void Step() { iterations = 0; LateUpdate(); }
'@
$originalMethod = $method -replace '(?s)(        while \(GuardIteration\(\) && dispatchedBeat < targetBeat\)\r?\n)        \{\r?\n.*?\r?\n        \}', '$1            OnMusicBeat?.Invoke(++dispatchedBeat);'
if (-not $originalMethod.Contains('OnMusicBeat?.Invoke(++dispatchedBeat);')) { throw 'Original regression case not constructed.' }
$preamble = @'
using System;
using System.Collections.Generic;
public enum STOP_MODE { IMMEDIATE }
public class FakeBus { public void stopAllEvents(STOP_MODE mode) {} }
public class FakeMusic { public bool isValid() { return false; } public void setPaused(bool paused) {} }
public static class Time { public static double unscaledTimeAsDouble = 0; public static float timeScale = 1f; }
'@
$tests = @'
public static class SpaceBeatRegressionTests
{
    static int assertions;
    static void Check(bool value, string label)
    {
        assertions++;
        if (!value) throw new Exception("FAIL: " + label);
    }
    public static string Run()
    {
        var original = new OriginalBeatFixture { MusicBeat = 1 };
        bool stuck = false;
        try { original.Step(); } catch (InvalidOperationException) { stuck = true; }
        Check(stuck && original.DispatchedBeat == 0, "original loop freezes with no listeners");

        var originalEnding = new OriginalBeatFixture { MusicBeat = 1 };
        Action<int> originalListener = null;
        originalListener = beat => originalEnding.OnMusicBeat -= originalListener;
        originalEnding.OnMusicBeat += originalListener;
        originalEnding.Step();
        Check(originalEnding.DispatchedBeat == 1, "original ending callback completes its last subscribed beat");
        originalEnding.MusicBeat = 2;
        stuck = false;
        try { originalEnding.Step(); } catch (InvalidOperationException) { stuck = true; }
        Check(stuck && originalEnding.DispatchedBeat == 1, "original freezes on the beat after last stone unsubscribes");

        var quiet = new ProductionBeatFixture { MusicBeat = 1 };
        quiet.Step();
        Check(quiet.DispatchedBeat == 1, "no listeners still advances the counter");
        quiet.MusicBeat = 12;
        quiet.Step();
        Check(quiet.DispatchedBeat == 12, "missed beats without listeners terminate");
        quiet.Step();
        Check(quiet.DispatchedBeat == 12, "same-frame target does not dispatch again");

        var lastStone = new ProductionBeatFixture { MusicBeat = 6 };
        int endingCalls = 0;
        Action<int> ending = null;
        ending = beat => { endingCalls++; lastStone.OnMusicBeat -= ending; };
        lastStone.OnMusicBeat += ending;
        lastStone.Step();
        Check(endingCalls == 1 && lastStone.DispatchedBeat == 6, "last-stone listener removes itself during dispatch");
        lastStone.MusicBeat = 7;
        lastStone.Step();
        Check(lastStone.DispatchedBeat == 7, "next frame after ending unsubscribe also terminates");

        var events = new List<int>();
        quiet.OnMusicBeat += events.Add;
        quiet.MusicBeat = 14;
        quiet.Step();
        Check(events.Count == 2 && events[0] == 13 && events[1] == 14, "new listener receives only future beats");

        var remaining = new ProductionBeatFixture { MusicBeat = 3 };
        int removedCalls = 0;
        Action<int> removed = null;
        removed = beat => { removedCalls++; remaining.OnMusicBeat -= removed; };
        var retained = new List<int>();
        remaining.OnMusicBeat += removed;
        remaining.OnMusicBeat += retained.Add;
        remaining.Step();
        Check(removedCalls == 1 && retained.Count == 3 && remaining.DispatchedBeat == 3, "other subscribers keep their beat sequence");
        return "PASS: " + assertions + " assertions. Original freeze reproduced with a safety guard; production loop passes no-listener and last-stone-unsubscribe cases. Unity/audio not launched.";
    }
}
'@
$compiledSource = $preamble + "`n" + $fixture.Replace('__TYPE__', 'ProductionBeatFixture') + "`n" + $method + "`n}`n" + $fixture.Replace('__TYPE__', 'OriginalBeatFixture') + "`n" + $originalMethod + "`n}`n" + $tests
Add-Type -TypeDefinition $compiledSource -WarningAction SilentlyContinue
[SpaceBeatRegressionTests]::Run()
