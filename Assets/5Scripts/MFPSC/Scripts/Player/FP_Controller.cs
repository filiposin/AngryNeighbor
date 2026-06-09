using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(FP_Input))]
[RequireComponent(typeof(FP_CameraLook))]
[RequireComponent(typeof(FP_FootSteps))]
public class FP_Controller : MonoBehaviour, ICrouchState
{
    public bool canControl = true;
    public float gravity = 20.0f;
    public float walkSpeed = 6.0f;
    public float runSpeed = 11.0f;
    public float jumpForce = 8.0f;
    public float crouchSpeed = 2.0F;
    public float crouchHeight = 1.0F;

    // --- FLIGHT MODE START ---
    public bool isFly = false;      // Включить/выключить полет
    public float flySpeed = 10.0f;  // Скорость полета
    private Transform camTransform; // Ссылка на камеру
    // --- FLIGHT MODE END ---

    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;

    public bool airControl = true;
    public bool canCrouch = true;
    public bool canJump = true;
    public bool canRun = true;
    
    public bool onLadder = false;
    public float climbSpeed = 3.0f;

    [Header("Sliding System")]
    public bool enableSliding = true;
    public float slideSpeed = 2.0f;
    public float slideMaxAngle = 85f;
    public float slopeInputInfluence = 0.5f;
    public float edgePushMultiplier = 0.8f;
    public float edgeGravityMultiplier = 0.6f;
    public float edgeInputInfluence = 0.5f;

    [Header("Character Motor Physics")]
    [SerializeField] private bool disableDuplicateBodyColliders = true;

    [HideInInspector]
    public CharacterController controller;

    private Vector3 moveDirection;
    private Vector3 contactPoint;
    private Vector3 hitNormal;
    internal AudioSource JumpLandSource;
    private FP_FootSteps footSteps;
    private Transform myTransform;
    private Rigidbody attachedRigidbody;
    internal FP_Input playerInput;
    private RaycastHit hit;

    private bool playerControl = false;
    private bool isCrouching = false;
    private bool grounded = false;
    private bool sliding = false;
    private bool crouch = false;
    private bool jump = false;
    private bool run = false;

    private int antiBunnyHopFactor = 1;
    private int jumpTimer;
    private int landTimer;
    private int jumpState;
    private int runState;

    private float antiBumpFactor = 0.75F;
    private float inputModifyFactor;
    private float minCrouchHeight;
    private float inputX, inputZ;
    private float defaultHeight;
    private float rayDistance;
    private float slideLimit;
    private float speed;
    private string surfaceTag;
    //public int bublicfps = 60;

    // Новые поля для контроля проверки стояния
    private bool prevIsCrouching = false;
    private bool spaceAboveClear = true; 

    // --- CURSOR LOCK (PC ONLY) ---
    [SerializeField] private bool lockCursorOnPC = true;

    private bool cursorLocked = false;        // фактическое состояние
    private bool pcWantsLock = true;          // то, чего хочет игрок (toggle по Esc)
    private bool lastUseMobileInput = false;  // чтобы ловить переключение мобилка/ПК
    
    public bool IsCursorForcedUnlocked { get; private set; } = false;

    public void ForceCursorUnlock(bool force)
    {
        IsCursorForcedUnlocked = force;
        if (force)
        {
            ApplyCursorState(false);
        }
        else
        {
            HandlePCCursorLock();
        }
    }
    
    void Awake()
    {
        //Application.targetFrameRate = bublicfps;
        //QualitySettings.vSyncCount = 0;
        Input.simulateMouseWithTouches = false;
        controller = GetComponent<CharacterController>();
        attachedRigidbody = GetComponent<Rigidbody>();
        playerInput = GetComponent<FP_Input>();
        footSteps = GetComponent<FP_FootSteps>();
        DisableDuplicateBodyColliders();
        ConfigureAttachedRigidbody();
        
    }
    
    void Start() 
    {
        if (!playerInput.UseMobileInput && lockCursorOnPC)
        {
            pcWantsLock = true;
            ApplyCursorState(true);
        }
        defaultHeight = controller.height;
        minCrouchHeight = crouchHeight > controller.radius * 2 ? crouchHeight : controller.radius * 2;
        myTransform = transform;
        speed = walkSpeed;
        rayDistance = controller.height * 0.5F + controller.radius;
        slideLimit = controller.slopeLimit - 0.1F;
        jumpTimer = antiBunnyHopFactor;
        JumpLandSource = gameObject.AddComponent<AudioSource>();

        spaceAboveClear = true;

        // Находим камеру для полета
        Camera foundCam = GetComponentInChildren<Camera>();
        if (foundCam != null)
            camTransform = foundCam.transform;
        else
            camTransform = Camera.main.transform;
    }
    
    void CalculateMovement()
    {
        ConfigureAttachedRigidbody();

        // --- FLIGHT MODE LOGIC (NO CLIP) ---
        if (isFly)
        {
            if (controller.enabled) controller.enabled = false;
            grounded = false;
            sliding = false;
            moveDirection = Vector3.zero;
            float currentFlySpeed = run ? flySpeed * 2f : flySpeed;
            Vector3 forward = camTransform.forward;
            Vector3 right = camTransform.right;
            Vector3 flightMove = (forward * inputZ + right * inputX).normalized * currentFlySpeed;
            transform.position += flightMove * Time.deltaTime;
            return;
        }
        else
        {
            if (!controller.enabled) { controller.enabled = true; moveDirection = Vector3.zero; }
        }
        // -----------------------------------

        inputModifyFactor = (inputX != 0.0F && inputZ != 0.0F)? 0.7071F : 1.0F;
        
        if (onLadder)
        {
            LadderMovement();
            return;
        }

        if (grounded) {
            sliding = false;
            
            // --- НОВАЯ СИСТЕМА СКОЛЬЖЕНИЯ ---
            Vector3 rayStart = myTransform.position + Vector3.up * 0.1f;
            float realRayDistance = rayDistance + 0.1f; 

            bool isEdgeSliding = false;
            Vector3 edgeSlideDir = Vector3.zero;

            if (enableSliding)
            {
                // 1. Проверяем, стоим ли мы на наклонной поверхности (склоне)
                if (Physics.Raycast(rayStart, Vector3.down, out hit, realRayDistance)) 
                {
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    // Игнорируем почти вертикальные стены
                    if (angle > slideLimit && angle < slideMaxAngle && CanSlide()) 
                    {
                        sliding = true;
                        hitNormal = hit.normal;
                    }
                }
                // 2. Если луч не нашел землю, НО контроллер считается grounded, значит центр в воздухе
                else 
                {
                    if (CanSlide()) 
                    {
                        sliding = true;
                        isEdgeSliding = true;

                        // Отталкиваем игрока от точки контакта с выступом
                        edgeSlideDir = myTransform.position - contactPoint;
                        edgeSlideDir.y = 0; 
                        
                        if (edgeSlideDir.sqrMagnitude < 0.01f) 
                            edgeSlideDir = myTransform.forward;
                            
                        edgeSlideDir.Normalize();
                    }
                }
            }

            speed = isCrouching || !CanStand() ? crouchSpeed : run ? canRun ? runSpeed : walkSpeed : walkSpeed;
            
            if (sliding) 
            {
                Vector3 inputDir = new Vector3(inputX * inputModifyFactor, 0, inputZ * inputModifyFactor);
                inputDir = myTransform.TransformDirection(inputDir) * speed;

                if (isEdgeSliding)
                {
                    // Плавно толкаем наружу от края
                    moveDirection = edgeSlideDir * (walkSpeed * edgePushMultiplier);
                    
                    // Тянем вниз, чтобы игрок не подпрыгивал
                    moveDirection.y = -gravity * edgeGravityMultiplier;

                    // Учитываем инпут только если игрок шагает В ПРОПАСТЬ (помогает быстрее спрыгнуть)
                    if (Vector3.Dot(inputDir, edgeSlideDir) > 0)
                    {
                        moveDirection.x += inputDir.x * edgeInputInfluence;
                        moveDirection.z += inputDir.z * edgeInputInfluence;
                    }

                    // СЕКРЕТ ПРЫЖКОВ ЗДЕСЬ: отключаем управление в воздухе при срыве!
                    // Иначе после потери земли код контроля в воздухе (airControl) 
                    // моментально вернет вас обратно на выступ, вызывая "баунс" 60 раз в секунду.
                    playerControl = false; 
                }
                else
                {
                    // Стандартное соскальзывание по склону горы/крыши
                    hitNormal = hit.normal;
                    moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                    Vector3.OrthoNormalize(ref hitNormal, ref moveDirection);
                    moveDirection *= slideSpeed;
                    moveDirection += inputDir * slopeInputInfluence;
                    
                    // На обычных склонах оставляем немного управления
                    playerControl = true; 
                }
            }
            else
            {
                // Обычная ходьба
                moveDirection = new Vector3(inputX * inputModifyFactor, -antiBumpFactor, inputZ * inputModifyFactor);
                moveDirection = myTransform.TransformDirection(moveDirection) * speed;
                playerControl = true;
            }
            
            if (!jump)
                jumpTimer++;
            else if (canJump && jumpTimer >= antiBunnyHopFactor) 
            {
                moveDirection.y = jumpForce;
                jumpTimer = 0;
                // Чтобы можно было выпрыгнуть из бага скольжения
                sliding = false; 
            }
        }
        else 
        {
            if (airControl && playerControl)
            {
                moveDirection.x = inputX * speed * inputModifyFactor;
                moveDirection.z = inputZ * speed * inputModifyFactor;
                moveDirection = myTransform.TransformDirection(moveDirection);
            }
        }
        
        moveDirection.y -= gravity * Time.deltaTime;
        
        if (controller.enabled)
            grounded = (controller.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    void Update()
    {
        if (!canControl)
        {
            inputX = 0; inputZ = 0;
            run = false; jump = false; crouch = false;
            HandlePCCursorLock();
            return;
        }
        HandlePCCursorLock();
        prevIsCrouching = isCrouching;

        switch (playerInput.UseMobileInput)
        {
            case true:
                // --- ЖЕСТКИЙ ФИКС ДВОЙНОЙ СКОРОСТИ ---

                bool runBtn = playerInput.Run();
                // Если мы на лестнице — не позволяем принудительно форсить вперед через кнопку бега
                bool canRunPhysically = runBtn && canRun && !isCrouching && !onLadder;


                // 2. Логика ПРИОРИТЕТА:
                if (canRunPhysically)
                {
                    // Если держим кнопку бега - МЫ ИГНОРИРУЕМ ВЕРТИКАЛЬНЫЙ ДЖОЙСТИК.
                    // Ставим 1.0f жестко. Теперь (1.0) с джойстика не прибавится.
                    inputZ = 1.0f;
                    run = true;
                }
                else
                {
                    // Кнопка не нажата - слушаем джойстик
                    inputZ = playerInput.MoveInput().z;
                    run = false;
                }

                // Горизонталь (стрейф) оставляем как есть
                inputX = playerInput.MoveInput().x;

                // Остальные инпуты
                crouch = playerInput.Crouch();
                jump = playerInput.Jump();
                // -------------------------------------
                break;

            case false:
                if (isFly)
                {
                    inputX = Input.GetAxisRaw("Horizontal");
                    inputZ = Input.GetAxisRaw("Vertical");
                }
                else
                {
                    inputX = Input.GetAxis("Horizontal");
                    inputZ = Input.GetAxis("Vertical");
                }
                crouch = Input.GetKey(crouchKey);
                run = Input.GetKey(runKey);
                jump = Input.GetKey(jumpKey);
                break;
        }

        if (onLadder) { run = false; }
        
        // Звук прыжка
        if (!isFly && jumpState == 0 && CanStand() && jump && jumpTimer >= antiBunnyHopFactor)
        {
            PlaySound(footSteps.jumpSound, JumpLandSource);
            jumpState++;
        }

        if ((Mathf.Abs((transform.position - contactPoint).magnitude) > 2))
            landTimer = 1;

        bool intendedCrouch = crouch && canCrouch;

        if (prevIsCrouching && !crouch)
        {
            bool canNowStand = PerformStandCheck();
            if (!canNowStand) intendedCrouch = true;
        }

        isCrouching = intendedCrouch;

        if (grounded)
        {
            if (isCrouching)
            {
                controller.center = Vector3.Lerp(controller.center, new Vector3(controller.center.x, -(defaultHeight - minCrouchHeight) / 2, controller.center.z), 15 * Time.deltaTime);
                controller.height = Mathf.Lerp(controller.height, minCrouchHeight, 15 * Time.deltaTime);
            }
            else
            {
                if (CanStand())
                {
                    controller.center = Vector3.Lerp(controller.center, Vector3.zero, 15 * Time.deltaTime);
                    controller.height = Mathf.Lerp(controller.height, defaultHeight, 15 * Time.deltaTime);
                }
                else isCrouching = true;
            }
        }

        CalculateMovement();
    }

    private void OnControllerColliderHit (ControllerColliderHit hit) {
        // Игнорируем удары в режиме полета
        if (isFly) return;

        if (!IsGrounded () && landTimer == 1)
            PlaySound(footSteps.landSound, JumpLandSource);
        landTimer = 0;
        jumpState = 0;
        contactPoint = hit.point;
        surfaceTag = hit.collider.tag;

    }

    private void ConfigureAttachedRigidbody()
    {
        if (attachedRigidbody == null) return;

        attachedRigidbody.freezeRotation = true;
        attachedRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void DisableDuplicateBodyColliders()
    {
        if (!disableDuplicateBodyColliders || controller == null) return;

        Collider[] bodyColliders = GetComponents<Collider>();
        for (int i = 0; i < bodyColliders.Length; i++)
        {
            Collider bodyCollider = bodyColliders[i];
            if (bodyCollider == null || bodyCollider == controller || bodyCollider.isTrigger) continue;

            bodyCollider.enabled = false;
        }
    }

    public void OnLadderEnter()
    {
        onLadder = true;
        controller.slopeLimit = 90f;
        moveDirection = Vector3.zero;
    }

    public void OnLadderExit()
    {
        onLadder = false;
        controller.slopeLimit = 45f;
    }

    public void Climb()
    {
        if (!onLadder) return;
    
        float verticalInput = playerInput.UseMobileInput ? 
            playerInput.MoveInput().z : Input.GetAxis("Vertical");
    
        moveDirection = new Vector3(0, verticalInput * climbSpeed, 0);
    }

    private void LadderMovement()
    {
        float verticalInput = playerInput.UseMobileInput ? 
            playerInput.MoveInput().z : Input.GetAxis("Vertical");
        float horizontalInput = playerInput.UseMobileInput ? 
            playerInput.MoveInput().x : Input.GetAxis("Horizontal");
    
        Vector3 moveDir = camTransform.forward * verticalInput + camTransform.right * horizontalInput;
        
        if (jump && canJump)
        {
            moveDirection = (camTransform.forward * -1.0f + Vector3.up * 1.5f).normalized * jumpForce;
            OnLadderExit();
            controller.Move(moveDirection * Time.deltaTime);
            return;
        }

        if (moveDir.magnitude > 1f) moveDir.Normalize();
    
        if (Mathf.Abs(verticalInput) < 0.1f && Mathf.Abs(horizontalInput) < 0.1f)
        {
            moveDirection = Vector3.zero;
            moveDirection.y -= gravity * 0.1f * Time.deltaTime;
        }
        else
        {
            moveDirection = moveDir * climbSpeed;
        }
    
        controller.Move(moveDirection * Time.deltaTime);
    }
    void PlaySound(AudioClip audio, AudioSource source)
    {
        source.clip = audio;
        if (audio)
            source.Play ();
    }

    internal bool IsGrounded()
    {
        return grounded;
    }

    public bool IsCrouching()
    {
        return crouch;
    }

    internal bool IsRunning()
    {
        return run;
    }

    private bool CanStand()
    {
        return spaceAboveClear;
    }

    private bool PerformStandCheck()
    {
        RaycastHit hitAbove;
        Vector3 origin = controller.bounds.center + Vector3.up * (controller.height / 2f);
        float distance = Mathf.Max(0.05f, defaultHeight - controller.height + 0.05f);

        bool blocked = Physics.Raycast(origin, Vector3.up, out hitAbove, distance);

        #if UNITY_EDITOR
        Debug.DrawRay(origin, Vector3.up * distance, blocked ? Color.red : Color.green, 0.2f);
        #endif

        spaceAboveClear = !blocked;
        return spaceAboveClear;
    }

    private bool CanSlide()
    {
        return new Vector3 (controller.velocity.x, 0, controller.velocity.z).magnitude < walkSpeed/2;
    }

    public string SurfaceTag()
    {
        return surfaceTag;
    }

    // --- NEW METHODS ---

    internal bool IsIdle()
    {
        // В полете используем прямой ввод
        if (isFly) return inputX == 0 && inputZ == 0;

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        return grounded && horizontalVelocity.sqrMagnitude < 0.01f;
    }

    internal bool IsWalking()
    {
        // В полете проверяем ввод
        if (isFly) return (inputX != 0 || inputZ != 0) && !run;

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        return grounded && !isCrouching && !run && horizontalVelocity.sqrMagnitude >= 0.01f;
    }

    internal bool IsCrouched()
    {
        return isCrouching;
    }

    private void ApplyCursorState(bool locked)
    {
        cursorLocked = locked;

        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void HandlePCCursorLock()
    {
        if (!lockCursorOnPC) return;

        bool useMobile = playerInput.UseMobileInput;

        // Переключились на мобилку -> отпускаем курсор и выходим
        if (useMobile)
        {
            lastUseMobileInput = true;
            pcWantsLock = false;          // чтобы при возвращении на ПК не лочило само
            if (cursorLocked) ApplyCursorState(false);
            return;
        }

        if (IsCursorForcedUnlocked)
        {
            if (cursorLocked) ApplyCursorState(false);
            return;
        }

        // Мы в ПК-режиме
        if (lastUseMobileInput) // только что вернулись с мобилки на ПК
        {
            lastUseMobileInput = false;

            // Тут можно выбрать поведение:
            // 1) НЕ лочить автоматически (как ты хочешь) -> оставляем pcWantsLock = false
            // 2) Лочить автоматически при входе в ПК -> поставь pcWantsLock = true;
            pcWantsLock = true; // <-- если хочешь чтобы при старте ПК было залочено
            ApplyCursorState(pcWantsLock);
        }

        // Если управление выключено — отпускаем
        if (!canControl)
        {
            if (cursorLocked) ApplyCursorState(false);
            return;
        }
        else if (cursorLocked != pcWantsLock)
        {
            // Восстанавливаем состояние, когда вернули управление
            ApplyCursorState(pcWantsLock);
        }

        // Toggle только по Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pcWantsLock = !pcWantsLock;
            ApplyCursorState(pcWantsLock);
        }

        // ВАЖНО: никаких "если не залочен — залочить" тут больше нет.
    }
}
