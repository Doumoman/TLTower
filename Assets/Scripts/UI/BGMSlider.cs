using UnityEngine;
using UnityEngine.UI;

public class BGMSlider : MonoBehaviour
{
    [SerializeField] private Slider sfxSlider;

    private const string PlayerPrefsKey = "BGMVolume";

    void Start()
    {
        // 저장된 값 불러오기 (없으면 1.0f)
        float savedVolume = PlayerPrefs.GetFloat(PlayerPrefsKey, 1.0f);

        // 슬라이더 초기화
        if (sfxSlider != null)
        {
            sfxSlider.value = savedVolume;
            sfxSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // FMOD에 초기 볼륨 적용
        SoundManager.Instance?.SetBGMVolume(savedVolume);
    }

    private void OnSliderValueChanged(float value)
    {
        // 볼륨 적용
        SoundManager.Instance?.SetBGMVolume(value);

        // 저장
        PlayerPrefs.SetFloat(PlayerPrefsKey, value);
        PlayerPrefs.Save();
    }
}