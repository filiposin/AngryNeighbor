using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiTrans : MonoBehaviour
{
    [Header("Слайдер для управления альфой")]
    public Slider slider;

    [Header("Обычные изображения (UI Image)")]
    public List<Image> images = new List<Image>();

    [Header("Raw Images (UI RawImage)")]
    public List<RawImage> rawImages = new List<RawImage>();

    [Header("Объекты, которые выключаются при нуле")]
    public List<GameObject> toggleObjects = new List<GameObject>();

    // Максимальная альфа (0–255, но ты хочешь 120)
    [Range(0, 255)]
    public float maxAlpha = 120f;

    void Start()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderChanged);
            OnSliderChanged(slider.value);
        }
        else
        {
            Debug.LogWarning("⚠️ Не назначен Slider!");
        }
    }

    void OnSliderChanged(float value)
    {
        // вычисляем альфу (в диапазоне 0–1, но масштабируем до maxAlpha)
        float alpha = Mathf.Clamp01(value) * (maxAlpha / 255f);

        // Меняем альфу у Image
        foreach (var img in images)
        {
            if (img != null)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }

        // Меняем альфу у RawImage
        foreach (var raw in rawImages)
        {
            if (raw != null)
            {
                Color c = raw.color;
                c.a = alpha;
                raw.color = c;
            }
        }

        // Включаем/выключаем указанные объекты
        bool active = value > 0.001f;
        foreach (var obj in toggleObjects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}