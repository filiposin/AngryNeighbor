using UnityEngine;
using System.Collections.Generic;

public class LevelInitializer : MonoBehaviour
{
    [Header("Manual References")]
    public RichAI_EnemyController enemy; 
    public GameObject shotgunWorldItem;
    
    [Header("Item Presets")]
    public GameObject[] itemPresets;

    private FP_Controller player;
    private List<ItemAmmoBox> ammoBoxes = new List<ItemAmmoBox>();

    void Start()
    {
        player = FindObjectOfType<FP_Controller>();
        ItemAmmoBox[] boxes = FindObjectsOfType<ItemAmmoBox>();
        ammoBoxes.AddRange(boxes);
        if (enemy == null) enemy = FindObjectOfType<RichAI_EnemyController>();

        if (GameSettingsManager.Instance == null)
        {
            GameObject temp = new GameObject("TempSettings");
            temp.AddComponent<GameSettingsManager>();
        }

        ApplySettings();
    }

    void ApplySettings()
    {
        var settings = GameSettingsManager.Instance;

        ApplyItemPresets(settings.itemPresetIndex);

        if (enemy != null)
        {
            if (!settings.neighborActive) enemy.gameObject.SetActive(false);
            else
            {
                enemy.gameObject.SetActive(true);
                ApplyDifficulty(settings.difficulty);
            }
        }

        if (settings.darkerMode)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.12f;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.05f);
        }
        else RenderSettings.fogDensity = 0.01f; 

        ApplyCreakyFloors(settings.floorMode);
        ApplyShotgunRules(settings.shotgunMode);
        
        // --- НОВОЕ: ПРИМЕНЕНИЕ OUTLINE ---
        ApplyOutline(settings.outlineIndex);
    }

    void ApplyOutline(int index)
    {
        // Находим ВСЕ скрипты Outline на сцене (даже если они выключены)
        // true в параметрах FindObjectsOfType означает "includeInactive"
        Outline[] outlines = FindObjectsOfType<Outline>(true); 

        // Определяем цвет
        Color targetColor = Color.white;
        bool shouldEnable = true;

        switch (index)
        {
            case 0: shouldEnable = false; break; // OFF
            case 1: targetColor = new Color32(123, 123, 123, 255); break; // White (Grey)
            case 2: targetColor = Color.cyan; break;
            case 3: targetColor = Color.red; break;
            case 4: targetColor = Color.green; break;
            case 5: targetColor = new Color(1.0f, 0.64f, 0.0f); break; // Orange (Unity default doesn't have it)
        }

        foreach (var outline in outlines)
        {
            if (!shouldEnable)
            {
                outline.enabled = false;
            }
            else
            {
                outline.enabled = true;
                outline.OutlineColor = targetColor;
                // outline.VisibleThroughWalls = true; // Можно принудительно включить, если надо
            }
        }
    }

    void ApplyItemPresets(int indexSetting)
    {
        if (itemPresets == null || itemPresets.Length == 0) return;

        int indexToActivate = -1;

        if (indexSetting == 0)
        {
            int randomNum = Random.Range(1, itemPresets.Length + 1); 
            Debug.Log($"Preset Mode: Random. Chosen Preset: {randomNum}");
            indexToActivate = randomNum;
        }
        else
        {
            indexToActivate = indexSetting;
            Debug.Log($"Preset Mode: Specific. Preset: {indexToActivate}");
        }

        for (int i = 0; i < itemPresets.Length; i++)
        {
            if ((i + 1) == indexToActivate)
            {
                if(itemPresets[i] != null) itemPresets[i].SetActive(true);
            }
            else
            {
                if (itemPresets[i] != null) Destroy(itemPresets[i]); 
            }
        }
    }

    void ApplyDifficulty(Difficulty diff)
    {
        switch (diff)
        {
            case Difficulty.Easy:
                enemy.walkSpeed = 2.0f; enemy.chaseSpeed = 4.7f; enemy.detectionRange = 15f;
                if(player) player.walkSpeed = 7.0f; player.runSpeed = 10.5f;
                break;
            case Difficulty.Normal:
                enemy.walkSpeed = 2.5f; enemy.chaseSpeed = 6.0f; enemy.detectionRange = 30f;
                if(player) player.walkSpeed = 6.0f; player.runSpeed = 9.5f;
                break;
            case Difficulty.Hard:
                enemy.walkSpeed = 3.5f; enemy.chaseSpeed = 7.0f; enemy.detectionRange = 45f;
                if(player) player.walkSpeed = 5.5f; player.runSpeed = 9.0f;
                break;
            case Difficulty.Nightmare:
                enemy.walkSpeed = 5.0f; enemy.chaseSpeed = 8.5f; enemy.detectionRange = 60f;
                if(player) player.walkSpeed = 5.0f; player.runSpeed = 9.0f;
                break;
        }
    }

    void ApplyCreakyFloors(CreakyFloorMode mode)
    {
        CreakingShit[] floors = FindObjectsOfType<CreakingShit>();
        foreach (var floor in floors)
        {
            switch (mode)
            {
                case CreakyFloorMode.Off: floor.gameObject.SetActive(false); break;
                case CreakyFloorMode.Normal:
                    if (floor.isExtraFloor) floor.gameObject.SetActive(false);
                    else { floor.gameObject.SetActive(true); floor.soundRadius = 15f; }
                    break;
                case CreakyFloorMode.ExtraLoud: floor.gameObject.SetActive(true); floor.soundRadius = 25f; break;
            }
        }
    }

    void ApplyShotgunRules(ShotgunMode mode)
    {
        if (shotgunWorldItem == null) return;

        switch (mode)
        {
            case ShotgunMode.Both:
                shotgunWorldItem.SetActive(true);
                foreach(var box in ammoBoxes) if(box) box.gameObject.SetActive(true);
                break;
            case ShotgunMode.PlayerOnly:
                shotgunWorldItem.SetActive(true);
                foreach(var box in ammoBoxes) if(box) box.gameObject.SetActive(true);
                break;
            case ShotgunMode.NeighborOnly:
                shotgunWorldItem.SetActive(true);
                foreach(var box in ammoBoxes) if(box) box.gameObject.SetActive(false); 
                break;
            case ShotgunMode.Disabled:
                shotgunWorldItem.SetActive(false);
                foreach(var box in ammoBoxes) if(box) box.gameObject.SetActive(false);
                break;
        }
    }
}
