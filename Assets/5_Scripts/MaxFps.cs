using UnityEngine;
using UnityEngine.UI;

public class MaxFps : MonoBehaviour
{
    [SerializeField]private Text FpsText;
    [SerializeField]private Slider FpsSlider;
    private void Start() {
        UpdateSlider();
    }
    public void UpdateSlider() 
    {
        if(FpsSlider.value == 0) {
            FpsText.text = "30";
            Application.targetFrameRate = 30;
            //QualitySettings.vSyncCount = 0;
        }
        else if(FpsSlider.value == 1) {
            FpsText.text = "60";
            Application.targetFrameRate = 60;
            //QualitySettings.vSyncCount = 0;
        }
        else if(FpsSlider.value == 2) {
            FpsText.text = "90";
            Application.targetFrameRate = 90;
            QualitySettings.vSyncCount = 0;
        }
        else if(FpsSlider.value == 3) {
            FpsText.text = "120";
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
        }
        else if(FpsSlider.value == 4) {
            FpsText.text = "∞";
            Application.targetFrameRate = 999;
            QualitySettings.vSyncCount = 0;
        }
    }
}
