using UnityEngine;
using System.Collections.Generic;

// Подключаем пространство имен редактора только если мы в Unity Editor
#if UNITY_EDITOR
using UnityEditor;
#endif

public class OneFileTreePlacer : MonoBehaviour
{
    [Header("Настройки")]
    public Terrain targetTerrain;
    public GameObject treePrefab;
    [Range(10, 10000)]
    public int treeCount = 500;
    
    [Header("Параметры размещения")]
    [Tooltip("Максимальный угол наклона (градусы), где растут деревья")]
    [Range(0, 90)]
    public float maxSlope = 45f;
    [Tooltip("Поднять/опустить дерево относительно земли")]
    public float heightOffset = 0f;
    public int randomSeed = 123;

    [Header("Рандомизация")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public bool randomRotation = true;

    // Ссылка на контейнер (скрыта в инспекторе, чтобы не мешалась)
    [HideInInspector]
    [SerializeField] 
    private Transform container;

    // --- ЛОГИКА ГЕНЕРАЦИИ ---
    public void GenerateTrees()
    {
        if (targetTerrain == null || treePrefab == null)
        {
            Debug.LogError("Не назначен Terrain или Prefab!");
            return;
        }

        // 1. Очистка старого
        ClearTrees();

        // 2. Создание контейнера
        GameObject contObj = new GameObject("GENERATED_TREES_CONTAINER");
        contObj.transform.parent = this.transform;
        contObj.transform.localPosition = Vector3.zero;
        container = contObj.transform;

        // 3. Генерация
        Random.InitState(randomSeed);
        TerrainData tData = targetTerrain.terrainData;
        Vector3 tPos = targetTerrain.transform.position;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = treeCount * 10; // Защита от зависания

        while (spawned < treeCount && attempts < maxAttempts)
        {
            attempts++;

            // Случайная позиция 0..1
            float normX = Random.value;
            float normZ = Random.value;

            // Проверка угла наклона
            float steepness = tData.GetSteepness(normX, normZ);
            if (steepness > maxSlope) continue;

            // Вычисление координат
            float worldX = tPos.x + normX * tData.size.x;
            float worldZ = tPos.z + normZ * tData.size.z;
            
            // Получаем высоту в этой точке
            float ySample = tData.GetHeight(Mathf.RoundToInt(normX * tData.heightmapResolution), Mathf.RoundToInt(normZ * tData.heightmapResolution));
            float worldY = tPos.y + ySample + heightOffset;

            Vector3 finalPos = new Vector3(worldX, worldY, worldZ);

            // Спавн объекта
#if UNITY_EDITOR
            // Используем PrefabUtility чтобы сохранить связь с префабом (синий кубик)
            GameObject tree = PrefabUtility.InstantiatePrefab(treePrefab) as GameObject;
            tree.transform.position = finalPos;
#else
            GameObject tree = Instantiate(treePrefab, finalPos, Quaternion.identity);
#endif

            tree.transform.parent = container;

            // Вращение и масштаб
            if (randomRotation) 
                tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            float scale = Random.Range(minScale, maxScale);
            tree.transform.localScale = Vector3.one * scale;

            spawned++;
        }
        
        Debug.Log($"Готово! Создано деревьев: {spawned}");
    }

    public void ClearTrees()
    {
        if (container != null)
        {
            DestroyImmediate(container.gameObject);
        }
        else
        {
            // На случай потери ссылки ищем по имени
            Transform child = transform.Find("GENERATED_TREES_CONTAINER");
            if (child != null) DestroyImmediate(child.gameObject);
        }
    }
}

// --- ЧАСТЬ ДЛЯ РЕДАКТОРА (КНОПКИ) ---
#if UNITY_EDITOR
[CustomEditor(typeof(OneFileTreePlacer))]
public class OneFileTreePlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Рисует все поля (Terrain, Prefab и т.д.)

        OneFileTreePlacer script = (OneFileTreePlacer)target;

        GUILayout.Space(20);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Сгенерировать деревья", GUILayout.Height(40)))
        {
            script.GenerateTrees();
            // Помечаем сцену как измененную, чтобы не забыть сохранить
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }

        GUILayout.Space(5);

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Удалить деревья", GUILayout.Height(30)))
        {
            script.ClearTrees();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif