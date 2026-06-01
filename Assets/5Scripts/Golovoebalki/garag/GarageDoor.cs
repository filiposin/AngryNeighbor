using UnityEngine;

public class GarageDoor : MonoBehaviour
{
    [Header("Positions")]
    [SerializeField] private Transform doorTransform; // Сама модель двери
    [SerializeField] private Transform closedPoint;   // Точка закрыто
    [SerializeField] private Transform openPoint;     // Точка открыто
    [SerializeField] private Transform postOpenPoint; // <--- НОВАЯ ТОЧКА (Post Open)

    [Header("Settings")]
    // На каком значении прогресса мы достигаем OpenPoint. 
    // 0.5f означает, что половину пути едем до Open, вторую половину до PostOpen.
    [Range(0.1f, 0.9f)] 
    [SerializeField] private float switchPhase = 0.5f; 

    [Header("State")]
    [Range(0f, 1f)]
    [SerializeField] private float openProgress = 0f;

    private void Update()
    {
        UpdateDoorPosition();
    }

    public void SetProgress(float value)
    {
        openProgress = Mathf.Clamp01(value);
        UpdateDoorPosition();
    }

    void OnValidate()
    {
        UpdateDoorPosition();
    }

    private void UpdateDoorPosition()
    {
        // Проверка на null для основных точек
        if (doorTransform == null || closedPoint == null || openPoint == null)
            return;

        // Если третьей точки нет, работаем по-старому (совместимость)
        if (postOpenPoint == null)
        {
            doorTransform.position = Vector3.Lerp(closedPoint.position, openPoint.position, openProgress);
            doorTransform.rotation = Quaternion.Lerp(closedPoint.rotation, openPoint.rotation, openProgress);
            return;
        }

        // Если третья точка есть, делим путь на два этапа
        if (openProgress <= switchPhase)
        {
            // ЭТАП 1: От Closed до Open
            // Пересчитываем прогресс из диапазона [0 ... switchPhase] в [0 ... 1]
            float t = openProgress / switchPhase;
            
            doorTransform.position = Vector3.Lerp(closedPoint.position, openPoint.position, t);
            doorTransform.rotation = Quaternion.Lerp(closedPoint.rotation, openPoint.rotation, t);
        }
        else
        {
            // ЭТАП 2: От Open до PostOpen
            // Пересчитываем прогресс из диапазона [switchPhase ... 1] в [0 ... 1]
            float t = (openProgress - switchPhase) / (1f - switchPhase);

            doorTransform.position = Vector3.Lerp(openPoint.position, postOpenPoint.position, t);
            doorTransform.rotation = Quaternion.Lerp(openPoint.rotation, postOpenPoint.rotation, t);
        }
    }
}