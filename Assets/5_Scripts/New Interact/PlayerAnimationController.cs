using System.Collections;
using System;
using UnityEngine;

public enum PlayerMoveState { Idle, Walk, Run, CrouchIdle, CrouchWalk }

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ссылка на твой FP_Controller. Обязательно назначь в инспекторе!")]
    public FP_Controller fpController;
    
    [Header("Settings")]
    [SerializeField] private float crossFadeDuration = 0.15f; 
    
    private Animator animator;
    private ItemDefinition currentItemDefinition;
    [SerializeField] private Transform animatedHandRoot;
    private Vector3 defaultHandLocalPosition;
    private Quaternion defaultHandLocalRotation;
    private Vector3 defaultHandLocalScale;
    private bool hasDefaultHandPose;
    
    // Хеши для быстродействия
    private int currentAnimHash;
    
    // Блокировка движения анимациями действий (Use, Equip)
    private bool isLockedByAction = false;
    private Coroutine actionCoroutine = null;
    private bool forceNextLocomotionInstant = false;

    // Константы имен дефолтных стейтов
    private const string DEFAULT_IDLE = "Default_Idle"; 
    private const string DEFAULT_WALK = "Default_Walk";
    private const string DEFAULT_RUN = "Default_Run";
    private const string DEFAULT_CROUCH_IDLE = "Default_CrouchIdle";
    private const string DEFAULT_CROUCH_WALK = "Default_CrouchWalk";
    private const string DEFAULT_EQUIP = "Default_Equip"; 
    private const string DEFAULT_USE = "Use"; 
    private const string DEFAULT_DROP = "Drop"; 
    private const int BASE_LAYER = 0;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogError("Animator not found!");
        
        // Автопоиск контроллера, если забыл назначить в инспекторе
        if (fpController == null) fpController = GetComponentInParent<FP_Controller>();
        CacheAnimatedHandPose();
    }
    
    void Update()
    {
        // Если проигрывается важная анимация (Use или Equip), не обновляем анимации ходьбы
        if (isLockedByAction) return;

        if (fpController == null) return;

        // 1. Определяем, какое состояние сейчас у игрока на основе FP_Controller
        string targetState = DetermineCurrentState();

        bool forceInstant = forceNextLocomotionInstant;
        forceNextLocomotionInstant = false;

        // 2. Применяем анимацию. После action-анимаций включаем следующий стейт резко,
        // чтобы не было обратного blend-а из последнего кадра Drop/Use/Equip.
        PlayAnimation(targetState, forceInstant);
    }

    void LateUpdate()
    {
        if (!isLockedByAction)
            RestoreAnimatedHandRootPose();
    }

    /// <summary>
    /// Главная логика выбора анимации на основе физики контроллера
    /// </summary>
    private string DetermineCurrentState()
    {
        // --- ПРИСЕД ---
        if (fpController.IsCrouched())
        {
            float speed = new Vector3(fpController.controller.velocity.x, 0, fpController.controller.velocity.z).magnitude;
            
            if (speed > 0.1f)
                return GetDefaultAnimForState(PlayerMoveState.CrouchWalk);
            else
                return GetDefaultAnimForState(PlayerMoveState.CrouchIdle);
        }
        
        // --- СТОИМ НА МЕСТЕ ---
        if (fpController.IsIdle())
        {
            return GetIdleAnim();
        }

        // --- ХОДЬБА ---
        if (fpController.IsWalking())
        {
            return GetDefaultAnimForState(PlayerMoveState.Walk);
        }

        // --- БЕГ ---
        if (fpController.IsGrounded() && fpController.IsRunning())
        {
            return GetDefaultAnimForState(PlayerMoveState.Run);
        }

        return GetIdleAnim();
    }

    private string GetIdleAnim()
    {
        if (currentItemDefinition == null || string.IsNullOrEmpty(currentItemDefinition.anim_IdleState))
            return DEFAULT_IDLE;

        return currentItemDefinition.anim_IdleState;
    }

    private string GetDefaultAnimForState(PlayerMoveState state)
    {
        switch (state)
        {
            case PlayerMoveState.Idle: return DEFAULT_IDLE;
            case PlayerMoveState.Walk: return DEFAULT_WALK;
            case PlayerMoveState.Run: return DEFAULT_RUN;
            case PlayerMoveState.CrouchIdle: return DEFAULT_CROUCH_IDLE;
            case PlayerMoveState.CrouchWalk: return DEFAULT_CROUCH_WALK;
            default: return DEFAULT_IDLE;
        }
    }

    /// <summary>
    /// Универсальный метод проигрывания.
    /// </summary>
    /// <param name="stateName">Имя стейта</param>
    /// <param name="forceInstant">Если true - Play с 0 кадра (резко). Если false - CrossFade (плавно).</param>
    private void PlayAnimation(string stateName, bool forceInstant)
    {
        bool stateExists;
        int hash = ResolveStateHash(stateName, out stateExists);
        if (!stateExists)
        {
            Debug.LogError($"Animator state '{stateName}' was not found on '{name}'. Check the Hand animator controller.");
            return;
        }
        
        // Если уже играем эту анимацию - выходим (только если это не принудительный мгновенный повтор)
        if (hash == currentAnimHash && !forceInstant) return;

        if (forceInstant)
        {
            // РЕЗКО: Играем сразу с 0-й секунды
            animator.Play(hash, BASE_LAYER, 0f);
        }
        else
        {
            // ПЛАВНО: Смешиваем
            animator.CrossFade(hash, crossFadeDuration, BASE_LAYER);
        }
        
        currentAnimHash = hash;
    }

    private int ResolveStateHash(string stateName, out bool stateExists)
    {
        int shortHash = Animator.StringToHash(stateName);
        if (animator.HasState(BASE_LAYER, shortHash))
        {
            stateExists = true;
            return shortHash;
        }

        int fullHash = Animator.StringToHash(animator.GetLayerName(BASE_LAYER) + "." + stateName);
        if (animator.HasState(BASE_LAYER, fullHash))
        {
            stateExists = true;
            return fullHash;
        }

        stateExists = false;
        return shortHash;
    }

    // =========================================================
    // ПУБЛИЧНЫЕ МЕТОДЫ (ВЫЗЫВАЮТСЯ ИЗ ITEM HANDLER)
    // =========================================================

    public void SetHeldItem(ItemDefinition def)
    {
        if (currentItemDefinition == def) return;

        currentItemDefinition = def;

        // Если убрали предмет - снимаем лок
        if (def == null)
        {
            isLockedByAction = false;
            ResetToDefaultIdlePose();
            return;
        }

        RestoreAnimatedHandRootPose();

        string equipAnim = !string.IsNullOrEmpty(def.anim_EquipState) ? def.anim_EquipState : DEFAULT_EQUIP;
        
        if (actionCoroutine != null) StopCoroutine(actionCoroutine);
        
        // Equip вызываем с forceInstant = true (резко)
        actionCoroutine = StartCoroutine(PlayActionCoroutine(equipAnim, forceInstant: true, fixedDuration: 0.5f)); 
    }

    public void PlayUseAnimation()
    {
        PlayUseAnimation(null);
    }

    public void PlayUseAnimation(Action onComplete)
    {
        string useState = (currentItemDefinition != null && !string.IsNullOrEmpty(currentItemDefinition.anim_UseState))
            ? currentItemDefinition.anim_UseState
            : DEFAULT_USE;

        if (actionCoroutine != null) StopCoroutine(actionCoroutine);
        
        // Use вызываем с forceInstant = true (резко)
        actionCoroutine = StartCoroutine(PlayActionCoroutine(useState, forceInstant: true, fixedDuration: 0f, waitForFinish: true, onComplete: onComplete));
    }

    public void PlayDropAnimation(Action onComplete)
    {
        string dropState = (currentItemDefinition != null && !string.IsNullOrEmpty(currentItemDefinition.anim_DropState))
            ? currentItemDefinition.anim_DropState
            : DEFAULT_DROP;

        if (actionCoroutine != null) StopCoroutine(actionCoroutine);

        actionCoroutine = StartCoroutine(PlayActionCoroutine(dropState, forceInstant: true, fixedDuration: 0f, waitForFinish: true, onComplete: onComplete));
    }

    private void ResetToDefaultIdlePose()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        isLockedByAction = false;
        RestoreAnimatedHandRootPose();
        PlayAnimation(DEFAULT_IDLE, forceInstant: true);
        animator.Update(0f);
        RestoreAnimatedHandRootPose();
        forceNextLocomotionInstant = true;
        currentAnimHash = 0;
    }

    private void CacheAnimatedHandPose()
    {
        if (animatedHandRoot == null)
            animatedHandRoot = transform.Find("hands/Hand");

        if (animatedHandRoot == null)
            animatedHandRoot = transform.Find("Hand");

        if (animatedHandRoot == null) return;

        defaultHandLocalPosition = animatedHandRoot.localPosition;
        defaultHandLocalRotation = animatedHandRoot.localRotation;
        defaultHandLocalScale = animatedHandRoot.localScale;
        hasDefaultHandPose = true;
    }

    private void RestoreAnimatedHandRootPose()
    {
        if (!hasDefaultHandPose || animatedHandRoot == null) return;

        animatedHandRoot.localPosition = defaultHandLocalPosition;
        animatedHandRoot.localRotation = defaultHandLocalRotation;
        animatedHandRoot.localScale = defaultHandLocalScale;
    }

    // =========================================================
    // КОРУТИНА ДЛЯ EQUIP / USE
    // =========================================================
    
    private IEnumerator PlayActionCoroutine(string stateName, bool forceInstant, float fixedDuration = 0f, bool waitForFinish = false, Action onComplete = null)
    {
        isLockedByAction = true;
        
        // Запускаем анимацию (резко или плавно в зависимости от флага)
        PlayAnimation(stateName, forceInstant);
        
        // Ждем 1 кадр, чтобы стейт успел переключиться в аниматоре
        yield return null;

        if (waitForFinish)
        {
            float timer = 0f;
            int targetShortHash = Animator.StringToHash(stateName);
            int targetFullHash = Animator.StringToHash(animator.GetLayerName(BASE_LAYER) + "." + stateName);
            while (timer < 2f) // Защитный таймаут 2 сек
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(BASE_LAYER);
                
                // Проверяем, находимся ли мы все еще в нужной анимации
                // (или в переходе на нее, хотя Play делает это мгновенно)
                if (info.shortNameHash == targetShortHash || info.fullPathHash == targetFullHash)
                {
                    // Если анимация дошла до конца (normalizedTime >= 1.0f или чуть меньше для надежности)
                    if (info.normalizedTime >= 0.95f) 
                        break;
                }
                
                timer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Фиксированное ожидание (например для Equip, если нет четкого конца или анимация зациклена)
            if (fixedDuration > 0)
                yield return new WaitForSeconds(fixedDuration);
            else
                yield return new WaitForSeconds(0.4f); // Дефолт
        }

        isLockedByAction = false;
        actionCoroutine = null;
        
        // Сбрасываем хеш, чтобы в следующем кадре Update мог снова включить Idle/Run
        currentAnimHash = 0; 
        forceNextLocomotionInstant = true;
        onComplete?.Invoke();
    }
}
