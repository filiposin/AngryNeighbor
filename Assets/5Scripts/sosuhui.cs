using UnityEngine;

public class sosuhui : MonoBehaviour
{
    public float amplitudeX = 1f;
    public float amplitudeY = 1f;
    public float amplitudeZ = 1f;

    public float speedX = 1f;
    public float speedY = 1.5f;
    public float speedZ = 0.5f;

    public float rotationAmplitudeX = 10f;
    public float rotationAmplitudeY = 10f;
    public float rotationAmplitudeZ = 10f;

    public float rotationSpeedX = 1f;
    public float rotationSpeedY = 1.5f;
    public float rotationSpeedZ = 0.5f;

    private Vector3 startPosition;
    private Vector3 startEulerAngles;

    private void Start()
    {
        startPosition = transform.localPosition;
        startEulerAngles = transform.localEulerAngles;
    }

    private void Update()
    {

        float offsetX = Mathf.Sin(Time.time * speedX) * amplitudeX;
        float offsetY = Mathf.Sin(Time.time * speedY) * amplitudeY;
        float offsetZ = Mathf.Sin(Time.time * speedZ) * amplitudeZ;

        transform.localPosition = startPosition + new Vector3(offsetX, offsetY, offsetZ);

        float offsetRotX = Mathf.Sin(Time.time * rotationSpeedX) * rotationAmplitudeX;
        float offsetRotY = Mathf.Sin(Time.time * rotationSpeedY) * rotationAmplitudeY;
        float offsetRotZ = Mathf.Sin(Time.time * rotationSpeedZ) * rotationAmplitudeZ;

        transform.localEulerAngles = startEulerAngles + new Vector3(offsetRotX, offsetRotY, offsetRotZ);
    }
}