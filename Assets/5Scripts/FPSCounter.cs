using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
public class FPSCounter : MonoBehaviour
{
    public float updateInterval = 0.5f;

    private TMP_Text fpsText;
    private float timeLeft;
    private int frameCount;
    private float accumFps;

    void Awake()
    {
        fpsText = GetComponent<TMP_Text>();
        if (fpsText == null)
        {
            enabled = false;
            return;
        }
        timeLeft = updateInterval;
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        accumFps += 1f / Time.deltaTime;
        frameCount++;

        if (timeLeft <= 0f)
        {
            int fps = Mathf.RoundToInt(accumFps / frameCount);
            fpsText.text = fps.ToString() + " FPS";

            timeLeft = updateInterval;
            accumFps = 0f;
            frameCount = 0;
        }
    }
}
