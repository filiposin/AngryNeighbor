using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// --- КЛАСС ДЛЯ НАСТРОЙКИ ЭЛЕМЕНТА ---
[System.Serializable]
public class GarlandItem
{
    [Tooltip("Префаб игрушки/лампочки")]
    public GameObject prefab;

    [Tooltip("Базовый размер (1 = 1,1,1). Умножается на глобальный рандом.")]
    public float baseScale = 1f;
}

[RequireComponent(typeof(MeshFilter))]
public class GarlandSpawner : MonoBehaviour
{
    [Header("--- СПИСОК ИГРУШЕК ---")]
    public GarlandItem[] items;

    [Header("--- РЕЖИМ СПАВНА ---")]
    [Tooltip("ВКЛ = строго по порядку списка (1, 2, 3...). ВЫКЛ = рандомно.")]
    public bool useSequentialOrder = false;

    [Header("--- ДИСТАНЦИЯ (ГЛАВНОЕ) ---")]
    [Tooltip("Минимальное расстояние между игрушками (в юнитах Unity).")]
    [Min(0.01f)]
    public float minDistance = 0.5f;

    [Header("--- ШАНС И РАНДОМ ---")]
    [Tooltip("Шанс появления в подходящей точке (0..1). Если меньше 1, будут пропуски.")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Tooltip("Сид рандома")]
    public int randomSeed = 12345;

    [Header("--- ТРАНСФОРМАЦИЯ ---")]
    [Tooltip("Сдвиг от поверхности по нормали")]
    public float surfaceOffset = 0.0f;

    [Tooltip("Доп. поворот (для всех)")]
    public Vector3 rotationOffset = new Vector3(0, 0, 0);

    [Tooltip("Рандомный разброс поворота")]
    public Vector3 randomRotationRange = new Vector3(0, 180, 0);

    [Tooltip("Выравнивать по нормали (торчать из провода)")]
    public bool alignToNormal = true;

    [Header("--- ВАРИАЦИЯ РАЗМЕРА ---")]
    public float minRandomMult = 0.9f;
    public float maxRandomMult = 1.1f;

    [Header("--- СИСТЕМА ---")]
    public string containerName = "Toys_Container";

    // --- ГЕНЕРАЦИЯ ---
    public void Generate()
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogError("Список Items пуст!");
            return;
        }

        ClearGarland();

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if (!mesh) return;

        Random.InitState(randomSeed);

        // Чтобы не зависеть от скейла самого объекта гирлянды, переводим точки в мировые (или учитываем lossyScale),
        // но проще работать в локальных, если гирлянда не искажена. 
        // Если гирлянда растянута скейлом, distance может врать. Будем считать в локальных координатах меша.

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        // Контейнер
        Transform container = new GameObject(containerName).transform;
        container.SetParent(this.transform);
        container.localPosition = Vector3.zero;
        container.localRotation = Quaternion.identity;
        container.localScale = Vector3.one;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(container.gameObject, "Garland Generation");
#endif

        int sequenceIndex = 0;
        
        // Позиция последней поставленной игрушки (ставим далеко, чтобы первая точно встала)
        Vector3 lastSpawnPos = new Vector3(-99999, -99999, -99999);

        // Идем по треугольникам
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // 1. Центр треугольника
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            Vector3 centerPos = (v1 + v2 + v3) / 3f;

            // 2. ПРОВЕРКА ДИСТАНЦИИ
            // Считаем расстояние от текущей точки до последней успешно поставленной игрушки
            // Используем transform.TransformPoint, чтобы учитывать реальный размер гирлянды в мире, если она отскейлена
            float dist = Vector3.Distance(transform.TransformPoint(centerPos), transform.TransformPoint(lastSpawnPos));

            if (dist < minDistance)
            {
                continue; // Слишком близко к предыдущей, пропускаем
            }

            // 3. Проверка шанса (уже после проверки дистанции, чтобы шанс создавал "дырки", а не сбивал ритм дистанции)
            // Но тут есть нюанс: если шанс не сработал, lastSpawnPos не обновляется, и мы попробуем на след. полигоне.
            // Это даст плотную укладку, но с элементами рандома. 
            // Если нужно "пропустить место", логика сложнее, но обычно так работает лучше.
            if (Random.value > spawnChance) 
            {
                 // Если хотим, чтобы при неудаче по шансу место оставалось пустым - раскомментируй строку ниже:
                 // lastSpawnPos = centerPos; 
                 continue;
            }

            // 4. Нормаль
            Vector3 n1 = normals[triangles[i]];
            Vector3 n2 = normals[triangles[i + 1]];
            Vector3 n3 = normals[triangles[i + 2]];
            Vector3 avgNormal = (n1 + n2 + n3).normalized;

            // 5. Выбор префаба
            GarlandItem selectedItem;
            if (useSequentialOrder)
            {
                selectedItem = items[sequenceIndex % items.Length];
                sequenceIndex++;
            }
            else
            {
                selectedItem = items[Random.Range(0, items.Length)];
            }

            if (selectedItem.prefab == null) continue;

            // 6. Спавн
            GameObject newObj;
#if UNITY_EDITOR
            newObj = (GameObject)PrefabUtility.InstantiatePrefab(selectedItem.prefab);
#else
            newObj = Instantiate(selectedItem.prefab);
#endif
            newObj.transform.SetParent(container);
            newObj.transform.localPosition = centerPos + (avgNormal * surfaceOffset);

            // 7. Поворот
            if (alignToNormal)
                newObj.transform.localRotation = Quaternion.LookRotation(avgNormal) * Quaternion.Euler(90, 0, 0);
            else
                newObj.transform.localRotation = Quaternion.identity;

            newObj.transform.Rotate(rotationOffset, Space.Self);
            newObj.transform.Rotate(
                Random.Range(-randomRotationRange.x, randomRotationRange.x),
                Random.Range(-randomRotationRange.y, randomRotationRange.y),
                Random.Range(-randomRotationRange.z, randomRotationRange.z),
                Space.Self
            );

            // 8. Скейл
            float randomMult = Random.Range(minRandomMult, maxRandomMult);
            newObj.transform.localScale = Vector3.one * (selectedItem.baseScale * randomMult);

            // 9. ЗАПОМИНАЕМ ПОЗИЦИЮ
            lastSpawnPos = centerPos;
        }
    }

    public void ClearGarland()
    {
        Transform child = transform.Find(containerName);
        if (child != null)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(child.gameObject);
#else
            DestroyImmediate(child.gameObject);
#endif
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GarlandSpawner))]
public class GarlandSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GarlandSpawner script = (GarlandSpawner)target;

        GUILayout.Space(20);
        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f); 
        if (GUILayout.Button("GENERATE (Distance Based)", GUILayout.Height(40)))
        {
            script.Generate();
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("CLEAR", GUILayout.Height(30)))
        {
            script.ClearGarland();
        }
    }
}
#endif