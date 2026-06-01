using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DynamicResolutionController : MonoBehaviour
{
    [Header("Пределы масштаба (0.5–1.0)")]
    [Range(0.5f, 1.0f)] public float minScale = 0.6f;
    [Range(0.5f, 1.0f)] public float maxScale = 1.0f;

    [Header("Автоподстройка под нагрузку")]
    public bool autoAdjust = true;
    [Tooltip("Целевой FPS для GPU-бюджета. 60 => ~16.7 мс на кадр.")]
    public int targetFPS = 60;
    [Tooltip("Скорость плавного выхода к целевому масштабу (ед/сек).")]
    public float rampSpeed = 0.5f;
    [Tooltip("Мёртвая зона, чтобы масштаб не дрожал.")]
    [Range(0f, 0.2f)] public float hysteresis = 0.05f;

    [Header("Ручной режим (если autoAdjust = false)")]
    [Range(0.5f, 1.0f)] public float manualScale = 1.0f;

    [Header("Обслуживание камер")]
    [Tooltip("Периодический рескан камер (на случай смерти/респавна/вкл/выкл).")]
    public bool autoScan = true;
    public float scanInterval = 1.0f;

    [Header("Отладка")]
    public bool showOverlay = true;

    private readonly List<Camera> cameras = new List<Camera>();
    private FrameTiming[] timings = new FrameTiming[1];
    private float budgetMs;
    private float targetScale = 1f;
    private float appliedScale = 1f;
    private float lastApplied = -1f;
    private float smoothGpuMs = 0f;
    private float scanTimer = 0f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Camera.onPreCull += OnCameraPreCull;
        budgetMs = 1000f / Mathf.Max(30, targetFPS);
        RefreshCameras();
        ScalableBufferManager.ResizeBuffers(appliedScale, appliedScale);
        lastApplied = appliedScale;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Camera.onPreCull -= OnCameraPreCull;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RefreshCameras();
    }

    void OnCameraPreCull(Camera cam)
    {
        if (!cam) return;
        if (!cameras.Contains(cam))
            cameras.Add(cam);
        cam.allowDynamicResolution = true;
    }

    void RefreshCameras()
    {
        cameras.Clear();
        var found = FindObjectsOfType<Camera>();
        for (int i = 0; i < found.Length; i++)
        {
            var cam = found[i];
            if (!cam) continue;
            cam.allowDynamicResolution = true;
            cameras.Add(cam);
        }
        PruneDeadCameras();
    }

    void PruneDeadCameras()
    {
        for (int i = cameras.Count - 1; i >= 0; i--)
            if (cameras[i] == null)
                cameras.RemoveAt(i);
    }

    void Update()
    {
        if (autoScan)
        {
            scanTimer += Time.unscaledDeltaTime;
            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;
                RefreshCameras();
            }
            else
            {
                PruneDeadCameras();
            }
        }

        budgetMs = 1000f / Mathf.Max(30, targetFPS);

        if (!autoAdjust)
        {
            targetScale = Mathf.Clamp(manualScale, minScale, maxScale);
        }
        else
        {
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
            {
                var gpuMs = (float)timings[0].gpuFrameTime;
                if (!float.IsNaN(gpuMs) && gpuMs > 0)
                    smoothGpuMs = smoothGpuMs <= 0 ? gpuMs : Mathf.Lerp(smoothGpuMs, gpuMs, 0.1f);
            }

            float upper = budgetMs * (1f + hysteresis);
            float lower = budgetMs * (1f - hysteresis);

            if (smoothGpuMs > upper)
                targetScale = Mathf.Max(minScale, targetScale - 0.05f);
            else if (smoothGpuMs < lower)
                targetScale = Mathf.Min(maxScale, targetScale + 0.05f);
        }

        appliedScale = Mathf.MoveTowards(appliedScale, targetScale, rampSpeed * Time.unscaledDeltaTime);
        if (Mathf.Abs(appliedScale - lastApplied) > 0.005f)
        {
            ScalableBufferManager.ResizeBuffers(appliedScale, appliedScale);
            lastApplied = appliedScale;
        }
    }

    void OnGUI()
    {
        if (!showOverlay) return;
        GUI.Label(new Rect(10, 10, 700, 22),
            $"DR scale: {ScalableBufferManager.widthScaleFactor:F2} | GPU: {smoothGpuMs:F1} ms | Target: {budgetMs:F1} ms | Cams: {cameras.Count}");
    }
}
