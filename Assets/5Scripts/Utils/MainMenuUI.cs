using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("Объект, который нужно скрыть (Логотип, кнопки Play, Quit)")]
    [SerializeField] private GameObject startPanel;

    private void Start()
    {
        // Подписываемся на событие GameManager при запуске сцены
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSettingsToggled += HandleSettingsState;
        }
    }

    private void OnDestroy()
    {
        // Обязательно отписываемся при переходе на другую сцену, чтобы не было ошибок
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSettingsToggled -= HandleSettingsState;
        }
    }

    private void HandleSettingsState(bool isSettingsOpen)
    {
        // Если настройки открыты -> прячем меню. Если закрыты -> показываем.
        if (startPanel != null)
        {
            startPanel.SetActive(!isSettingsOpen);
        }
    }
}