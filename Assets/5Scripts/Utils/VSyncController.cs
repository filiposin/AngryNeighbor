using UnityEngine;
using UnityEngine.UI;

public class VSyncController : MonoBehaviour
{
    [Header("UI Elements (Необязательно)")]
    [SerializeField] private Toggle vsyncToggle;

    private void Start()
    {
        // При старте проверяем текущие настройки и выставляем правильное состояние галочки в UI
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            
            // Подписываемся на событие изменения значения Toggle
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }
    }

    // Метод для работы с UI Toggle
    public void SetVSync(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;

        Debug.Log($"VSync изменен на: {(isOn ? "ВКЛ" : "ВЫКЛ")}. Target FPS: {Application.targetFrameRate}");
    }

    // Метод переключения (Toggle) по кнопке или клавише
    public void ToggleVSync()
    {
        bool isCurrentlyOn = QualitySettings.vSyncCount > 0;
        SetVSync(!isCurrentlyOn);

        // Если привязан UI Toggle, обновляем его визуальное состояние
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = !isCurrentlyOn;
        }
    }
}