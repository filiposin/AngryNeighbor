using UnityEngine;

// Структура для связывания слоев физических объектов со звуками
[System.Serializable]
public struct GroundHitSound
{
    [Tooltip("Слои земли/объектов, для которых будет проигрываться этот звук")]
    public LayerMask groundLayers;
    public AudioClip sound;
}

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Interaction/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;

    [Tooltip("Prefab MUST have a component inheriting ItemBase")]
    public GameObject itemPrefab;

    [Header("Player Hold Settings")]
    public Vector3 holdPosition = Vector3.zero;
    public Vector3 holdEulerRotation = Vector3.zero;
    
    public float throwForce = 8f;
    public float spawnDistanceFromCamera = 0.6f;

    [Header("Pooling")]
    public int poolSize = 6;

    [Header("Optional gameplay")]
    public float useCooldown = 0.2f;

    [Header("Sounds")]
    public AudioClip pickupSound;
    [Tooltip("Звук при падении предмета на любую поверхность (по умолчанию)")]
    public AudioClip defaultHitGroundSound;
    [Tooltip("Специфичные звуки для разных слоев земли (например, для травы, бетона и т.д.)")]
    public GroundHitSound[] specificGroundHitSounds;

    [Header("Internal")]
    [HideInInspector] public string guid;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(guid))
        {
            guid = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
#endif

    [Header("Item Animations")]
    public string anim_EquipState = "Equip"; 
    public string anim_IdleState = "Idle_Handgun";
    public string anim_UseState = "Use"; // <-- Имя анимации выстрела игрока
    public string anim_DropState = "Drop";
}