using System.Collections.Generic;
using UnityEngine;

public class PatrolNode : MonoBehaviour
{
    public enum NodeType { Normal, Special }
    public NodeType nodeType = NodeType.Special;

    [Header("Normal Node")]
    [Tooltip("Сколько ИИ будет стоять на обычной ноде (секунды)")]
    public float normalStandSeconds = 3.5f;

    [Header("Special Node")]
    [Tooltip("Включить поворот при прибытии на спец-ноду")]
    public bool rotateOnArrive = false;

    [Tooltip("Абсолютная Y-ротация (в градусах), которая будет выставлена")]
    public float targetYRotation = 0f;

    [Tooltip("Какой трансформ поворачивать. Если не задано — повернётся сам корень ИИ (объект с node_AIMovement).")]
    public Transform rotateTarget;

    [Header("Special Node: SMR / SFX")]
    [Tooltip("Эти SMR отключить при прибытии")]
    public List<Transform> disableOnArrive = new List<Transform>();
    [Tooltip("Эти SMR включить при прибытии")]
    public List<Transform> enableOnArrive = new List<Transform>();

    [Tooltip("Заморозить ИИ на указанное время (сек) после прибытия на спец-ноду")]
    public float freezeSeconds = 2f;

    public bool hasSound = false;
    public AudioSource specialAudio;

    // --- НОВОЕ: списки для включения/выключения GameObject'ов ---
    [Header("Special Node: GameObject toggles (новое)")]
    [Tooltip("Эти GameObject включить при прибытии (active = true)")]
    public List<GameObject> enableGameObjectOnArrive = new List<GameObject>();
    [Tooltip("Эти GameObject отключить при прибытии (active = false)")]
    public List<GameObject> disableGameObjectOnArrive = new List<GameObject>();

    // ----------------------------- Методы -----------------------------

    /// <summary>
    /// Включить/выключить SkinnedMeshRenderer на Transform и всех детей.
    /// Сохранён оригинальный метод (работает с Transform).
    /// </summary>
    public static void SetSMRActiveOnTransform(Transform t, bool active)
    {
        if (t == null) return;

        var smr = t.GetComponent<SkinnedMeshRenderer>();
        if (smr != null) smr.enabled = active;

        var smrs = t.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var s in smrs) s.enabled = active;
    }

    /// <summary>
    /// Включить/выключить GameObject (activeSelf -> active)
    /// </summary>
    public static void SetActiveOnGameObject(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }

    /// <summary>
    /// Включить/выключить SkinnedMeshRenderer(ы) на GameObject и всех детей.
    /// (Альтернатива для случаев, когда тебе нужно отключать SMR по GameObject'у).
    /// </summary>
    public static void SetSMRActiveOnGameObject(GameObject go, bool active)
    {
        if (go == null) return;

        var smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr != null) smr.enabled = active;

        var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var s in smrs) s.enabled = active;
    }
}
