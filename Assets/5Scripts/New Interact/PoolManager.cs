using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Pool storage per prefab
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    public void WarmPool(GameObject prefab, int count, Transform parent = null)
    {
        if (prefab == null || count <= 0) return;
        if (!pools.ContainsKey(prefab)) pools[prefab] = new Queue<GameObject>();

        var q = pools[prefab];
        for (int i = 0; i < count; i++)
        {
            var o = Instantiate(prefab, parent);
            o.SetActive(false);
            q.Enqueue(o);
        }
    }

    public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        if (!pools.ContainsKey(prefab) || pools[prefab].Count == 0)
        {
            // lazy create one
            var inst = Instantiate(prefab, position, rotation, parent);
            return inst;
        }
        var obj = pools[prefab].Dequeue();
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // call OnSpawnFromPool if available
        var itemBase = obj.GetComponent<ItemBase>();
        if (itemBase != null) itemBase.OnSpawnFromPool();

        return obj;
    }

    public void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (prefab == null || obj == null) { Destroy(obj); return; }
        obj.SetActive(false);
        var itemBase = obj.GetComponent<ItemBase>();
        if (itemBase != null) itemBase.OnReturnToPool();
        if (!pools.ContainsKey(prefab)) pools[prefab] = new Queue<GameObject>();
        pools[prefab].Enqueue(obj);
    }
}
