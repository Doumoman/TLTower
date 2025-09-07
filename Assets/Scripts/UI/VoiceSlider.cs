using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class VoiceSlider : MonoBehaviour
{
    [SerializeField] private Slider voiceSlider;
    private SoundManager soundManager;
    private Bus voiceBus;
    void Start()
    {
        // SoundManager 인스턴스를 찾음
        soundManager = FindObjectOfType<SoundManager>();

        // 저장된 슬라이더 값 불러오기
        float savedVolume = PlayerPrefs.GetFloat("VoiceVolume", .75f); // 기본값은 .75f
        if (soundManager != null && voiceSlider != null)
        {
            voiceSlider.value = savedVolume; // 저장된 값으로 슬라이더 초기화
            voiceSlider.onValueChanged.AddListener(OnEffectVolumeChange);
        }

        voiceBus = RuntimeManager.GetBus("bus:/SFX");
    }

    void OnEffectVolumeChange(float value)
    {
        if (soundManager != null)
            voiceBus.setVolume(value);
        PlayerPrefs.SetFloat("VoiceVolume", value);
        PlayerPrefs.Save();
    }
}