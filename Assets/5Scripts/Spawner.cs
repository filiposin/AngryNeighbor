using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [Header("Коллайдеры")]
    public Collider colliderA;
    public Collider colliderB;

    [Header("Имена предметов (фильтр по имени)")]
    public string requiredNameA;
    public string requiredNameB;

    [Header("Префаб и трансформ спавна")]
    public GameObject prefabToSpawn;
    public Vector3 spawnPosition;
    public Vector3 spawnRotationEuler;

    [System.Serializable]
    public class TrackedItem
    {
        public GameObject obj;          // объект, у которого будем менять материалы
        public Material matOff;         // материал когда нет предмета
        public Material matOn;          // материал когда предмет есть
        public ReactTo reactTo = ReactTo.Both;
        [HideInInspector] public bool lockedOn = false; // после спавна фиксируем matOn
    }

    public enum ReactTo { A, B, Both }

    [Header("Список отслеживаемых объектов")]
    public TrackedItem[] trackedItems;

    bool spawned = false;

    void Update()
    {
        // Проверки наличия требуемых объектов в коллайдерах
        bool aHas = IsNameInCollider(colliderA, requiredNameA);
        bool bHas = IsNameInCollider(colliderB, requiredNameB);

        // Обновляем материалы у отслеживаемых объектов (если они ещё не зафиксированы)
        foreach (var t in trackedItems)
        {
            if (t == null || t.obj == null) continue;
            if (t.lockedOn) continue;

            bool shouldBeOn = false;
            if (t.reactTo == ReactTo.A) shouldBeOn = aHas;
            else if (t.reactTo == ReactTo.B) shouldBeOn = bHas;
            else shouldBeOn = aHas && bHas;

            ApplyMaterial(t, shouldBeOn ? t.matOn : t.matOff);
        }

        // Если оба предмета есть и ещё не спавнили — спавним, удаляем предметы и фиксируем материалы
        if (!spawned && aHas && bHas)
        {
            SpawnAndCleanup();
        }
    }

    bool IsNameInCollider(Collider col, string name)
    {
        if (col == null || string.IsNullOrEmpty(name)) return false;
        Bounds b = col.bounds;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, Quaternion.identity);
        foreach (var hit in hits)
        {
            if (hit != null && hit.gameObject != null && hit.gameObject.name == name)
                return true;
        }
        return false;
    }

    void SpawnAndCleanup()
    {
        // Спавн
        if (prefabToSpawn != null)
        {
            Quaternion rot = Quaternion.Euler(spawnRotationEuler);
            Instantiate(prefabToSpawn, spawnPosition, rot);
        }

        // Удаляем все объекты с нужными именами внутри коллайдеров
        DestroyNamedInCollider(colliderA, requiredNameA);
        DestroyNamedInCollider(colliderB, requiredNameB);

        // Фиксируем для всех trackedItems второй материал (если он указан)
        foreach (var t in trackedItems)
        {
            if (t == null || t.obj == null) continue;
            if (t.matOn != null)
            {
                ApplyMaterial(t, t.matOn);
                t.lockedOn = true;
            }
        }

        spawned = true;
    }

    void DestroyNamedInCollider(Collider col, string name)
    {
        if (col == null || string.IsNullOrEmpty(name)) return;
        Bounds b = col.bounds;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, Quaternion.identity);
        // Собираем уникальные GameObject'ы (чтобы не дублировать Destroy)
        HashSet<GameObject> toDestroy = new HashSet<GameObject>();
        foreach (var hit in hits)
        {
            if (hit != null && hit.gameObject != null && hit.gameObject.name == name)
                toDestroy.Add(hit.gameObject);
        }
        foreach (var go in toDestroy)
        {
            Destroy(go);
        }
    }

    void ApplyMaterial(TrackedItem t, Material mat)
    {
        if (t == null || t.obj == null || mat == null) return;
        var rend = t.obj.GetComponent<Renderer>();
        if (rend == null) rend = t.obj.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        // если у рендера несколько материалов — поменяем все на указанный (проще и безопаснее)
        var mats = rend.materials;
        for (int i = 0; i < mats.Length; i++) mats[i] = mat;
        rend.materials = mats;
    }

    // Для наглядности в редакторе: покажем bounds коллайдеров
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (colliderA != null) Gizmos.DrawWireCube(colliderA.bounds.center, colliderA.bounds.size);
        Gizmos.color = Color.cyan;
        if (colliderB != null) Gizmos.DrawWireCube(colliderB.bounds.center, colliderB.bounds.size);
    }
}
