using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class SFXSlider : MonoBehaviour
{
    [SerializeField] private Slider sfxSlider;
    private SoundManager soundManager;
    private Bus sfxBus;

    void Start()
    {
        // SoundManager 인스턴스를 찾음
        soundManager = FindObjectOfType<SoundManager>();

        // 저장된 슬라이더 값 불러오기
        float savedVolume = PlayerPrefs.GetFloat("sfxVolume", .75f); // 기본값은 .75f
        if (soundManager != null && sfxSlider != null)
        {
            sfxSlider.value = savedVolume; // 저장된 값으로 슬라이더 초기화
            sfxSlider.onValueChanged.AddListener(OnEffectVolumeChange);
        }

        sfxBus = RuntimeManager.GetBus("bus:/SFX");
    }

    void OnEffectVolumeChange(float value)
    {
        if (soundManager != null)
            sfxBus.setVolume(value);
        PlayerPrefs.SetFloat("sfxVolume", value);
    }
}
