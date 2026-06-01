using UnityEngine;

public class HydraulicJackItem : SocketPlaceableItem
{
    public AudioSource pumpSoundSource;
    public AudioClip pumpSoundClip;
    public Animator jackAnimator;
    [Header("Visual Parts")]
    [Tooltip("Объект, который будет двигаться (поршень/рычаг)")]
    [SerializeField] private Transform pistonPart;

    [Header("Animation Settings")]
    [Tooltip("Точка состояния 0% (Сюда кладем Transform с начальной поз, rot, scale)")]
    [SerializeField] private Transform startPoint; 
    
    [Tooltip("Точка состояния 100% (Сюда кладем Transform с конечной поз, rot, scale)")]
    [SerializeField] private Transform endPoint;   

    [Header("Logic")]
    [SerializeField] private float pumpSpeed = 0.1f; // Шаг одного нажатия

    [Header("Debug / State")]
    [Range(0f, 1f)]
    public float currentProgress = 0f; // Публичный Range для тестов в инспекторе

    protected override void Update()
    {
        // Позволяет двигать ползунок в редакторе (в режиме Play) и видеть результат сразу
        // В реальном билде можно убрать или оставить, нагрузки почти нет
        base.Update();
#if UNITY_EDITOR
        if (Application.isPlaying) UpdateVisuals();
#endif
    }

    // Метод вызывается кнопкой UI
    public void PumpIt()
    {
        if (IsPlacedInSocket())
        {
            pumpSoundSource.PlayOneShot(pumpSoundClip);
            jackAnimator.SetTrigger("Pump");
            currentProgress += pumpSpeed;
            currentProgress = Mathf.Clamp01(currentProgress);

            UpdateVisuals();
            UpdateGarageDoor();
        }
    }

    // Проверка, стоит ли домкрат в слоте
    public bool IsPlacedInSocket()
    {
        return currentInstalledSocket != null; 
    }

    private void UpdateVisuals()
    {
        if (pistonPart == null || startPoint == null || endPoint == null) return;

        // Плавный переход (Lerp) позиции, поворота и масштаба
        // Используем local, чтобы анимация работала корректно, когда вы носите домкрат
        pistonPart.localPosition = Vector3.Lerp(startPoint.localPosition, endPoint.localPosition, currentProgress);
        pistonPart.localRotation = Quaternion.Lerp(startPoint.localRotation, endPoint.localRotation, currentProgress);
        pistonPart.localScale = Vector3.Lerp(startPoint.localScale, endPoint.localScale, currentProgress);
    }

    private void UpdateGarageDoor()
    {
        if (currentInstalledSocket != null)
        {
            var link = currentInstalledSocket.GetComponent<GarageSocketLink>();
            if (link != null && link.linkedDoor != null)
            {
                // Передаем прогресс двери
                link.linkedDoor.SetProgress(currentProgress);
            }
        }
    }
    
    // Сброс при поднятии
    public override void OnPickup(GameObject holder)
    {
        base.OnPickup(holder);
        
        // Сбрасываем прогресс в ноль, когда игрока забирает домкрат
        currentProgress = 0f;
        UpdateVisuals();
    }

    // Для удобства настройки в редакторе (без Play Mode)
    private void OnValidate()
    {
        UpdateVisuals();
    }
}