using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    private const string PrefKey = "MasterVolume";
    [SerializeField]private Slider _slider;

    private void Awake()
    {

        float savedVolume = PlayerPrefs.GetFloat(PrefKey, 1f);
        _slider.value = savedVolume;
        AudioListener.volume = savedVolume;

        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PrefKey, value);
    }

    private void OnDestroy()
    {
        _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }
}
