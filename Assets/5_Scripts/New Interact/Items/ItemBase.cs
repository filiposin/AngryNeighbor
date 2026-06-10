using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class ItemBase : MonoBehaviour, IItem
{
    protected ItemDefinition definition;
    public ItemDefinition Definition => definition;

    internal GameObject holder;
    protected Rigidbody rb;
    protected Collider[] colliders;
    protected int collidersCount;
    
    // --- НОВОЕ: Звук ---
    protected AudioSource audioSource;
    private float lastHitTime;

    // --- НОВОЕ: Запоминаем родной размер ---
    public Vector3 InitialScale { get; private set; } 
    private System.Collections.Generic.Dictionary<Transform, int> originalLayers = new System.Collections.Generic.Dictionary<Transform, int>();

    protected virtual void Awake()
    {
        // Сразу запоминаем размер, с которым предмет родился/был создан
        InitialScale = transform.localScale;

        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);
        collidersCount = colliders != null ? colliders.Length : 0;

        // Автоматически настраиваем AudioSource для предмета (если его нет)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Делаем звук полностью трехмерным (3D)
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 15f;
        }
    }

    public virtual void Initialize(ItemDefinition def)
    {
        definition = def;
    }

    // --- НОВОЕ: Статический 2D AudioSource для оптимизации ---
    private static AudioSource shared2DAudioSource;

    public virtual void OnPickup(GameObject holder)
    {
        this.holder = holder;
        
        var worldItem = GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.CancelFreeze();
        }

        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        SetCollidersEnabled(false);

        // Воспроизводим звук подбора предмета (ОПТИМИЗИРОВАНО)
        if (definition != null && definition.pickupSound != null)
        {
            if (shared2DAudioSource == null)
            {
                GameObject audioObj = new GameObject("Shared_2D_AudioSource");
                DontDestroyOnLoad(audioObj);
                shared2DAudioSource = audioObj.AddComponent<AudioSource>();
                shared2DAudioSource.spatialBlend = 0f; // Строго 2D звук
            }
            
            shared2DAudioSource.PlayOneShot(definition.pickupSound, 1f);
        }
    }

    public virtual void OnDrop()
    {
        holder = null;
        if (rb) rb.isKinematic = false;
        SetCollidersEnabled(true);
    }

    public virtual void OnUse() { }

    public virtual void OnThrow(Vector3 velocity)
    {
        holder = null;
        if (rb) rb.isKinematic = false;
        SetCollidersEnabled(true);
        if (rb) rb.velocity = velocity;
    }

    // --- Обработка звука удара о землю ---
    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Если предмет находится в руках, или нет definition, выходим
        if (holder != null || definition == null) return;

        // Защита от спама звуком (чтобы при качении не было пулемета из звуков)
        if (Time.time - lastHitTime < 0.1f) return;

        // Если сила столкновения слишком слабая, игнорируем
        if (collision.relativeVelocity.sqrMagnitude < 0.5f) return;

        lastHitTime = Time.time;
        int hitLayer = collision.gameObject.layer;
        AudioClip clipToPlay = definition.defaultHitGroundSound;

        // Ищем подходящий звук на основе слоя поверхности
        if (definition.specificGroundHitSounds != null && definition.specificGroundHitSounds.Length > 0)
        {
            foreach (var hitSound in definition.specificGroundHitSounds)
            {
                // Проверяем, включен ли слой (hitLayer) в маску слоев (groundLayers)
                if ((hitSound.groundLayers.value & (1 << hitLayer)) != 0)
                {
                    if (hitSound.sound != null)
                    {
                        clipToPlay = hitSound.sound;
                    }
                    break;
                }
            }
        }

        // Если найден какой-то звук - играем его с зависящей от силы удара громкостью
        if (clipToPlay != null && audioSource != null)
        {
            // Чем сильнее предмет ударился, тем громче звук (ограничиваем от 0 до 1)
            float volume = Mathf.Clamp01(collision.relativeVelocity.magnitude / 8f);
            audioSource.PlayOneShot(clipToPlay, volume);
        }
    }

    protected void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < collidersCount; i++)
            colliders[i].enabled = enabled;
    }

    private void SaveLayersRecursively(Transform parent)
    {
        if (parent == null) return;
        originalLayers[parent] = parent.gameObject.layer;
        foreach (Transform child in parent)
        {
            SaveLayersRecursively(child);
        }
    }

    public void SetHandLayer()
    {
        int handLayer = LayerMask.NameToLayer("Hand");
        if (handLayer == -1) return;

        if (gameObject.layer != handLayer)
        {
            originalLayers.Clear();
            SaveLayersRecursively(transform);
        }

        SetLayerRecursively(transform, handLayer);
    }

    public void RestoreLayer()
    {
        foreach (var kvp in originalLayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.gameObject.layer = kvp.Value;
            }
        }
    }

    private void SetLayerRecursively(Transform parent, int layer)
    {
        if (parent == null) return;
        parent.gameObject.layer = layer;
        foreach (Transform child in parent)
        {
            SetLayerRecursively(child, layer);
        }
    }

    public virtual void OnSpawnFromPool() { }
    public virtual void OnReturnToPool() { }
}