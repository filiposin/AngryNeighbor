using UnityEngine;
using UnityEditor;

public class ReplaceOldObjects : EditorWindow
{
    private enum AlignMode
    {
        Pivot,
        BoundsCenter,
        BoundsBottomCenter
    }

    private enum RotationMode
    {
        OldObjectRotation,
        OldObjectPlusPrefabRotation,
        OldObjectPlusManualOffset
    }

    public GameObject newPrefab;
    [SerializeField] private AlignMode alignMode = AlignMode.BoundsBottomCenter;
    [SerializeField] private RotationMode rotationMode = RotationMode.OldObjectPlusManualOffset;
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(0f, 90f, 0f);

    [MenuItem("Tools/Replace Old Objects")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceOldObjects>("Replace Old Objects");
    }

    private void OnGUI()
    {
        GUILayout.Label("Замена старых объектов (тег 'old')", EditorStyles.boldLabel);
        newPrefab = (GameObject)EditorGUILayout.ObjectField("Новый Prefab:", newPrefab, typeof(GameObject), false);
        alignMode = (AlignMode)EditorGUILayout.EnumPopup("Align By:", alignMode);
        rotationMode = (RotationMode)EditorGUILayout.EnumPopup("Rotation:", rotationMode);
        if (rotationMode == RotationMode.OldObjectPlusManualOffset)
        {
            rotationOffsetEuler = EditorGUILayout.Vector3Field("Rotation Offset:", rotationOffsetEuler);
        }

        if (GUILayout.Button("Заменить все старые объекты"))
        {
            if (newPrefab == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Сначала назначь новый prefab!", "Ок");
                return;
            }

            ReplaceObjectsWithTag("old", newPrefab, alignMode, rotationMode, rotationOffsetEuler);
        }
    }

    private static void ReplaceObjectsWithTag(
        string tag,
        GameObject newPrefab,
        AlignMode alignMode,
        RotationMode rotationMode,
        Vector3 rotationOffsetEuler)
    {
        GameObject[] oldObjects = GameObject.FindGameObjectsWithTag(tag);
        int replacedCount = 0;

        foreach (GameObject oldObj in oldObjects)
        {
            Transform oldTransform = oldObj.transform;
            Transform parent = oldTransform.parent;
            int siblingIndex = oldTransform.GetSiblingIndex();

            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);
            Undo.RegisterCreatedObjectUndo(newObj, "Replace Old Object");
            Quaternion prefabLocalRotation = newObj.transform.localRotation;

            // Сохраняем иерархию и локальный transform старого объекта.
            newObj.transform.SetParent(parent, false);
            newObj.transform.SetSiblingIndex(siblingIndex);
            newObj.transform.localPosition = oldTransform.localPosition;
            newObj.transform.localRotation = GetTargetRotation(
                oldTransform.localRotation,
                prefabLocalRotation,
                rotationMode,
                rotationOffsetEuler);
            newObj.transform.localScale = oldTransform.localScale;

            AlignObject(oldObj, newObj, alignMode);

            Undo.DestroyObjectImmediate(oldObj);
            replacedCount++;
        }

        EditorUtility.DisplayDialog("Готово", $"Заменено объектов: {replacedCount}", "Ок");
    }

    private static Quaternion GetTargetRotation(
        Quaternion oldRotation,
        Quaternion prefabRotation,
        RotationMode rotationMode,
        Vector3 rotationOffsetEuler)
    {
        switch (rotationMode)
        {
            case RotationMode.OldObjectPlusPrefabRotation:
                return oldRotation * prefabRotation;
            case RotationMode.OldObjectPlusManualOffset:
                return oldRotation * Quaternion.Euler(rotationOffsetEuler);
            default:
                return oldRotation;
        }
    }

    private static void AlignObject(GameObject oldObj, GameObject newObj, AlignMode alignMode)
    {
        if (alignMode == AlignMode.Pivot)
        {
            return;
        }

        if (!TryGetObjectBounds(oldObj, out Bounds oldBounds) ||
            !TryGetObjectBounds(newObj, out Bounds newBounds))
        {
            return;
        }

        Vector3 oldPoint = GetAlignPoint(oldBounds, alignMode);
        Vector3 newPoint = GetAlignPoint(newBounds, alignMode);
        newObj.transform.position += oldPoint - newPoint;
    }

    private static Vector3 GetAlignPoint(Bounds bounds, AlignMode alignMode)
    {
        if (alignMode == AlignMode.BoundsBottomCenter)
        {
            Vector3 point = bounds.center;
            point.y = bounds.min.y;
            return point;
        }

        return bounds.center;
    }

    private static bool TryGetObjectBounds(GameObject obj, out Bounds bounds)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return true;
        }

        bounds = default;
        return false;
    }
}
