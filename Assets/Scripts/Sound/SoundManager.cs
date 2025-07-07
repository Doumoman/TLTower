using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
public enum Sound
{
    Bgm, //loop 안됨. 자세한 재생은 SoundPlayer에서 따로 지정.
    Sfx,
    Voice, //보살 대사
    Ambience, //Ambience 사운드들로로 SFX 아니고 BGM에 들어감!

    MaxCount
}

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance = null;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
            }
            if (!_initialized) instance.Init();
            return instance;
        }
    }
    private static bool _initialized = false; 
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        // 만약 플레이어 프렙스에 저장된 bgm과 effect의 Volume값이 있다면 불러온다. 게임이 꺼졌다 켜져도 전의 값을 유지하기 위함.
        if (!PlayerPrefs.HasKey("bgmVolume")) PlayerPrefs.SetFloat("bgmVolume", 1.0f);
        if (!PlayerPrefs.HasKey("effectVolume")) PlayerPrefs.SetFloat("effectVolume", 1.0f);
        if (!PlayerPrefs.HasKey("voiceVolume")) PlayerPrefs.SetFloat("voiceVolume", 1.0f);

        Init();
    }
    private AudioSource[] bgmTracks = new AudioSource[15]; // bgm은 루프되지 않음! 현재 재생할 bgm 소스들을 15개까지 큐잉해서 사용


    AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];
    Dictionary<string, AudioClip> _audioClips = new();

    public AudioMixer audioMixer;

    public float currentBGMVolume { get; set; }
    public float currentEffectVolume { get; set; }
    public float currentVoiceVolume { get; set; }

    public void Init()
    {
        currentBGMVolume = PlayerPrefs.GetFloat("bgmVolume");
        currentEffectVolume = PlayerPrefs.GetFloat("effectVolume");
        currentVoiceVolume = PlayerPrefs.GetFloat("voiceVolume");
        audioMixer = Resources.Load<AudioMixer>("Mixer");
        AudioMixerGroup[] audioMixerGroups = audioMixer.FindMatchingGroups("Master");

        //GameObject root = GameObject.Find("@Sound");
        //root = new GameObject { name = "@Sound" };
        GameObject root = this.gameObject;
        //root.AddComponent<SoundManager>();
        //Object.DontDestroyOnLoad(root);

        string[] soundNames = System.Enum.GetNames(typeof(Sound));
        for (int i = 0; i < soundNames.Length - 1; i++)
        {
            GameObject go = new GameObject { name = soundNames[i] };
            _audioSources[i] = go.AddComponent<AudioSource>();
            go.transform.parent = root.transform;

            // Ambience는 BGM 그룹으로 묶기, loop 켜기
            if ((Sound)i == Sound.Ambience)
            {
                _audioSources[i].outputAudioMixerGroup = audioMixerGroups[1]; // BGM
                _audioSources[i].loop = true;
            }
            else
                _audioSources[i].outputAudioMixerGroup = audioMixerGroups[i + 1]; // SFX, Voice               
        }
        _audioSources[(int)Sound.Ambience].loop = true;

        for (int i = 0; i < bgmTracks.Length; i++)
        {
            bgmTracks[i] = gameObject.AddComponent<AudioSource>();
            bgmTracks[i].outputAudioMixerGroup = audioMixer.FindMatchingGroups("BGM")[0];
        }
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
        {
            audioSource.clip = null;
            audioSource.Stop();
        }
        _audioClips.Clear();
    }
    public void Play(AudioClip audioClip, Sound type = Sound.Sfx, float pitch = 1.0f)
    {
        if (audioClip == null)
        {
            Debug.Log("AudioClip is null");
            return;
        }
        if (_audioSources[(int)type] == null)
        {
            Debug.Log($"AudioSource for {type} is null");
        }
        if (type == Sound.Bgm)
        {
            PlayBGM(audioClip);
        }
        else if (type == Sound.Ambience)
        {
            AudioSource audioSource = _audioSources[(int)Sound.Ambience];
            if (audioSource.isPlaying)
                audioSource.Stop();
            audioSource.pitch = pitch;
            audioSource.clip = audioClip;
            audioSource.volume = PlayerPrefs.GetFloat("bgmVolume");
            audioSource.Play();
        }
        else
        {
            AudioSource audioSource = _audioSources[(int)Sound.Sfx];

            audioSource.pitch = pitch;
            audioSource.volume = PlayerPrefs.GetFloat("effectVolume");
            audioSource.PlayOneShot(audioClip);
        }
    }
    public void Play(string path, Sound type = Sound.Sfx, float pitch = 1.0f)
    {
        AudioClip audioClip = GetOrAddAudioClip(path, type);
        Play(audioClip, type, pitch);
    }

    public void PlayBGM(AudioClip audioClip)
    {
        Debug.Log($"BGM Playing : {audioClip}");
        for (int i = 0; i < bgmTracks.Length; i++)
        {
            if (!bgmTracks[i].isPlaying)
            {
                bgmTracks[i].clip = audioClip;
                bgmTracks[i].volume = PlayerPrefs.GetFloat("bgmVolume");
                bgmTracks[i].Play();
                return;
            }
        }
    }
    AudioClip GetOrAddAudioClip(string path, Sound type = Sound.Sfx)
    {
        if (path.Contains("Sounds/") == false)
            path = $"Sounds/{path}";
        AudioClip audioClip = null;

        if (type == Sound.Bgm)
        {
            audioClip = GameManager.Resource.Load<AudioClip>(path);
        }
        else
        {
            if (_audioClips.TryGetValue(path, out audioClip) == false)
            {
                audioClip = GameManager.Resource.Load<AudioClip>(path);
                _audioClips.Add(path, audioClip);
            }
        }

        if (audioClip == null)
            Debug.Log($"AudioClip Missing : {path}");

        return audioClip;
    }
    public void Stop(string clip = "")
    {
        if (clip == "")
        {
            foreach (var src in _audioSources) src.Stop();
            foreach (var src in bgmTracks) src.Stop();
            return;
        }
        if (System.Enum.TryParse(clip, out Sound soundType))
        {
            if (soundType == Sound.Bgm)
                foreach (var src in bgmTracks) src.Stop();
            else _audioSources[(int)soundType].Stop();
            return;
        }
        foreach (var src in bgmTracks)
        {
            if (src.clip != null && src.clip.name == clip)
            {
                src.Stop();
                return;
            }
        }

        foreach (var src in _audioSources)
        {
            if (src.clip != null && src.clip.name == clip)
            {
                src.Stop();
                return;
            }
        }

        Debug.LogWarning($"Stop AudioClip Missing : {clip}");
    }

    public void BgmOff(string path)
    {
        for (int i = 0; i < bgmTracks.Length; i++) bgmTracks[i].Stop();
    }

    //옵션창 음향 슬라이더에서 값 변경시 오디오소스의 볼륨을 조절하고 이 값을 플레이어 프렙스에 저장
    public void OnBgmVolumeChange(float volume)
    {
        for (int i = 0; i < bgmTracks.Length; i++) bgmTracks[i].volume = volume;
        _audioSources[(int)Sound.Ambience].volume = volume;
        PlayerPrefs.SetFloat("bgmVolume", volume);

    }
    public void OnEffectVolumeChange(float volume)
    {
        _audioSources[(int)Sound.Sfx].volume = volume;
        PlayerPrefs.SetFloat("effectVolume", volume);
    }
    public void OnVoiceVolumeChange(float volume)
    {
        _audioSources[(int)Sound.Voice].volume = volume;
        PlayerPrefs.SetFloat("voiceVolume", volume);
    }

    // 원하는 곳에 효과음 추가 위한 함수
    // SoundManager.Instance.PlayOneShot("Walk")와 같이 사용
    public void PlayOneShot(string effectName)
    {
        var source = _audioSources[(int)Sound.Sfx];
        string effect = "Sounds/" + effectName;
        AudioClip effectClip = Resources.Load<AudioClip>(effect);
        source.volume = PlayerPrefs.GetFloat("effectVolume"); // 플레이어프렙스에서 effectVolume 값 가져오기
        source.clip = effectClip;
        source.PlayOneShot(effectClip);
    }
}