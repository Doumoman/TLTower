using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public enum SoundType
{
    SFX,
    AMB,
    BEAT,
    PAD,
    MELODY,
    OTHERS,
}

[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class SoundManager : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioMixer Mixer;
    [SerializeField] private SoundList[] AllSounds;


    public float BgmVolume, SfxVolume; // 옵션에서 BGM과 SFX의 볼륨을 조작
    private float AmbVolume, BeatVolume, PadVolume, MelVolume; //내부 조작! 나중에 다른 Manager에서 필터 걸거나 볼륨 조절할 때 사용


    private Dictionary<string, AudioClip> AudioDict; //저장된 사운드 목록
    private List<SoundPlayer> CurrentSounds; //현재 생성된 SoundPlayer의 목록
    public static SoundManager instance;
    private void Awake()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize(ref AllSounds, names.Length);

        for (int i = 0; i < AllSounds.Length; i++)
        {
            AllSounds[i].name = names[i];
        }

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }

        else Destroy(instance);
    }//싱글톤 패턴

    private void Start()
    {
        TickManager Tick = GetComponent<TickManager>();
        Tick.OnTickEvent += TickEvent; //OnTickEvent 

        AudioDict = new Dictionary<string, AudioClip>();
        foreach (SoundList soundList in AllSounds)
        {
            foreach (AudioClip clip in soundList.Sounds)
            {
                AudioDict.Add(clip.name, clip);
            }
        }

        CurrentSounds = new List<SoundPlayer>();
    }
    private void TickEvent(object sender, System.EventArgs eventArgs)
    {

    }
    private AudioClip GetClip(string clipName)
    {
        AudioClip clip = AudioDict[clipName];
        return clip;
    }//사운드 이름으로 찾아서 반환하기

    public void Stop(string clipName)
    {
        foreach (SoundPlayer audioPlayer in CurrentSounds)
        {
            if (audioPlayer.ClipName == clipName)
            {
                CurrentSounds.Remove(audioPlayer);
                Destroy(audioPlayer.gameObject);
            }
        }
    } //현재 재생중인 사운드 종료

    public void Play(string clipName, SoundType type = 0, int loopTicks = 0)
    {
        switch (type)
        {
            case SoundType.SFX: //1회 실행
                {
                    GameObject obj = new GameObject("SoundPlayer");
                    SoundPlayer soundPlayer = obj.AddComponent<SoundPlayer>();
                    soundPlayer.InitSound(GetClip(clipName));
                    soundPlayer.Play(Mixer.FindMatchingGroups("SFX")[0], false);
                    break;
                }
            case SoundType.AMB: //tick에 상관없이 재생. tick에 상관없이 loop
                {
                    GameObject obj = new GameObject("SoundPlayer");
                    SoundPlayer soundPlayer = obj.AddComponent<SoundPlayer>();
                    soundPlayer.InitSound(GetClip(clipName));
                    soundPlayer.Play(Mixer.FindMatchingGroups("AMB")[0], true);
                    break;
                }
            case SoundType.BEAT: //tick에 맞춰서 재생, loopTick 틱마다 틱 맞춰서 loop
                TickPlay(clipName, type, loopTicks);
                break;
            case SoundType.PAD: //tick에 맞춰서 재생, loopTick 틱마다 틱 맞춰서 loop
                TickPlay(clipName, type, loopTicks);
                break;
            case SoundType.MELODY: //tick에 맞춰서 재생, loopTick 틱마다 맞춰서 loop
                TickPlay(clipName, type, loopTicks);
                break;
            default: //tick 상관 없이 실행 후 loop
                {
                    GameObject obj = new GameObject("SoundPlayer");
                    SoundPlayer soundPlayer = obj.AddComponent<SoundPlayer>();
                    soundPlayer.InitSound(GetClip(clipName));
                    soundPlayer.Play(Mixer.FindMatchingGroups("BGM")[0], true);
                    break;
                }
        }
    }
    /*
    그냥 Play시 Tick에 상관없이 한 번 재생하고 끝
    type을 명시한다면 SoundPlayer에서 sound를 type에 맞게 재생
    loopTicks를 적는다면 loopTicks마다 재생
    */

    /*
    tick을 기다림에 관한 함수들
    tick이 시작되기 전까지 호출된 함수들의 string을 저장해서 
    */
    private void TickPlay(string clipName, SoundType type, int loopTicks)
    {
        WaitingPlay[clipName] = (type, loopTicks);
    }

    private Dictionary<string, (SoundType type, int loop)> WaitingPlay = new();

    private void TickEvent()
    {
        foreach (var v in WaitingPlay)
        {
            string name = v.Key;
            SoundType type = v.Value.type;
            int loop = v.Value.loop;
            bool val = false;

            switch (type) {
                case SoundType.BEAT:
                    {
                        val = true;
                        break;
                    }
                case SoundType.PAD:
                    {
                        val = true;
                        break;
                    }
                case SoundType.MELODY:
                    {
                        val = true;
                        break;
                    }
                default:
                    {
                        val = false;
                        break;
                    }
            }

            GameObject obj = new GameObject("SoundPlayer");
            SoundPlayer soundPlayer = obj.AddComponent<SoundPlayer>();
            soundPlayer.InitSound(GetClip(name));
            soundPlayer.Play(Mixer.FindMatchingGroups("AMB")[0], val);
            CurrentSounds.Add(soundPlayer);
        }
        WaitingPlay.Clear();
    }

    [Serializable]
    public struct SoundList
    {
        [HideInInspector] public string name;
        [SerializeField] public AudioClip[] Sounds;
    }
}