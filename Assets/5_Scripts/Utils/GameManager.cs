using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject pause;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject PauseCanvas;

    public static GameManager Instance;

    public event Action<bool> OnSettingsToggled;

    public bool IsSettingsOpen => settings != null && settings.activeSelf;
    public bool IsPauseOpen => pause != null && pause.activeSelf;

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
        UpdatePlayerControl();
    }

    public void SetSettings(bool isOn)
    {
        if (settings) settings.SetActive(isOn);
        
        OnSettingsToggled?.Invoke(isOn);
        UpdatePlayerControl();

        if (!isOn && _openedFromInventory)
        {
            _openedFromInventory = false;
            
            SetPause(false);

            if (PlayerItemHandler.inst != null)
            {
                PlayerItemHandler.inst.SetInventoryOpen(true);
            }
        }
    }

    public void OpenSettingsFromInventory()
    {
        _openedFromInventory = true;

        if (PlayerItemHandler.inst != null)
        {
            PlayerItemHandler.inst.SetInventoryOpen(false);
        }

        SetSettings(true);
        SetPause(true);
    }

    private void UpdatePlayerControl()
    {
        if (PlayerItemHandler.inst != null)
        {
            PlayerItemHandler.inst.SetExternalUIOpen(IsSettingsOpen || IsPauseOpen);
        }
    }
}