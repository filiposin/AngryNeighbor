using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    public ItemDefinition definition;
    private GameObject spawned;
    private PoolManager pool;

    void Start()
    {
        pool = PoolManager.Instance;
        if (definition == null) return;
        if (definition.itemPrefab != null)
            pool.WarmPool(definition.itemPrefab, Mathf.Max(1, definition.poolSize));
        Spawn();
    }

    public void Spawn()
    {
        if (spawned != null) return;

        if (definition == null || definition.itemPrefab == null) return;

        spawned = pool.GetFromPool(definition.itemPrefab, transform.position, transform.rotation);

        var world = spawned.GetComponent<WorldItem>();
        if (world != null)
        {
            world.itemDefinition = definition;
            world.InitializeFromDefinition();
        }
        else
        {
            var ib = spawned.GetComponent<ItemBase>();
            if (ib != null) ib.Initialize(definition);
        }
    }

    public void Despawn()
    {
        if (spawned == null || definition == null || definition.itemPrefab == null) return;
        pool.ReturnToPool(definition.itemPrefab, spawned);
        spawned = null;
    }
}