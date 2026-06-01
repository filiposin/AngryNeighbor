using UnityEngine;

public enum Difficulty { Easy, Normal, Hard, Nightmare }
public enum CreakyFloorMode { Off, Normal, ExtraLoud }
public enum ShotgunMode { Both, PlayerOnly, NeighborOnly, Disabled }

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance;

    [Header("Current Settings")]
    public Difficulty difficulty = Difficulty.Normal;
    public bool darkerMode = false;
    public CreakyFloorMode floorMode = CreakyFloorMode.Normal;
    public ShotgunMode shotgunMode = ShotgunMode.Both;
    public bool neighborActive = true;
    public int itemPresetIndex = 0; 
    
    // НОВАЯ НАСТРОЙКА: 0=Off, 1=White, 2=Cyan, 3=Red, 4=Green, 5=Orange
    public int outlineIndex = 1; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("Difficulty", (int)difficulty);
        PlayerPrefs.SetInt("DarkerMode", darkerMode ? 1 : 0);
        PlayerPrefs.SetInt("FloorMode", (int)floorMode);
        PlayerPrefs.SetInt("ShotgunMode", (int)shotgunMode);
        PlayerPrefs.SetInt("NeighborActive", neighborActive ? 1 : 0);
        PlayerPrefs.SetInt("ItemPreset", itemPresetIndex);
        PlayerPrefs.SetInt("OutlineIndex", outlineIndex); // Сохраняем цвет
        PlayerPrefs.Save();
        Debug.Log("Settings Saved");
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("Difficulty"))
        {
            difficulty = (Difficulty)PlayerPrefs.GetInt("Difficulty");
            darkerMode = PlayerPrefs.GetInt("DarkerMode") == 1;
            floorMode = (CreakyFloorMode)PlayerPrefs.GetInt("FloorMode");
            shotgunMode = (ShotgunMode)PlayerPrefs.GetInt("ShotgunMode");
            neighborActive = PlayerPrefs.GetInt("NeighborActive") == 1;
            itemPresetIndex = PlayerPrefs.GetInt("ItemPreset");
            outlineIndex = PlayerPrefs.GetInt("OutlineIndex");
        }
    }

    // UI Methods
    public void SetDifficulty(int index) { difficulty = (Difficulty)index; SaveSettings(); }
    public void SetDarkerMode(bool active) { darkerMode = active; SaveSettings(); }
    public void SetFloorMode(int index) { floorMode = (CreakyFloorMode)index; SaveSettings(); }
    public void SetShotgunMode(int index) { shotgunMode = (ShotgunMode)index; SaveSettings(); }
    public void SetNeighborActive(bool active) { neighborActive = active; SaveSettings(); }
    public void SetItemPreset(int index) { itemPresetIndex = index; SaveSettings(); }
    public void SetOutlineIndex(int index) { outlineIndex = index; SaveSettings(); }
}