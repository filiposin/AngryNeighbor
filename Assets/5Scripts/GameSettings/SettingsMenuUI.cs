using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public Dropdown difficultyDropdown;
    public Toggle darkerModeToggle;
    public Dropdown floorDropdown;
    public Dropdown shotgunDropdown;
    public Toggle neighborToggle;

    [Header("Preset Settings")]
    public Slider presetSlider; 
    public Text presetLabel;    

    [Header("Outline Settings")]
    public Slider outlineSlider; 
    public Text outlineLabel;    

    private bool isInitializing = false;

    private void Start()
    {
        if (GameSettingsManager.Instance == null) return;

        // Удаляем старые подписки
        difficultyDropdown.onValueChanged.RemoveAllListeners();
        darkerModeToggle.onValueChanged.RemoveAllListeners();
        floorDropdown.onValueChanged.RemoveAllListeners();
        shotgunDropdown.onValueChanged.RemoveAllListeners();
        neighborToggle.onValueChanged.RemoveAllListeners();
        
        if(presetSlider != null) presetSlider.onValueChanged.RemoveAllListeners();
        if(outlineSlider != null) outlineSlider.onValueChanged.RemoveAllListeners();

        // Добавляем новые
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        darkerModeToggle.onValueChanged.AddListener(OnDarkerToggle);
        floorDropdown.onValueChanged.AddListener(OnFloorChanged);
        shotgunDropdown.onValueChanged.AddListener(OnShotgunChanged);
        neighborToggle.onValueChanged.AddListener(OnNeighborToggle);
        
        if(presetSlider != null) presetSlider.onValueChanged.AddListener(OnPresetChanged);
        if(outlineSlider != null) outlineSlider.onValueChanged.AddListener(OnOutlineChanged);

        InitializeUI();
    }

    private void InitializeUI()
    {
        isInitializing = true;

        var sm = GameSettingsManager.Instance;

        difficultyDropdown.value = (int)sm.difficulty;
        darkerModeToggle.isOn = sm.darkerMode;
        floorDropdown.value = (int)sm.floorMode;
        shotgunDropdown.value = (int)sm.shotgunMode;
        neighborToggle.isOn = sm.neighborActive;

        if (presetSlider != null)
        {
            presetSlider.maxValue = 7;
            presetSlider.value = sm.itemPresetIndex;
            UpdatePresetText((int)sm.itemPresetIndex);
        }

        if (outlineSlider != null)
        {
            outlineSlider.value = sm.outlineIndex;
            UpdateOutlineText(sm.outlineIndex);
        }

        UpdateLockState((Difficulty)difficultyDropdown.value);

        isInitializing = false;
    }

    // --- ЛОГИКА OUTLINE ---
    private void OnOutlineChanged(float val)
    {
        int index = Mathf.RoundToInt(val);
        
        if (outlineLabel != null) UpdateOutlineText(index);
        
        if (isInitializing) return;

        GameSettingsManager.Instance.outlineIndex = index;
        GameSettingsManager.Instance.SaveSettings();
    }

    private void UpdateOutlineText(int index)
    {
        if (outlineLabel == null) return;

        switch (index)
        {
            case 0: outlineLabel.text = "OFF"; break;
            case 1: outlineLabel.text = "White"; break;
            case 2: outlineLabel.text = "Cyan"; break;
            case 3: outlineLabel.text = "Red"; break;
            case 4: outlineLabel.text = "Green"; break;
            case 5: outlineLabel.text = "Orange"; break;
        }
    }

    // --- ОСТАЛЬНЫЕ МЕТОДЫ ---
    private void OnPresetChanged(float val)
    {
        int index = Mathf.RoundToInt(val);
        if (presetLabel != null) UpdatePresetText(index);
        if (isInitializing) return;
        GameSettingsManager.Instance.itemPresetIndex = index;
        GameSettingsManager.Instance.SaveSettings();
    }

    private void UpdatePresetText(int index)
    {
        if (presetLabel == null) return;
        if (index == 0) presetLabel.text = "Preset: Random";
        else presetLabel.text = $"Preset: {index}";
    }

    private void OnDifficultyChanged(int index)
    {
        if (isInitializing) return;
        Difficulty diff = (Difficulty)index;
        GameSettingsManager.Instance.difficulty = diff;
        UpdateLockState(diff);
        GameSettingsManager.Instance.SaveSettings();
    }
    
    private void OnDarkerToggle(bool val) { if(!isInitializing) { GameSettingsManager.Instance.darkerMode = val; GameSettingsManager.Instance.SaveSettings(); }}
    private void OnFloorChanged(int val) { if(!isInitializing) { GameSettingsManager.Instance.floorMode = (CreakyFloorMode)val; GameSettingsManager.Instance.SaveSettings(); }}
    private void OnShotgunChanged(int val) { if(!isInitializing) { GameSettingsManager.Instance.shotgunMode = (ShotgunMode)val; GameSettingsManager.Instance.SaveSettings(); }}
    private void OnNeighborToggle(bool val) { if(!isInitializing) { GameSettingsManager.Instance.neighborActive = val; GameSettingsManager.Instance.SaveSettings(); }}

    // --- БЛОКИРОВКА (ОБНОВЛЕННАЯ) ---
    private void UpdateLockState(Difficulty diff)
    {
        var sm = GameSettingsManager.Instance;
        isInitializing = true; 

        switch (diff)
        {
            case Difficulty.Easy:
            case Difficulty.Normal:
                UnlockUI(darkerModeToggle);
                UnlockUI(neighborToggle);
                UnlockUI(floorDropdown);
                UnlockUI(shotgunDropdown);
                if(presetSlider) presetSlider.interactable = true;
                if(outlineSlider) outlineSlider.interactable = true; // Разблокируем Outline
                break;

            case Difficulty.Hard:
                LockToggle(darkerModeToggle, true); sm.darkerMode = true;
                LockToggle(neighborToggle, true); sm.neighborActive = true;
                UnlockUI(floorDropdown);
                UnlockUI(shotgunDropdown);
                if(presetSlider) presetSlider.interactable = true;
                if(outlineSlider) outlineSlider.interactable = true; // На Харде Outline еще можно менять
                break;

            case Difficulty.Nightmare:
                LockToggle(darkerModeToggle, true); sm.darkerMode = true;
                LockToggle(neighborToggle, true); sm.neighborActive = true;
                LockDropdown(floorDropdown, 2); sm.floorMode = CreakyFloorMode.ExtraLoud;
                LockDropdown(shotgunDropdown, 2); sm.shotgunMode = ShotgunMode.NeighborOnly;

                // Блокируем Пресеты на Random
                if (presetSlider != null)
                {
                    presetSlider.value = 0;
                    presetSlider.interactable = false;
                    UpdatePresetText(0);
                }
                sm.itemPresetIndex = 0;

                // БЛОКИРУЕМ OUTLINE НА OFF (0)
                if (outlineSlider != null)
                {
                    outlineSlider.value = 0; // Ставим OFF
                    outlineSlider.interactable = false; // Запрещаем менять
                    UpdateOutlineText(0); // Обновляем текст
                }
                sm.outlineIndex = 0; // Записываем в настройки
                break;
        }
        isInitializing = false;
    }

    private void UnlockUI(Selectable ui) { if (ui != null) ui.interactable = true; }
    private void LockToggle(Toggle t, bool v) { if (t != null) { t.isOn = v; t.interactable = false; } }
    private void LockDropdown(Dropdown d, int v) { if (d != null) { d.value = v; d.interactable = false; } }
}