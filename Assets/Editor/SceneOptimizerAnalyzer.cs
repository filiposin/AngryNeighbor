using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SceneOptimizerAnalyzer : EditorWindow
{
    private struct ObjectStats
    {
        public GameObject gameObject;
        public int triangles;
        public int vertices;
        public int materials;
        public bool castsShadows;
        public bool isSkinned;
    }

    private List<ObjectStats> statsList = new List<ObjectStats>();
    private Vector2 scrollPos;
    private bool sortByTriangles = true;

    [MenuItem("Tools/Scene Optimizer Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<SceneOptimizerAnalyzer>("Scene Analyzer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Анализ объектов в текущей сцене", EditorStyles.boldLabel);

        if (GUILayout.Button("Сканировать сцену", GUILayout.Height(30)))
        {
            AnalyzeScene();
        }

        if (statsList.Count > 0)
        {
            EditorGUILayout.Space();
            
            // Сортировка
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(sortByTriangles, "Сортировать по треугольникам", "Button")) { sortByTriangles = true; SortData(); }
            if (GUILayout.Toggle(!sortByTriangles, "Сортировать по материалам", "Button")) { sortByTriangles = false; SortData(); }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Заголовки таблицы
            DrawHeader();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var stats in statsList)
            {
                if (stats.gameObject == null) continue;

                DrawObjectRow(stats);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void AnalyzeScene()
    {
        statsList.Clear();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            Mesh mesh = null;
            bool isSkinned = false;
            int matCount = 0;
            bool shadows = false;

            // Проверка обычного Mesh
            if (obj.TryGetComponent<MeshFilter>(out var filter))
            {
                mesh = filter.sharedMesh;
            }
            
            // Проверка Skinned Mesh (персонажи)
            if (obj.TryGetComponent<SkinnedMeshRenderer>(out var skinned))
            {
                mesh = skinned.sharedMesh;
                isSkinned = true;
                matCount = skinned.sharedMaterials.Length;
                shadows = skinned.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else if (obj.TryGetComponent<MeshRenderer>(out var renderer))
            {
                matCount = renderer.sharedMaterials.Length;
                shadows = renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            if (mesh != null)
            {
                statsList.Add(new ObjectStats
                {
                    gameObject = obj,
                    triangles = mesh.triangles.Length / 3,
                    vertices = mesh.vertexCount,
                    materials = matCount,
                    castsShadows = shadows,
                    isSkinned = isSkinned
                });
            }
        }
        SortData();
    }

    private void SortData()
    {
        if (sortByTriangles)
            statsList = statsList.OrderByDescending(s => s.triangles).ToList();
        else
            statsList = statsList.OrderByDescending(s => s.materials).ToList();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("Объект", GUILayout.Width(150));
        GUILayout.Label("Треугольники", GUILayout.Width(100));
        GUILayout.Label("Материалы", GUILayout.Width(80));
        GUILayout.Label("Тени", GUILayout.Width(50));
        GUILayout.Label("Skinned", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawObjectRow(ObjectStats stats)
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button(stats.gameObject.name, EditorStyles.label, GUILayout.Width(150)))
        {
            Selection.activeGameObject = stats.gameObject;
            EditorGUIUtility.PingObject(stats.gameObject);
        }

        // Подсветка критических значений
        GUI.color = stats.triangles > 5000 ? Color.yellow : Color.white;
        if (stats.triangles > 20000) GUI.color = Color.red;
        GUILayout.Label(stats.triangles.ToString("N0"), GUILayout.Width(100));
        
        GUI.color = stats.materials > 2 ? Color.yellow : Color.white;
        GUILayout.Label(stats.materials.ToString(), GUILayout.Width(80));
        
        GUI.color = Color.white;
        GUILayout.Label(stats.castsShadows ? "Да" : "Нет", GUILayout.Width(50));
        GUILayout.Label(stats.isSkinned ? "Да" : "Нет", GUILayout.Width(60));

        EditorGUILayout.EndHorizontal();
    }
}