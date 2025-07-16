using UnityEngine;
using UnityEngine.UI;

public class VoiceSlider : MonoBehaviour
{
    [SerializeField] private Slider voiceSlider;
    private SoundManager soundManager;

    void Start()
    {
        // SoundManager 인스턴스를 찾음
        soundManager = FindObjectOfType<SoundManager>();

        // 저장된 슬라이더 값 불러오기
        float savedVolume = PlayerPrefs.GetFloat("effectVolume", 1.0f); // 기본값은 1.0f
        if (soundManager != null && voiceSlider != null)
        {
            voiceSlider.value = savedVolume; // 저장된 값으로 슬라이더 초기화
            voiceSlider.onValueChanged.AddListener(OnEffectVolumeChange);
        }
    }

    void OnEffectVolumeChange(float value)
    {
        //SoundManager.Instance.voiceBus.setVolume(value);
    }
}