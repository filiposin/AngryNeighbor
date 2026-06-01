using UnityEngine;
using UnityEditor;

public class MeshCleaner : EditorWindow
{
    [MenuItem("Tools/Clean Mesh Asset (Keep Bones & BlendShapes)")]
    public static void ExtractPureMesh()
    {
        Mesh selectedMesh = Selection.activeObject as Mesh;

        if (selectedMesh == null && Selection.activeGameObject != null)
        {
            MeshFilter mf = Selection.activeGameObject.GetComponent<MeshFilter>();
            if (mf != null) selectedMesh = mf.sharedMesh;
            else
            {
                SkinnedMeshRenderer smr = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
                if (smr != null) selectedMesh = smr.sharedMesh;
            }
        }

        if (selectedMesh == null)
        {
            Debug.LogError("Ошибка: Выделите файл .asset в Project или объект на сцене.");
            return;
        }

        Mesh cleanMesh = new Mesh();
        cleanMesh.name = selectedMesh.name + "_Cleaned";

        // 1. Формат индексов (для моделей > 65k полигонов)
        cleanMesh.indexFormat = selectedMesh.indexFormat;

        // 2. Базовая геометрия
        cleanMesh.vertices = selectedMesh.vertices;
        cleanMesh.normals = selectedMesh.normals;
        cleanMesh.uv = selectedMesh.uv;
        cleanMesh.uv2 = selectedMesh.uv2;
        cleanMesh.tangents = selectedMesh.tangents;

        // 3. КОСТИ (ИСПРАВЛЕНИЕ ДЛЯ REALLUSION: поддержка 8 костей на вершину!)
        var bonesPerVertex = selectedMesh.GetBonesPerVertex();
        var allBoneWeights = selectedMesh.GetAllBoneWeights();
        
        if (bonesPerVertex.Length > 0 && allBoneWeights.Length > 0)
        {
            // Этот метод сохраняет все сложные привязки CC3/CC4
            cleanMesh.SetBoneWeights(bonesPerVertex, allBoneWeights);
        }
        else
        {
            // Запасной вариант для простых моделей
            cleanMesh.boneWeights = selectedMesh.boneWeights;
        }
        cleanMesh.bindposes = selectedMesh.bindposes;

        // 4. Сабмеши (Материалы)
        cleanMesh.subMeshCount = selectedMesh.subMeshCount;
        for (int i = 0; i < selectedMesh.subMeshCount; i++)
        {
            cleanMesh.SetIndices(selectedMesh.GetIndices(i), selectedMesh.GetTopology(i), i);
        }

        // 5. БЛЕНДШЕЙПЫ (ЭКСТРЕМАЛЬНОЕ СЖАТИЕ)
        int blendShapeCount = selectedMesh.blendShapeCount;
        int vertexCount = selectedMesh.vertexCount;

        // Выделяем память ОДИН раз, чтобы Unity не завис от переполнения памяти
        Vector3[] deltaVertices = new Vector3[vertexCount];
        Vector3[] deltaNormals = new Vector3[vertexCount];
        Vector3[] deltaTangents = new Vector3[vertexCount];

        for (int i = 0; i < blendShapeCount; i++)
        {
            string shapeName = selectedMesh.GetBlendShapeName(i);
            int frameCount = selectedMesh.GetBlendShapeFrameCount(i);
            
            for (int frame = 0; frame < frameCount; frame++)
            {
                float weight = selectedMesh.GetBlendShapeFrameWeight(i, frame);

                // Извлекаем данные
                selectedMesh.GetBlendShapeFrameVertices(i, frame, deltaVertices, deltaNormals, deltaTangents);

                // ВНИМАНИЕ: Мы передаем null вместо нормалей и тангентов!
                // Это моментально сокращает вес блендшейпов на ~66% без заметной потери качества.
                cleanMesh.AddBlendShapeFrame(shapeName, weight, deltaVertices, null, null);
            }
        }

        // 6. ОПТИМИЗАЦИЯ И КОМПРЕССИЯ
        cleanMesh.RecalculateBounds();
        cleanMesh.Optimize(); 

        // Жестко включаем компрессию меша (уменьшает размер файла на диске)
        MeshUtility.SetMeshCompression(cleanMesh, ModelImporterMeshCompression.Medium);

        // 7. Сохранение
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Cleaned Mesh",
            cleanMesh.name + ".asset",
            "asset",
            "Please enter a file name to save the clean mesh to"
        );

        if (string.IsNullOrEmpty(path)) return;

        AssetDatabase.CreateAsset(cleanMesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>Успех!</color> Меш сжат и сохранен: {path}");
    }
}