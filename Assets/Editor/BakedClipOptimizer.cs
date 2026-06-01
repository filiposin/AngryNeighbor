using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BakedClipOptimizer : EditorWindow
{
    public AnimationClip sourceClip;
    public string outputFolder = "Assets/BakedAnimations/Optimized";

    public bool removeStaticPosition = true;
    public bool removeStaticRotation = true;
    public bool removeStaticScale = true;

    public bool skipRootTransform = false;

    public float positionTolerance = 0.00001f;
    public float scaleTolerance = 0.00001f;
    public float rotationToleranceDegrees = 0.01f;

    public string onlyPathsContaining = "";

    private int removedCurveCount;
    private int removedKeyCount;
    private int keptAnimatedGroupCount;
    private int removedStaticGroupCount;

    private static readonly string[] PositionProps =
    {
        "m_LocalPosition.x",
        "m_LocalPosition.y",
        "m_LocalPosition.z"
    };

    private static readonly string[] ScaleProps =
    {
        "m_LocalScale.x",
        "m_LocalScale.y",
        "m_LocalScale.z"
    };

    private static readonly string[] QuaternionRotationProps =
    {
        "m_LocalRotation.x",
        "m_LocalRotation.y",
        "m_LocalRotation.z",
        "m_LocalRotation.w"
    };

    private static readonly string[] EulerRawRotationProps =
    {
        "localEulerAnglesRaw.x",
        "localEulerAnglesRaw.y",
        "localEulerAnglesRaw.z"
    };

    private static readonly string[] EulerBakedRotationProps =
    {
        "localEulerAnglesBaked.x",
        "localEulerAnglesBaked.y",
        "localEulerAnglesBaked.z"
    };

    [MenuItem("Tools/Animation/Aggressive Optimize Baked Clip")]
    public static void Open()
    {
        GetWindow<BakedClipOptimizer>("Aggressive Clip Optimizer");
    }

    private void OnGUI()
    {
        sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Source Clip",
            sourceClip,
            typeof(AnimationClip),
            false
        );

        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space();

        removeStaticPosition = EditorGUILayout.Toggle("Remove Static Position", removeStaticPosition);
        removeStaticRotation = EditorGUILayout.Toggle("Remove Static Rotation", removeStaticRotation);
        removeStaticScale = EditorGUILayout.Toggle("Remove Static Scale", removeStaticScale);

        EditorGUILayout.Space();

        skipRootTransform = EditorGUILayout.Toggle("Skip Root Transform", skipRootTransform);

        EditorGUILayout.Space();

        positionTolerance = EditorGUILayout.FloatField("Position Tolerance", positionTolerance);
        scaleTolerance = EditorGUILayout.FloatField("Scale Tolerance", scaleTolerance);
        rotationToleranceDegrees = EditorGUILayout.FloatField("Rotation Tolerance Degrees", rotationToleranceDegrees);

        EditorGUILayout.Space();

        onlyPathsContaining = EditorGUILayout.TextField("Only Paths Containing", onlyPathsContaining);

        EditorGUILayout.HelpBox(
            "Пустое поле Only Paths Containing = оптимизировать все кости.\n" +
            "Например, можно вписать Tongue или Foot01, чтобы проверить только конкретные кости.",
            MessageType.Info
        );

        if (GUILayout.Button("Aggressively Optimize Clip"))
        {
            Optimize();
        }
    }

    private void Optimize()
    {
        if (sourceClip == null)
        {
            Debug.LogError("Assign Source Clip.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        removedCurveCount = 0;
        removedKeyCount = 0;
        keptAnimatedGroupCount = 0;
        removedStaticGroupCount = 0;

        AnimationClip optimizedClip = new AnimationClip();
        EditorUtility.CopySerialized(sourceClip, optimizedClip);
        optimizedClip.name = sourceClip.name + "_AggressiveOptimized";

        Dictionary<string, BoneCurveSet> bones = CollectTransformCurves(optimizedClip);

        foreach (BoneCurveSet bone in bones.Values)
        {
            if (skipRootTransform && string.IsNullOrEmpty(bone.path))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(onlyPathsContaining))
            {
                if (!bone.path.ToLowerInvariant().Contains(onlyPathsContaining.ToLowerInvariant()))
                {
                    continue;
                }
            }

            if (removeStaticPosition)
            {
                ProcessVectorGroup(
                    optimizedClip,
                    bone,
                    PositionProps,
                    positionTolerance,
                    "Position"
                );
            }

            if (removeStaticScale)
            {
                ProcessVectorGroup(
                    optimizedClip,
                    bone,
                    ScaleProps,
                    scaleTolerance,
                    "Scale"
                );
            }

            if (removeStaticRotation)
            {
                ProcessQuaternionRotationGroup(optimizedClip, bone);
                ProcessEulerRotationGroup(optimizedClip, bone, EulerRawRotationProps);
                ProcessEulerRotationGroup(optimizedClip, bone, EulerBakedRotationProps);
            }
        }

        optimizedClip.EnsureQuaternionContinuity();

        string safeName = sourceClip.name.Replace("/", "_").Replace("\\", "_");
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            outputFolder + "/" + safeName + "_AggressiveOptimized.anim"
        );

        AssetDatabase.CreateAsset(optimizedClip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(assetPath);

        Debug.Log(
            "Aggressive optimized clip created: " + assetPath +
            "\nRemoved static groups: " + removedStaticGroupCount +
            "\nKept animated groups: " + keptAnimatedGroupCount +
            "\nRemoved curves: " + removedCurveCount +
            "\nRemoved keys: " + removedKeyCount
        );
    }

    private Dictionary<string, BoneCurveSet> CollectTransformCurves(AnimationClip clip)
    {
        Dictionary<string, BoneCurveSet> result = new Dictionary<string, BoneCurveSet>();

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.type != typeof(Transform))
            {
                continue;
            }

            if (!IsSupportedTransformProperty(binding.propertyName))
            {
                continue;
            }

            BoneCurveSet set;

            if (!result.TryGetValue(binding.path, out set))
            {
                set = new BoneCurveSet();
                set.path = binding.path;
                result.Add(binding.path, set);
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

            set.bindings[binding.propertyName] = binding;
            set.curves[binding.propertyName] = curve;
        }

        return result;
    }

    private bool IsSupportedTransformProperty(string property)
    {
        return
            property == "m_LocalPosition.x" ||
            property == "m_LocalPosition.y" ||
            property == "m_LocalPosition.z" ||

            property == "m_LocalScale.x" ||
            property == "m_LocalScale.y" ||
            property == "m_LocalScale.z" ||

            property == "m_LocalRotation.x" ||
            property == "m_LocalRotation.y" ||
            property == "m_LocalRotation.z" ||
            property == "m_LocalRotation.w" ||

            property == "localEulerAnglesRaw.x" ||
            property == "localEulerAnglesRaw.y" ||
            property == "localEulerAnglesRaw.z" ||

            property == "localEulerAnglesBaked.x" ||
            property == "localEulerAnglesBaked.y" ||
            property == "localEulerAnglesBaked.z";
    }

    private void ProcessVectorGroup(
        AnimationClip clip,
        BoneCurveSet bone,
        string[] properties,
        float tolerance,
        string groupName
    )
    {
        if (!HasAnyCurve(bone, properties))
        {
            return;
        }

        if (AreScalarCurvesStatic(bone, properties, tolerance))
        {
            RemoveCurves(clip, bone, properties);
            removedStaticGroupCount++;
        }
        else
        {
            keptAnimatedGroupCount++;
        }
    }

    private void ProcessQuaternionRotationGroup(AnimationClip clip, BoneCurveSet bone)
    {
        if (!HasAnyCurve(bone, QuaternionRotationProps))
        {
            return;
        }

        if (IsQuaternionRotationStatic(bone, QuaternionRotationProps))
        {
            RemoveCurves(clip, bone, QuaternionRotationProps);
            removedStaticGroupCount++;
        }
        else
        {
            keptAnimatedGroupCount++;
        }
    }

    private void ProcessEulerRotationGroup(
        AnimationClip clip,
        BoneCurveSet bone,
        string[] properties
    )
    {
        if (!HasAnyCurve(bone, properties))
        {
            return;
        }

        if (IsEulerRotationStatic(bone, properties))
        {
            RemoveCurves(clip, bone, properties);
            removedStaticGroupCount++;
        }
        else
        {
            keptAnimatedGroupCount++;
        }
    }

    private bool HasAnyCurve(BoneCurveSet bone, string[] properties)
    {
        foreach (string property in properties)
        {
            if (bone.curves.ContainsKey(property))
            {
                return true;
            }
        }

        return false;
    }

    private bool AreScalarCurvesStatic(
        BoneCurveSet bone,
        string[] properties,
        float tolerance
    )
    {
        foreach (string property in properties)
        {
            AnimationCurve curve;

            if (!bone.curves.TryGetValue(property, out curve))
            {
                continue;
            }

            if (!IsScalarCurveStatic(curve, tolerance))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsScalarCurveStatic(AnimationCurve curve, float tolerance)
    {
        if (curve == null || curve.length <= 1)
        {
            return true;
        }

        float firstValue = curve.keys[0].value;

        for (int i = 1; i < curve.keys.Length; i++)
        {
            if (Mathf.Abs(curve.keys[i].value - firstValue) > tolerance)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsQuaternionRotationStatic(BoneCurveSet bone, string[] properties)
    {
        List<float> times = CollectKeyTimes(bone, properties);

        if (times.Count <= 1)
        {
            return true;
        }

        Quaternion first = EvaluateQuaternion(bone, properties, times[0]);

        for (int i = 1; i < times.Count; i++)
        {
            Quaternion current = EvaluateQuaternion(bone, properties, times[i]);

            if (Quaternion.Angle(first, current) > rotationToleranceDegrees)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsEulerRotationStatic(BoneCurveSet bone, string[] properties)
    {
        List<float> times = CollectKeyTimes(bone, properties);

        if (times.Count <= 1)
        {
            return true;
        }

        Quaternion first = EvaluateEulerAsQuaternion(bone, properties, times[0]);

        for (int i = 1; i < times.Count; i++)
        {
            Quaternion current = EvaluateEulerAsQuaternion(bone, properties, times[i]);

            if (Quaternion.Angle(first, current) > rotationToleranceDegrees)
            {
                return false;
            }
        }

        return true;
    }

    private List<float> CollectKeyTimes(BoneCurveSet bone, string[] properties)
    {
        List<float> times = new List<float>();

        foreach (string property in properties)
        {
            AnimationCurve curve;

            if (!bone.curves.TryGetValue(property, out curve))
            {
                continue;
            }

            if (curve == null)
            {
                continue;
            }

            for (int i = 0; i < curve.keys.Length; i++)
            {
                float time = curve.keys[i].time;

                bool alreadyExists = false;

                for (int j = 0; j < times.Count; j++)
                {
                    if (Mathf.Abs(times[j] - time) < 0.000001f)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    times.Add(time);
                }
            }
        }

        times.Sort();
        return times;
    }

    private Quaternion EvaluateQuaternion(
        BoneCurveSet bone,
        string[] properties,
        float time
    )
    {
        float x = EvaluateOrDefault(bone, properties[0], time, 0f);
        float y = EvaluateOrDefault(bone, properties[1], time, 0f);
        float z = EvaluateOrDefault(bone, properties[2], time, 0f);
        float w = EvaluateOrDefault(bone, properties[3], time, 1f);

        Quaternion q = new Quaternion(x, y, z, w);
        return NormalizeSafe(q);
    }

    private Quaternion EvaluateEulerAsQuaternion(
        BoneCurveSet bone,
        string[] properties,
        float time
    )
    {
        float x = EvaluateOrDefault(bone, properties[0], time, 0f);
        float y = EvaluateOrDefault(bone, properties[1], time, 0f);
        float z = EvaluateOrDefault(bone, properties[2], time, 0f);

        return Quaternion.Euler(x, y, z);
    }

    private float EvaluateOrDefault(
        BoneCurveSet bone,
        string property,
        float time,
        float defaultValue
    )
    {
        AnimationCurve curve;

        if (!bone.curves.TryGetValue(property, out curve))
        {
            return defaultValue;
        }

        if (curve == null || curve.length == 0)
        {
            return defaultValue;
        }

        return curve.Evaluate(time);
    }

    private Quaternion NormalizeSafe(Quaternion q)
    {
        float mag = Mathf.Sqrt(
            q.x * q.x +
            q.y * q.y +
            q.z * q.z +
            q.w * q.w
        );

        if (mag < 0.000001f)
        {
            return Quaternion.identity;
        }

        return new Quaternion(
            q.x / mag,
            q.y / mag,
            q.z / mag,
            q.w / mag
        );
    }

    private void RemoveCurves(
        AnimationClip clip,
        BoneCurveSet bone,
        string[] properties
    )
    {
        foreach (string property in properties)
        {
            EditorCurveBinding binding;
            AnimationCurve curve;

            if (!bone.bindings.TryGetValue(property, out binding))
            {
                continue;
            }

            if (bone.curves.TryGetValue(property, out curve))
            {
                if (curve != null)
                {
                    removedKeyCount += curve.length;
                }
            }

            AnimationUtility.SetEditorCurve(clip, binding, null);
            removedCurveCount++;
        }
    }

    private class BoneCurveSet
    {
        public string path;

        public Dictionary<string, EditorCurveBinding> bindings =
            new Dictionary<string, EditorCurveBinding>();

        public Dictionary<string, AnimationCurve> curves =
            new Dictionary<string, AnimationCurve>();
    }
}