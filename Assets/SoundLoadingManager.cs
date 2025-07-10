using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundLoadingManager : MonoBehaviour
{
    public static SoundLoadingManager Instance { get; private set; }

    public void PreloadAllBGM()
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds/BGM");
        foreach (var clip in clips)
            if (clip != null)
                SoundManager.Instance.CacheClip(clip.name, clip);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// 한 개의 사운드를 비동기로 로드하여 SoundManager에 등록
    /// </summary>
    public IEnumerator PreloadSound(string clipPath, Sound type = Sound.Sfx)
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager not ready");
            yield break;
        }

        if (!clipPath.StartsWith("Sounds/"))
            clipPath = "Sounds/" + clipPath;

        if (SoundManager.Instance.HasClip(clipPath))
        {
            Debug.Log($"[PreloadManager] Already cached: {clipPath}");
            yield break;
        }

        ResourceRequest req = Resources.LoadAsync<AudioClip>(clipPath);
        yield return req;

        AudioClip clip = req.asset as AudioClip;
        if (clip == null)
        {
            Debug.LogWarning($"[PreloadManager] Failed to load: {clipPath}");
            yield break;
        }

        SoundManager.Instance.CacheClip(clipPath, clip);
        Debug.Log($"[PreloadManager] Cached: {clipPath}");
    }

    /// <summary>
    /// 여러 개를 한 번에 로드
    /// </summary>
    public IEnumerator PreloadSounds(List<string> clipPaths, Sound type = Sound.Sfx)
    {
        foreach (var path in clipPaths)
        {
            yield return StartCoroutine(PreloadSound(path, type));
        }
    }
}
