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
    public Sprite icon;
    public GameObject itemPrefab;

    [Header("Настройки предмета в руках игрока")]
    public Vector3 holdPosition = Vector3.zero;
    public Vector3 holdEulerRotation = Vector3.zero;
    
    public float throwForce = 8f;
    public float spawnDistanceFromCamera = 0.6f;

    [Header("Геймпейные настройки")]
    public float useCooldown = 0.2f;
    public bool tpBackAfterDeath = true;
    public bool consumeOnThrow = true;
    public bool uniqueInInventory = false;

    [Header("Звуки")]
    public AudioClip pickupSound;
    public AudioClip defaultHitGroundSound;
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