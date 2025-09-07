using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class BGMSlider : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    private SoundManager soundManager;
    private Bus bgmBus;

    void Start()
    {
        // SoundManager 인스턴스를 찾음
        soundManager = FindObjectOfType<SoundManager>();

        // 저장된 슬라이더 값 불러오기
        float savedVolume = PlayerPrefs.GetFloat("bgmVolume", .75f); // 기본값은 .75f
        if (soundManager != null && bgmSlider != null)
        {
            bgmSlider.value = savedVolume; // 저장된 값으로 슬라이더 초기화
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }

        bgmBus = RuntimeManager.GetBus("bus:/BGM");
    }

    void OnBGMSliderChanged(float value)
    {
        if (soundManager != null)
        {
            bgmBus.setVolume(value);
        }

        PlayerPrefs.SetFloat("bgmVolume", value);
        PlayerPrefs.Save();
    }
}