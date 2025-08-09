using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

[DisallowMultipleComponent]
public class SpatialSound : MonoBehaviour
{
    [Header("Base FMOD path (trailing slash!)")]
    public string basePath = "event:/SFX/"; // 최종 경로는 basePath + key

    [Header("Optional")]
    public Rigidbody rb;   // 있으면 도플러/속도 전달

    // key -> EventDescription 캐시
    readonly Dictionary<string, EventDescription> descCache = new();

    EventInstance current; // 현재 재생 중 인스턴스
    EventInstance loop;
    string loopKey;
    bool loopPlaying;
    string currentKey;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void OnDisable() { StopImmediate(); }
    void OnDestroy() { StopImmediate(); }

    public bool IsPlaying
    {
        get
        {
            if (!current.hasHandle()) return false;
            current.getPlaybackState(out var s);
            return s == PLAYBACK_STATE.PLAYING || s == PLAYBACK_STATE.STARTING;
        }
    }

    public void PlaySFX(string key, float vol = 1f)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[SpatialSoundSimple] key is null/empty on {name}");
            return;
        }

        // 기존 인스턴스 정리(같은 Emitter에서 교체 재생)
        if (current.hasHandle())
        {
            current.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            current.release();
            current.clearHandle();
        }

        // EventDescription 가져오기(없으면 생성해서 캐싱)
        if (!descCache.TryGetValue(key, out var desc))
        {
            string path = basePath + key; // 예: "event:/SFX/" + "Footstep"
            var getResult = RuntimeManager.StudioSystem.getEvent(path, out desc);
            if (getResult != FMOD.RESULT.OK)
            {
                Debug.LogError($"[SpatialSoundSimple] getEvent failed ({getResult}) : {path}");
                return;
            }
            descCache[key] = desc;

            // (선택) 샘플 선로딩: 잦은 사용 SFX면 켜 두면 지연 감소
            // desc.loadSampleData();
        }

        // 3D 이벤트인지 가드(스튜디오에서 3D 세팅 확인용)
        desc.is3D(out bool is3d);
        if (!is3d)
            Debug.LogWarning($"[SpatialSoundSimple] '{basePath + key}' is not 3D.");

        // 인스턴스 생성 → 게임오브젝트에 부착 → 볼륨 → 재생
        desc.createInstance(out current);
        currentKey = key;

        RuntimeManager.AttachInstanceToGameObject(current, gameObject, rb);
        current.setVolume(Mathf.Clamp01(vol));

        var startResult = current.start();
        if (startResult != FMOD.RESULT.OK)
            Debug.LogError($"[SpatialSoundSimple] start failed ({startResult}) : {basePath + key}");
    }

    public void StopImmediate()
    {
        if (!current.hasHandle()) return;
        current.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        current.release();
        current.clearHandle();
        currentKey = null;
    }

    public void StopFadeOut()
    {
        if (!current.hasHandle()) return;
        current.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        current.release();
        current.clearHandle();
        currentKey = null;
    }

    public void PlayLoop(string key, float vol = 1f)
    {
        if (loopPlaying && loopKey == key) return; // 이미 같은 루프 재생 중

        // 기존 루프 중지
        StopLoop();

        // EventDescription 가져오기
        if (!descCache.TryGetValue(key, out var desc))
        {
            string path = basePath + key;
            var getResult = RuntimeManager.StudioSystem.getEvent(path, out desc);
            if (getResult != FMOD.RESULT.OK)
            {
                Debug.LogError($"[SpatialSoundSimple] getEvent failed ({getResult}) : {path}");
                return;
            }
            descCache[key] = desc;
        }

        // 이벤트 자체가 루프 세팅이어야 함 (FMOD에서 Loop Region 지정)
        desc.createInstance(out loop);
        loopKey = key;

        RuntimeManager.AttachInstanceToGameObject(loop, gameObject, rb);
        loop.setVolume(Mathf.Clamp01(vol));

        loop.start();
        loopPlaying = true;
    }
    
    public void StopLoop(bool fadeOut = true)
    {
        if (!loop.hasHandle()) return;

        loop.stop(fadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
        loop.release();
        loop.clearHandle();

        loopKey = null;
        loopPlaying = false;
    }
}