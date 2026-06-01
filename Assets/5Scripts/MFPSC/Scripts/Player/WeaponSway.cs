using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Sway General Settings")]
    [Tooltip("Общая сила раскачивания")]
    public float amount = 4f;
    [Tooltip("Максимальный угол отклонения (Clamp)")]
    public float maxAmount = 10f;
    [Tooltip("Скорость возврата оружия в центр (Slerp speed)")]
    public float smoothAmount = 6f;

    [Header("Axis Sensitivity (0 to 1)")]
    [Range(0f, 1f)] 
    [Tooltip("Чувствительность по горизонтали (влево-вправо)")]
    public float sensitivityX = 1.0f;

    [Range(0f, 1f)] 
    [Tooltip("Чувствительность по вертикали (вверх-вниз)")]
    public float sensitivityY = 1.0f;

    [Header("Mobile Smoothing")]
    [Tooltip("Насколько плавно интерполируется ВВОД с тачпада. Меньше = плавнее, но с задержкой. Больше = резче.")]
    public float inputSmoothSpeed = 5f; 
    [Tooltip("Множитель силы ввода для мобилок")]
    public float mobileInputMultiplier = 2.0f;

    [Header("Movement Tilt (Strafe)")]
    public float tiltAmount = 2f;
    public float maxTilt = 5f;

    [Header("References")]
    public FP_Input playerInput;

    private Quaternion initialRotation;
    
    // Переменные для сглаживания ввода
    private float currentInputX;
    private float currentInputY;
    
    private float moveX; // Strafe input

    void Start()
    {
        initialRotation = transform.localRotation;

        if (playerInput == null)
        {
            playerInput = GetComponentInParent<FP_Input>();
        }
    }

    void Update()
    {
        if (playerInput == null) return;

        CalculateInput();
        UpdateSway();
    }

    void CalculateInput()
    {
        float targetX = 0f;
        float targetY = 0f;

        if (playerInput.UseMobileInput)
        {
            // --- MOBILE LOGIC ---
            Vector2 look = playerInput.LookInput();
            
            // Берем сырой ввод
            float rawX = look.x * mobileInputMultiplier;
            float rawY = look.y * mobileInputMultiplier;

            // Интерполируем ВВОД (Input Smoothing)
            // Это убирает "дрожание" пальца
            currentInputX = Mathf.Lerp(currentInputX, rawX, Time.deltaTime * inputSmoothSpeed);
            currentInputY = Mathf.Lerp(currentInputY, rawY, Time.deltaTime * inputSmoothSpeed);

            targetX = currentInputX;
            targetY = currentInputY;

            // Strafe
            moveX = Mathf.Lerp(moveX, playerInput.MoveInput().x, Time.deltaTime * inputSmoothSpeed);
        }
        else
        {
            // --- PC LOGIC ---
            // На ПК мышь обычно плавная сама по себе, используем raw или легкое сглаживание
            targetX = Input.GetAxis("Mouse X");
            targetY = Input.GetAxis("Mouse Y");
            
            // Для ПК можно сразу присвоить (или тоже сгладить, если хочется "веса")
            currentInputX = targetX; 
            currentInputY = targetY;

            moveX = Input.GetAxis("Horizontal");
        }
    }

    void UpdateSway()
    {
        // 1. Применяем настройки чувствительности осей (Range 0-1)
        // inputX отвечает за поворот вокруг Y (Yaw), поэтому умножаем на sensitivityX
        // inputY отвечает за поворот вокруг X (Pitch), поэтому умножаем на sensitivityY
        float swayX = -currentInputX * amount * sensitivityX;
        float swayY = -currentInputY * amount * sensitivityY;

        // 2. Ограничиваем углы (Clamp)
        swayX = Mathf.Clamp(swayX, -maxAmount, maxAmount);
        swayY = Mathf.Clamp(swayY, -maxAmount, maxAmount);

        // 3. Расчет наклона от стрейфа
        float tiltZ = -moveX * tiltAmount;
        tiltZ = Mathf.Clamp(tiltZ, -maxTilt, maxTilt);

        // 4. Формируем поворот
        // Важно: swayY идет в X (Pitch), swayX идет в Y (Yaw), tiltZ идет в Z (Roll)
        Quaternion finalSwayRotation = Quaternion.Euler(swayY, swayX, tiltZ);

        // 5. Финальное сглаживание самого объекта
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            initialRotation * finalSwayRotation, 
            Time.deltaTime * smoothAmount
        );
    }
}