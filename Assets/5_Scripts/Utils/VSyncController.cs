using UnityEngine;
using UnityEngine.UI;

public class VSyncController : MonoBehaviour
{
    [SerializeField] private Toggle vsyncToggle;
    private void Start()
    {
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }
    }
    public void SetVSync(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;
    }
    public void ToggleVSync()
    {
        bool isCurrentlyOn = QualitySettings.vSyncCount > 0;
        SetVSync(!isCurrentlyOn);
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = !isCurrentlyOn;
        }
    }
}