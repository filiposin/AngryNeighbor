using UnityEngine;
using System.Collections.Generic; // Просто для примера, System.Reflection мы выкинули.

// ====================================================================
// ВАЖНО: Этот интерфейс должен быть доступен для FP_Controller.
// Лучше создать его в отдельном файле: ICrouchState.cs
// ====================================================================
public interface ICrouchState
{
    // Метод, который FP_Controller обязан реализовать.
    // ЭТО В ТЫСЯЧУ РАЗ БЫСТРЕЕ, ЧЕМ REFLECTION!
    bool IsCrouching();
}
// ====================================================================

[RequireComponent(typeof(FP_Controller))]
[RequireComponent(typeof(FP_FootSteps))]
public class FP_Headbob : MonoBehaviour
{
    // Используем System.Serializable, но сам System не тащим
    [System.Serializable]
    public class HeadBob
    {
        public Transform MainCamera;

        [Header("Классический headbob (твои значения)")]
        public float BobFrequency = 1.5f;
        public float BobHeight = 0.3f;
        public float BobSwayAngle = 0.5f;
        public float BobSideMovement = 0.05f;

        [Header("Скорость / stride")]
        public float heightSpeedMultiplier = 0.3f;
        public float strideSpeedLengthen = 0.3f;

        [Header("Прыжки/приземления")]
        public float jumpLandMove = 0.2f;
        public float jumpLandTilt = 10f;

        [Header("Присед")]
        public float crouchHeightOffset = -0.25f;
        public float crouchTransitionSpeed = 12f;
        public float crouchLandMove = 0.06f;
        public float crouchLandTilt = 3f;

        [Header("Опции поведения")]
        public bool useClassicHeadbob = true;
        public bool mobileOptimizations = true;
        [Range(0.4f, 1f)] public float mobileScale = 0.75f;
    }

    public HeadBob headBob;

    // --- Поля: Кешированные константы и состояние ---
    
    // Пружина
    private float springPos = 0f;
    private float springVelocity = 0f;
    private const float SPRING_VEL_THR = 0.05f;
    private const float SPRING_POS_THR = 0.05f;
    private const float MAX_SPRING = 0.35f;
    private const float springElastic = 1.1f;
    private const float springDampen = 0.8f;
    
    // Цикл
    private Vector3 originalLocalPos;
    private float nextStepTime = 0.5f;
    private float headBobCycle = 0f;
    private float headBobFade = 0f;
    private const float TWO_PI = Mathf.PI * 2f;

    // Скорость
    private Vector3 prevPosition;
    private Vector3 prevVelocity = Vector3.zero;
    private Vector3 velocity;
    private Vector3 velocityChange;
    private bool prevGrounded = true;

    // Присед
    private float crouchBlend = 0f;
    private bool prevCrouch = false;

    // Компоненты (Наш любимый кеш!)
    private AudioSource audioSource;
    private FP_Controller playerController;
    private FP_FootSteps footSteps;
    private ICrouchState crouchProvider; // <-- НАШ ЧИСТЫЙ И БЫСТРЫЙ СПОСОБ ПОЛУЧИТЬ CROUCH!

    void Start()
    {
        // Получаем все компоненты, один раз, как боги!
        playerController = GetComponent<FP_Controller>();
        footSteps = GetComponent<FP_FootSteps>();
        // Пытаемся получить интерфейс для Crouch
        crouchProvider = GetComponent<ICrouchState>(); 

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (headBob.MainCamera == null)
        {
            Debug.LogError("БЛЯТЬ! Засунь камеру в HeadBob.MainCamera в инспекторе.");
            enabled = false;
            return;
        }

        originalLocalPos = headBob.MainCamera.localPosition;
        prevPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (headBob.MainCamera == null) return;

        float dt = Time.fixedDeltaTime;

        // --- Временные переменные (локальные, чтобы не *засрать* класс) ---
        float flatVelocity, strideLengthen, bobFactor, bobSwayFactor, speedHeightFactor, xPos, yPos, xTilt, zTilt;

        // --- скорость (по XZ) ---
        velocity = (transform.position - prevPosition) / Mathf.Max(dt, 1e-6f);
        velocityChange = velocity - prevVelocity;
        prevPosition = transform.position;
        prevVelocity = velocity;

        // --- пружина приземления ---
        springVelocity -= velocityChange.y;
        springVelocity -= springPos * springElastic;
        springVelocity *= springDampen;
        springPos += springVelocity * dt;
        springPos = Mathf.Clamp(springPos, -MAX_SPRING, MAX_SPRING);

        if ((Mathf.Abs(springVelocity) < SPRING_VEL_THR && Mathf.Abs(springPos) < SPRING_POS_THR))
        {
            springVelocity = 0f;
            springPos = 0f;
        }

        // --- mobile scale tweaks (Масштабируем все, что нужно) ---
        float scale = (headBob.mobileOptimizations ? headBob.mobileScale : 1f);
        float bobHeight = headBob.BobHeight * scale; // Переименовал в camelCase, так чище
        float bobSideMovement = headBob.BobSideMovement * scale; 
        float bobSwayAngle = headBob.BobSwayAngle * scale; 
        float jumpLandMove = headBob.jumpLandMove * scale; 
        float jumpLandTilt = headBob.jumpLandTilt * scale;

        // --- classic headbob cycle ---
        // ИСПОЛЬЗУЕМ МАТЕМАТИКУ ВМЕСТО new Vector3()! НЕТ GC ALLOC, БЛЯТЬ!
        flatVelocity = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
        strideLengthen = 1f + (flatVelocity * headBob.strideSpeedLengthen);

        // advance cycle 
        headBobCycle += (flatVelocity / Mathf.Max(strideLengthen, 0.0001f)) * (dt / Mathf.Max(headBob.BobFrequency, 0.0001f));
        if (headBobCycle > 1000f) headBobCycle %= 1f;

        // тригонометрия
        float sin = Mathf.Sin(headBobCycle * TWO_PI);
        bobFactor = sin;
        bobSwayFactor = Mathf.Sin(headBobCycle * TWO_PI + Mathf.PI * 0.5f);
        bobFactor = 1f - (bobFactor * 0.5f + 1f);
        bobFactor *= bobFactor;

        // fade
        if (flatVelocity < 0.1f)
        {
            headBobFade = Mathf.Lerp(headBobFade, 0f, dt * 10f);
        }
        else
        {
            headBobFade = Mathf.Lerp(headBobFade, 1f, dt * 10f);
        }

        speedHeightFactor = 1f + (flatVelocity * headBob.heightSpeedMultiplier);

        // --- составляем финальные смещения ---
        xPos = -bobSideMovement * bobSwayFactor * headBobFade;
        yPos = (springPos * jumpLandMove) + (bobFactor * bobHeight * headBobFade * speedHeightFactor);
        xTilt = -springPos * jumpLandTilt;
        zTilt = bobSwayFactor * bobSwayAngle * headBobFade;

        // --- crouch ---
        bool isCrouching = DetectCrouch();
        crouchBlend = Mathf.MoveTowards(crouchBlend, isCrouching ? 1f : 0f, headBob.crouchTransitionSpeed * dt);

        float crouchOffset = headBob.crouchHeightOffset * crouchBlend;
        float crouchTilt = headBob.crouchLandTilt * crouchBlend;

        // импульсы при входе/выходе из приседа
        if (isCrouching && !prevCrouch)
        {
            springVelocity -= headBob.crouchLandMove * 20f;
        }
        else if (!isCrouching && prevCrouch)
        {
            springVelocity += headBob.crouchLandMove * 10f;
        }
        prevCrouch = isCrouching;

        // --- Применение в зависимости от режима ---
        if (headBob.useClassicHeadbob)
        {
            headBob.MainCamera.localPosition = originalLocalPos + new Vector3(xPos, yPos + crouchOffset, 0f);
            headBob.MainCamera.localRotation = Quaternion.Euler(xTilt + crouchTilt, 0f, zTilt);
        }
        else
        {
            // Сглаживание, если юзер выбрал не "классик"
            float lerpK = 12f * dt;
            Vector3 targetPos = originalLocalPos + new Vector3(xPos, yPos + crouchOffset, 0f);
            Quaternion targetRot = Quaternion.Euler(xTilt + crouchTilt, 0f, zTilt);
            headBob.MainCamera.localPosition = Vector3.Lerp(headBob.MainCamera.localPosition, targetPos, lerpK);
            headBob.MainCamera.localRotation = Quaternion.Slerp(headBob.MainCamera.localRotation, targetRot, lerpK);
        }

        // --- footsteps sync (логика осталась твоя) ---
        if (playerController != null && playerController.IsGrounded())
        {
            if (headBobCycle > nextStepTime)
            {
                // Защита от спама звуков шагов на старте
                if (headBobFade > 0.2f && flatVelocity > 0.1f)
                    footSteps.PlayFootstepSounds(audioSource);
                
                nextStepTime = headBobCycle + 0.5f;
            }
            
            // Если только что приземлились, сброс звука, если цикл уже прошел
            if (!prevGrounded && headBobCycle > 0.5f)
            {
                footSteps.ResetFootstepSounds(audioSource);
            }
            
            prevGrounded = true;
        }
        else
        {
            prevGrounded = false;
        }
    }

    // БЫСТРЫЙ DetectCrouch() БЕЗ СРАНОЙ REFLECTION!
    private bool DetectCrouch()
    {
        // 1. Сначала пытаемся получить состояние от правильного интерфейса
        if (crouchProvider != null)
        {
            return crouchProvider.IsCrouching();
        }

        // 2. Если контроллер не реализует интерфейс, FALLBACK на Input (как было раньше).
        // Это медленно для юзера, но не для CPU! 
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
    }
}