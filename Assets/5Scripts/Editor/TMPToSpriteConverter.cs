using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

public class TMPToSpriteConverter : EditorWindow
{
    [MenuItem("GameObject/Convert TMP to Sprite", false, 10)]
    static void ConvertToSprite()
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a TextMeshPro object.", "OK");
            return;
        }

        TextMeshPro tmp = selectedObj.GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            EditorUtility.DisplayDialog("Error", "Selected object does not have a TextMeshPro component.", "OK");
            return;
        }

        // 1. Получаем ЛОКАЛЬНЫЕ границы оригинала (не зависят от World-поворота)
        tmp.ForceMeshUpdate();
        Bounds localBounds = tmp.bounds;

        if (localBounds.size.x <= 0 || localBounds.size.y <= 0)
        {
            EditorUtility.DisplayDialog("Error", "Text bounds are empty. Does it have any text?", "OK");
            return;
        }

        // Высчитываем реальный геометрический центр текста в мире (учитывая offset локального центра)
        Vector3 worldCenter = tmp.transform.TransformPoint(localBounds.center);

        // 2. Делаем клон, убираем вращение, ставим в нули
        GameObject clone = Instantiate(selectedObj);
        clone.transform.SetParent(null);
        clone.transform.position = Vector3.zero;
        clone.transform.rotation = Quaternion.identity;
        // Копируем мировой масштаб
        Vector3 lossyScale = selectedObj.transform.lossyScale;
        clone.transform.localScale = lossyScale;

        TextMeshPro cloneTmp = clone.GetComponent<TextMeshPro>();
        cloneTmp.ForceMeshUpdate();
        
        // 3. Получаем границы клона и реальные размеры с учетом масштаба
        Bounds cloneLocalBounds = cloneTmp.bounds;
        Vector3 cloneCenterWorld = clone.transform.TransformPoint(cloneLocalBounds.center);
        
        float worldWidth = cloneLocalBounds.size.x * Mathf.Abs(lossyScale.x);
        float worldHeight = cloneLocalBounds.size.y * Mathf.Abs(lossyScale.y);

        // 4. Настраиваем слои рендера
        int renderLayer = 31;
        SetLayerRecursively(clone, renderLayer);

        // 5. Создаем камеру
        GameObject camObj = new GameObject("TempRenderCamera");
        Camera cam = camObj.AddComponent<Camera>();
        
        // Камера СМОТРИТ в сторону +Z, ставим её ровно перед текстом
        cam.transform.position = cloneCenterWorld - new Vector3(0, 0, 10f); // Ставим ровно по центру
        cam.transform.LookAt(cloneCenterWorld);
        
        cam.orthographic = true;
        cam.orthographicSize = worldHeight / 2f; 
        float aspect = worldWidth / worldHeight;
        cam.aspect = aspect;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0); // Абсолютно прозрачный фон
        cam.cullingMask = 1 << renderLayer;

        // 6. Формируем RenderTexture
        int height = 1024;
        int width = Mathf.RoundToInt(height * aspect);
        if (width > 8192)
        {
            width = 8192;
            height = Mathf.RoundToInt(width / aspect);
        }

        if (height <= 0) height = 100;
        if (width <= 0) width = 100;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        cam.targetTexture = rt;

        // 7. Снапшот
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // Очищаем временные объекты
        cam.targetTexture = null;
        DestroyImmediate(rt);
        DestroyImmediate(camObj);
        DestroyImmediate(clone);

        // 8. Сохраняем файл
        byte[] bytes = tex.EncodeToPNG();
        DestroyImmediate(tex);

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/ConvertedTMP"))
            AssetDatabase.CreateFolder("Assets/Sprites", "ConvertedTMP");

        string safeName = string.Join("_", selectedObj.name.Split(Path.GetInvalidFileNameChars()));
        string fileName = safeName.Replace(" ", "_") + "_" + System.DateTime.Now.Ticks + ".png";
        string filePath = "Assets/Sprites/ConvertedTMP/" + fileName;

        File.WriteAllBytes(filePath, bytes);
        AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);

        // 9. Настраиваем Sprite
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;

            // PPU такой, чтобы мировой размер совпадал с worldHeight
            float ppu = height / worldHeight;
            importer.spritePixelsPerUnit = ppu;
            
            importer.SaveAndReimport();
        }

        // 10. Спавним спрайт
        Sprite generatedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
        if (generatedSprite != null)
        {
            GameObject spriteObj = new GameObject(selectedObj.name + "_Sprite");
            
            // Центр спрайта ставится точно в оригинальный геометрический центр 3D-текста
            spriteObj.transform.position = worldCenter;
            spriteObj.transform.rotation = selectedObj.transform.rotation;
            
            SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.sprite = generatedSprite;

            Undo.RegisterCreatedObjectUndo(spriteObj, "Convert TMP to Sprite");
            Selection.activeGameObject = spriteObj;
            Debug.Log($"[TMP to Sprite] Успешно! Спрайт сохранен: {filePath}");
        }
    }

    static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    [MenuItem("GameObject/Convert TMP to Sprite", true)]
    static bool ValidateConvert()
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<TextMeshPro>() != null;
    }
}
