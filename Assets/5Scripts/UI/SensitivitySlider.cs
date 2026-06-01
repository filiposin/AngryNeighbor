using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SensitivitySlider : MonoBehaviour
{
    private Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
        
        // Load saved sensitivity or default to 2.0f
        float currentSens = PlayerPrefs.GetFloat("PlayerSensitivity", 2.0f);
        
        // Update slider value without triggering the onValueChanged event during initialization
        slider.SetValueWithoutNotify(currentSens);

        slider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    public void OnSensitivityChanged(float value)
    {
        // Save the new value
        PlayerPrefs.SetFloat("PlayerSensitivity", value);
        PlayerPrefs.Save();

        // Apply immediately to the player if currently loaded
        FP_CameraLook camLook = Object.FindFirstObjectByType<FP_CameraLook>();
        if (camLook != null)
        {
            camLook.LookSensitivity = value;
        }
    }
}
