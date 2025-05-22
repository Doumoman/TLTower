using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class SoundPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    public string ClipName
    {
        get
        {
            return audioSource.clip.name;
        }
    }

    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        TickManager Tick = GetComponent<TickManager>();
        Tick.OnTickEvent += TickEvent; //OnTickEvent 
    }
    private void TickEvent(object sender, System.EventArgs eventArgs)
    {

    }
    public void InitSound(AudioClip clip)
    {
        audioSource.clip = clip;
    }

    public void Play(AudioMixerGroup audioMixer, bool isLoop = true, int loopTicks = 0)
    {
        audioSource.outputAudioMixerGroup = audioMixer;
        if (loopTicks != 0) { StartCoroutine(Looping(loopTicks)); } // 틱 단위 루프
        else audioSource.loop = isLoop; //그냥 루프 or one-shot

        if (isLoop) StartCoroutine(OneShot(audioSource.clip.length));
    }

    private IEnumerator Looping(int loopTicks)
    {
        TickManager tickManager = FindObjectOfType<TickManager>();
        yield return new WaitForSeconds(tickManager.Tick * loopTicks);
    }

    private IEnumerator OneShot(float length) {
        yield return new WaitForSeconds(length);
        Destroy(gameObject);
    }
}
