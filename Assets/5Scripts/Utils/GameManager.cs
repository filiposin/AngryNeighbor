using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject pause;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject PauseCanvas;

    public static GameManager Instance;

    // Событие, которое будет сообщать другим скриптам, что настройки открылись/закрылись
    public event Action<bool> OnSettingsToggled;

    // Переменная-память: были ли настройки открыты из инвентаря?
    private bool _openedFromInventory = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            DontDestroyOnLoad(PauseCanvas);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetSettings(false);
        SetPause(false);
    }

    public void SetPause(bool isPaused)
    {
        if (pause) pause.SetActive(isPaused);
    }

    public void SetSettings(bool isOn)
    {
        if (settings) settings.SetActive(isOn);
        
        // Вызываем событие для меню (чтобы скрыть/показать кнопки и логотип)
        OnSettingsToggled?.Invoke(isOn);

        // Если настройки ЗАКРЫВАЮТСЯ, и они были открыты из инвентаря:
        if (!isOn && _openedFromInventory)
        {
            _openedFromInventory = false; // сбрасываем память
            
            // Открываем инвентарь обратно
            if (PlayerItemHandler.inst != null)
            {
                PlayerItemHandler.inst.SetInventoryOpen(true);
            }
        }
    }

    // НОВЫЙ МЕТОД: Вызывать только при нажатии на кнопку настроек В ИНВЕНТАРЕ
    public void OpenSettingsFromInventory()
    {
        _openedFromInventory = true; // Запоминаем

        // Закрываем инвентарь
        if (PlayerItemHandler.inst != null)
        {
            PlayerItemHandler.inst.SetInventoryOpen(false);
        }

        // Открываем настройки и ставим паузу
        SetSettings(true);
        SetPause(true);
    }
}