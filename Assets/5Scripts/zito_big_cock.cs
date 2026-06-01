using UnityEngine;

public class zito_big_cock : MonoBehaviour
{
    public float amplitudeX = 10f;
    public float amplitudeY = 10f;
    public float speedX = 1f;
    public float speedY = 1.5f;
    public float rotationAmplitude = 10f;
    public float rotationSpeed = 1f;
    
    private RectTransform rect;
    private Vector2 startAnchoredPos;
    private Quaternion startRotation;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        startAnchoredPos = rect.anchoredPosition;
        startRotation = rect.localRotation;
    }

    private void Update()
    {
        float offsetX = Mathf.Sin(Time.time * speedX) * amplitudeX;
        float offsetY = Mathf.Sin(Time.time * speedY) * amplitudeY;
        rect.anchoredPosition = startAnchoredPos + new Vector2(offsetX, offsetY);
        float rotZ = Mathf.Sin(Time.time * rotationSpeed) * rotationAmplitude;
        rect.localRotation = startRotation * Quaternion.Euler(0f, 0f, rotZ);
    }
}