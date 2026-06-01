using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ShadowBaker : MonoBehaviour
{
    [Header("Настройки Генерации")]
    public int textureResolution = 256;
    [Range(0, 10)] public int blurIterations = 4;
    [Range(1f, 3f)] public float expandSize = 1.3f;
    public float groundOffset = 0.01f;

    [Header("Внешний вид")]
    public Color shadowColor = new Color(0, 0, 0, 0.7f); // Цвет тени
    public Material customMaterial; // Сюда кидай Mat_ShadowMultiply

    [Header("Настройки Света")]
    public Vector3 lightDirection = new Vector3(1f, -1.5f, 1f); 
    public bool autoStretch = true;
    public float lengthMultiplier = 1.0f;
    public Vector2 manualPositionOffset = Vector2.zero;

    private void OnDrawGizmosSelected()
    {
        if (lightDirection.y >= -0.1f) lightDirection.y = -0.1f;
        Bounds bounds = GetBounds();
        Vector3 shadowPos = CalculateShadowPosition(bounds);
        Quaternion shadowRot = CalculateShadowRotation();
        Vector3 shadowScale = CalculateShadowScale(bounds);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bounds.center, bounds.center - lightDirection.normalized * 2f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(bounds.center, shadowPos);

        Gizmos.color = shadowColor; // Гизмо теперь цвета тени
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(shadowPos, shadowRot, shadowScale);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = oldMatrix;
    }

#if UNITY_EDITOR
    public void Bake()
    {
        if (lightDirection.y >= -0.1f) lightDirection.y = -0.1f;
        Bounds bounds = GetBounds();

        // 1. Рендер (черно-белая маска)
        GameObject camObj = new GameObject("TempBakeCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = new Color(0,0,0,0); // Полностью прозрачный фон
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.rotation = Quaternion.LookRotation(lightDirection);
        float dist = bounds.size.magnitude * 2f;
        cam.transform.position = bounds.center - (lightDirection.normalized * dist);
        float maxDim = Mathf.Max(bounds.extents.x, bounds.extents.z, bounds.extents.y);
        float orthoSize = maxDim * expandSize;
        cam.orthographicSize = orthoSize;

        int originalLayer = gameObject.layer;
        int tempLayer = 31;
        SetLayerRecursively(gameObject, tempLayer);
        cam.cullingMask = 1 << tempLayer;

        RenderTexture rt = new RenderTexture(textureResolution, textureResolution, 16);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, textureResolution, textureResolution), 0, 0);
        texture.Apply();

        SetLayerRecursively(gameObject, originalLayer);
        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(camObj);
        DestroyImmediate(rt);

        ProcessTexture(texture);

        string folder = "Assets/GeneratedShadows";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string path = $"{folder}/{gameObject.name}_{gameObject.GetInstanceID()}_Shadow.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.Refresh();

        ApplyShadowSprite(path, bounds, orthoSize);
    }

    private void ProcessTexture(Texture2D tex)
    {
        // Теперь мы храним только форму (Alpha), цвет задаст Материал или SpriteRenderer
        Color[] pixels = tex.GetPixels();
        for (int i = 0; i < pixels.Length; i++) 
        {
            // Делаем пиксель белым, но с нужной альфой, чтобы шейдер мог его красить
            float alpha = pixels[i].a;
            pixels[i] = new Color(1, 1, 1, alpha > 0.1f ? 1f : 0f); 
        }
        tex.SetPixels(pixels);
        
        // Блюр альфа-канала
        if (blurIterations > 0)
        {
            for (int k = 0; k < blurIterations; k++)
            {
                Color[] source = tex.GetPixels();
                Color[] dest = new Color[source.Length];
                int w = tex.width; 
                for(int i=0; i<source.Length; i++) {
                    int x = i % w; int y = i / w;
                    float sumA = 0; int cnt = 0;
                    for(int dy=-1; dy<=1; dy++) {
                        for(int dx=-1; dx<=1; dx++) {
                            int nx = Mathf.Clamp(x+dx, 0, w-1);
                            int ny = Mathf.Clamp(y+dy, 0, tex.height-1);
                            sumA += source[ny*w+nx].a; cnt++;
                        }
                    }
                    dest[i] = new Color(1,1,1, sumA/cnt);
                }
                tex.SetPixels(dest);
            }
        }
        tex.Apply();
    }

    private void ApplyShadowSprite(string path, Bounds bounds, float orthoSize)
    {
        Transform shadowT = transform.Find("Shadow_Sprite");
        GameObject shadowObj = shadowT ? shadowT.gameObject : new GameObject("Shadow_Sprite");
        shadowObj.transform.SetParent(transform);

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        SpriteRenderer sr = shadowObj.GetComponent<SpriteRenderer>();
        if (!sr) sr = shadowObj.AddComponent<SpriteRenderer>();
        
        sr.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f, 0.5f), 100f);

        // Применяем материал и цвет
        if (customMaterial != null)
        {
            sr.material = customMaterial;
            // Передаем цвет в шейдер
            Material instMat = new Material(customMaterial); 
            instMat.SetColor("_Color", shadowColor);
            sr.material = instMat;
        }
        else
        {
            sr.color = shadowColor; // Стандартный режим (Alpha Blend)
        }

        shadowObj.transform.position = CalculateShadowPosition(bounds);
        shadowObj.transform.rotation = CalculateShadowRotation();
        
        float spriteSizeUnits = tex.height / 100f; 
        float worldSizeCamera = orthoSize * 2f;
        float baseScale = worldSizeCamera / spriteSizeUnits;
        
        Vector3 projectionScale = CalculateShadowScale(bounds);
        shadowObj.transform.localScale = new Vector3(baseScale * projectionScale.x, baseScale * projectionScale.z, 1f);
    }
#endif

    // Математика (без изменений)
    private Vector3 CalculateShadowPosition(Bounds bounds) {
        float floorY = bounds.min.y + groundOffset;
        float h = bounds.center.y - floorY;
        float shiftX = (lightDirection.x / -lightDirection.y) * h;
        float shiftZ = (lightDirection.z / -lightDirection.y) * h;
        return new Vector3(bounds.center.x + shiftX + manualPositionOffset.x, floorY, bounds.center.z + shiftZ + manualPositionOffset.y);
    }
    private Quaternion CalculateShadowRotation() {
        Vector3 lightFlat = new Vector3(lightDirection.x, 0, lightDirection.z).normalized;
        if (lightFlat.magnitude < 0.1f) return Quaternion.Euler(90, 0, 0);
        Quaternion yRot = Quaternion.LookRotation(lightFlat);
        return Quaternion.Euler(90, yRot.eulerAngles.y, 0);
    }
    private Vector3 CalculateShadowScale(Bounds bounds) {
        if (!autoStretch) return new Vector3(lengthMultiplier, 1f, lengthMultiplier);
        float stretchFactor = 1.0f / Mathf.Abs(lightDirection.y / lightDirection.magnitude);
        return new Vector3(lengthMultiplier, 1f, Mathf.Clamp(stretchFactor, 1.0f, 5.0f) * lengthMultiplier);
    }
    private Bounds GetBounds() {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if(rs.Length==0) return new Bounds(transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach(Renderer r in rs) if(r.name!="Shadow_Sprite") b.Encapsulate(r.bounds);
        return b;
    }
    private void SetLayerRecursively(GameObject obj, int newLayer) {
        obj.layer = newLayer;
        foreach(Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(ShadowBaker))]
public class ShadowBakerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        ShadowBaker s = (ShadowBaker)target;
        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
        if(GUILayout.Button("🔥 СГЕНЕРИРОВАТЬ ТЕНЬ", GUILayout.Height(30))) s.Bake();
        GUI.backgroundColor = Color.white;
    }
}
#endif