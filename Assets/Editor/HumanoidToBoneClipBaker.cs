using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class HumanoidToBoneClipBaker : EditorWindow
{
    public Animator sourceAnimator;
    public AnimationClip sourceClip;
    public int fps = 30;
    public string outputFolder = "Assets/BakedAnimations";

    [MenuItem("Tools/Animation/Bake Humanoid Clip To Bone Curves")]
    public static void Open()
    {
        GetWindow<HumanoidToBoneClipBaker>("Humanoid To Bones");
    }

    private void OnGUI()
    {
        sourceAnimator = (Animator)EditorGUILayout.ObjectField(
            "Source Animator",
            sourceAnimator,
            typeof(Animator),
            true
        );

        sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Source Clip",
            sourceClip,
            typeof(AnimationClip),
            false
        );

        fps = EditorGUILayout.IntField("FPS", fps);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        if (GUILayout.Button("Bake"))
        {
            Bake();
        }
    }

    private void Bake()
    {
        if (sourceAnimator == null)
        {
            Debug.LogError("Assign Source Animator from the scene.");
            return;
        }

        if (sourceClip == null)
        {
            Debug.LogError("Assign Source Clip.");
            return;
        }

        if (fps <= 0)
        {
            Debug.LogError("FPS must be greater than zero.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        GameObject instance = Instantiate(sourceAnimator.gameObject);
        instance.name = sourceAnimator.gameObject.name + "_BakeTemp";
        instance.hideFlags = HideFlags.HideAndDontSave;

        Animator animator = instance.GetComponent<Animator>();
        Transform root = animator.transform;
        Transform[] bones = root.GetComponentsInChildren<Transform>(true);

        var curves = new Dictionary<string, AnimationCurve>();
        var lastRotations = new Dictionary<Transform, Quaternion>();

        AnimationClip bakedClip = new AnimationClip();
        bakedClip.name = sourceClip.name + "_BakedBones";
        bakedClip.frameRate = fps;

        int sampleCount = Mathf.CeilToInt(sourceClip.length * fps);

        AnimationMode.StartAnimationMode();

        try
        {
            for (int i = 0; i <= sampleCount; i++)
            {
                float time = Mathf.Min(i / (float)fps, sourceClip.length);

                AnimationMode.SampleAnimationClip(instance, sourceClip, time);

                foreach (Transform bone in bones)
                {
                    string path = AnimationUtility.CalculateTransformPath(bone, root);

                    Vector3 pos = bone.localPosition;
                    Vector3 scale = bone.localScale;
                    Quaternion rot = bone.localRotation;

                    if (lastRotations.TryGetValue(bone, out Quaternion lastRot))
                    {
                        if (Quaternion.Dot(lastRot, rot) < 0f)
                        {
                            rot = new Quaternion(-rot.x, -rot.y, -rot.z, -rot.w);
                        }
                    }

                    lastRotations[bone] = rot;

                    AddKey(curves, path, "m_LocalPosition.x", time, pos.x);
                    AddKey(curves, path, "m_LocalPosition.y", time, pos.y);
                    AddKey(curves, path, "m_LocalPosition.z", time, pos.z);

                    AddKey(curves, path, "m_LocalRotation.x", time, rot.x);
                    AddKey(curves, path, "m_LocalRotation.y", time, rot.y);
                    AddKey(curves, path, "m_LocalRotation.z", time, rot.z);
                    AddKey(curves, path, "m_LocalRotation.w", time, rot.w);

                    AddKey(curves, path, "m_LocalScale.x", time, scale.x);
                    AddKey(curves, path, "m_LocalScale.y", time, scale.y);
                    AddKey(curves, path, "m_LocalScale.z", time, scale.z);
                }
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            DestroyImmediate(instance);
        }

        foreach (var pair in curves)
        {
            string[] parts = pair.Key.Split('|');
            string path = parts[0];
            string property = parts[1];

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                property
            );

            AnimationUtility.SetEditorCurve(bakedClip, binding, pair.Value);
        }

        bakedClip.EnsureQuaternionContinuity();

        string safeName = sourceClip.name.Replace("/", "_").Replace("\\", "_");
        string assetPath = $"{outputFolder}/{safeName}_BakedBones.anim";

        AssetDatabase.CreateAsset(bakedClip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Baked clip created: {assetPath}");
    }

    private static void AddKey(
        Dictionary<string, AnimationCurve> curves,
        string path,
        string property,
        float time,
        float value
    )
    {
        string key = path + "|" + property;

        if (!curves.TryGetValue(key, out AnimationCurve curve))
        {
            curve = new AnimationCurve();
            curves[key] = curve;
        }

        curve.AddKey(time, value);
    }
}