using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using Unity.Collections;
using Unity.Jobs;

public class RichAI_EnemyController : MonoBehaviour
{
    public enum AIState { Wander, Idle, Chase, Search, InvestigateSound }
    
    [Header("Состояние")]
    [SerializeField] private AIState currentState = AIState.Wander;

    private float ignorePlayerUntilTime = 0f;

    [Header("Зрение (Vision)")]
    public float detectionRange = 30f;
    public float visionAngle = 80f;
    public float verticalVisionAngle = 110f;
    public LayerMask visionMask;
    public Transform eyePivot;
    public float reactionDelay = 0.3f;
    public float instantReactionRange = 5f;
    public float visionPersistence = 1.5f;

    [Header("Движение")]
    public List<Transform> patrolNodes;
    public float walkSpeed = 2.5f;
    public float chaseSpeed = 6.0f;
    public float acceleration = 35f;
    public float safeZoneLostDelay = 1.5f;

    [Header("Поиск")]
    public float searchTime = 8f;
    public LayerMask groundMask = -1;

    [Header("Аудио")]
    public AudioSource chaseAudio;
    public float maxAudioVolume = 1f;

    private node_AIAnimation aiAnim;
    public RichAI ai;
    private Seeker seeker;
    private Transform targetPlayer;

    private NativeArray<RaycastCommand> rayCommands;
    private NativeArray<RaycastHit> rayResults;
    private JobHandle visionJobHandle;
    private bool isJobScheduled = false;

    private Vector3 lastSeenPosition;
    private float stateTimer;
    private bool isFreezing;
    private int currentNodeIndex = 0;
    private float reactionTimer;
    private float persistenceTimer;
    private bool canSeePlayerNow = false;
    private bool isLosingPlayerToSafeZone = false;
    private Vector3 lastPosCheck;
    private float stuckTimer;

    private PatrolNode lastAppliedNode;
    private bool effectsApplied = false;

    private void OnEnable()
    {
        rayCommands = new NativeArray<RaycastCommand>(1, Allocator.Persistent);
        rayResults = new NativeArray<RaycastHit>(1, Allocator.Persistent);
    }

    private void OnDisable()
    {
        if (isJobScheduled) visionJobHandle.Complete();
        if (rayCommands.IsCreated) rayCommands.Dispose();
        if (rayResults.IsCreated) rayResults.Dispose();
    }

    void Start()
    {
        ai = GetComponent<RichAI>();
        seeker = GetComponent<Seeker>();
        aiAnim = GetComponent<node_AIAnimation>();

        if (ai != null)
        {
            ai.FindComponents();
            ai.acceleration = acceleration;
            ai.rotationSpeed = 600f;
            ai.slowWhenNotFacingTarget = false;
            ai.endReachedDistance = 0.1f; 
            ai.slowdownTime = 0f;
            ai.canMove = true;
            ai.isStopped = false;
        }

        if (eyePivot == null) eyePivot = transform;

        FindPlayer();
        GoToNextNode();
    }

    public void TakeDamage()
    {
        // Compatibility stub: this enemy no longer dies from shotgun damage.
    }

    public bool IsChasingPlayer()
    {
        return currentState == AIState.Chase && targetPlayer != null;
    }

    public void OnVictory()
    {
        Debug.Log("[AI] Victory! Resetting...");
        ResetAfterPlayerEscape(10.0f);
    }

    public void ResetAfterCatch()
    {
        ResetAfterPlayerEscape(2.5f);
    }

    private void ResetAfterPlayerEscape(float ignoreDuration)
    {
        StopAllCoroutines();
        isLosingPlayerToSafeZone = false;
        isFreezing = false;
        canSeePlayerNow = false;
        targetPlayer = null;
        reactionTimer = 0f;
        persistenceTimer = 0f;
        lastSeenPosition = Vector3.zero;
        ignorePlayerUntilTime = Time.time + Mathf.Max(0f, ignoreDuration);

        StopChaseAudioNow();

        if (aiAnim != null)
        {
            aiAnim.SetTired(false);
        }

        ResetToPatrol();
    }

    public void LosePlayerToSafeZone()
    {
        if (isLosingPlayerToSafeZone) return;

        if (!IsChasingPlayer()) return;

        StopAllCoroutines();
        StartCoroutine(LosePlayerToSafeZoneRoutine());
    }

    private IEnumerator LosePlayerToSafeZoneRoutine()
    {
        isLosingPlayerToSafeZone = true;

        RevertNodeEffects();
        isFreezing = false;
        canSeePlayerNow = false;
        reactionTimer = 0f;
        persistenceTimer = 0f;
        currentState = AIState.Idle;
        targetPlayer = null;
        lastSeenPosition = Vector3.zero;
        ignorePlayerUntilTime = Time.time + safeZoneLostDelay + 1.0f;

        if (ai != null)
        {
            ai.isStopped = true;
            ai.canMove = false;
            ai.destination = transform.position;
        }

        StopChaseAudioNow();
        if (aiAnim != null) aiAnim.PlayLost(safeZoneLostDelay);

        yield return new WaitForSeconds(safeZoneLostDelay);

        if (aiAnim != null) aiAnim.FinishLost();

        isLosingPlayerToSafeZone = false;
        currentState = AIState.Wander;
        ResetToPatrol();
    }

    private Vector3 GetPlayerGroundPos(Vector3 playerPos)
    {
        RaycastHit hit;
        // Пускаем луч от центра игрока (чуть выше ног) вниз
        // 4.0f - запас высоты (хватит для обычного прыжка)
        if (Physics.Raycast(playerPos + Vector3.up * 0.5f, Vector3.down, out hit, 4.0f, groundMask))
        {
            return hit.point;
        }
        // Если пол не найден (например, игрок падает в пропасть), возвращаем как есть
        return playerPos;
    }

    void Update()
    {
        if (isLosingPlayerToSafeZone)
        {
            UpdateAudio();
            return;
        }

        if (targetPlayer == null) FindPlayer();

        HandleVisionJob();
        UpdateStateMachine();
        UpdateAudio();
        CheckIfStuck();

        if (aiAnim != null && ai != null) aiAnim.UpdateAnimator(ai.velocity.magnitude);
    }

    private void CheckIfStuck()
    {
        if (!EnsureAIReady()) return;

        if (isLosingPlayerToSafeZone || currentState == AIState.Idle || isFreezing || ai.isStopped)
        {
            stuckTimer = 0;
            lastPosCheck = transform.position;
            return;
        }

        if (ai.velocity.magnitude > 0.5f)
        {
            stuckTimer = 0;
            lastPosCheck = transform.position;
            return;
        }

        if (Vector3.Distance(transform.position, lastPosCheck) < 0.1f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 5.0f)
            {
                stuckTimer = 0;
                if (currentState == AIState.Chase) SearchPathIfReady();
                else GoToNextNode();
            }
        }
        else stuckTimer = 0;
        lastPosCheck = transform.position;
    }

    public void HearSound(Vector3 soundPos)
    {
        if (currentState == AIState.Chase) return;
        if (!EnsureAIReady()) return;
        if (currentState == AIState.InvestigateSound && Vector3.Distance(ai.destination, soundPos) < 2.0f) return;

        StopAllCoroutines();
        RevertNodeEffects();
        isFreezing = false;
        if (aiAnim != null) aiAnim.SetTired(false);
        lastSeenPosition = soundPos;
        currentState = AIState.InvestigateSound;
        ai.canMove = true;
        ai.isStopped = false;
        ai.maxSpeed = chaseSpeed;
        ai.destination = GetGroundedTargetPos(soundPos);
        SearchPathIfReady();
    }

    private Vector3 GetTargetOnNavMesh(Vector3 targetPos)
    {
        NNConstraint constraint = NNConstraint.Default;
        constraint.constrainDistance = true; 
        
        NNInfo info = AstarPath.active.GetNearest(targetPos, constraint);
        
        if (Mathf.Abs(info.position.y - targetPos.y) > 3.0f)
        {
             return transform.position; 
        }

        return info.position;
    }

    private void UpdateStateMachine()
    {
        if (isFreezing && currentState != AIState.Chase) return;
        if (!EnsureAIReady()) return;

        switch (currentState)
        {
            case AIState.Wander:
                ai.canMove = true; ai.isStopped = false; ai.maxSpeed = walkSpeed;
                float distToNode = Vector3.Distance(transform.position, ai.destination);
                if (!ai.pathPending && (ai.reachedDestination || distToNode < 1.0f)) CheckPatrolNodeLogic();
                break;

            case AIState.Idle:
                ai.isStopped = true;
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0) ExitIdle();
                break;

            case AIState.Chase:
                if (targetPlayer != null)
                {
                    // ИЗМЕНЕНИЕ: Используем GetPlayerGroundPos перед отправкой в навигацию
                    Vector3 groundPos = GetPlayerGroundPos(targetPlayer.position);
                    ai.destination = GetTargetOnNavMesh(groundPos);
                    
                    ai.canMove = true;
                    ai.isStopped = false;
                    ai.maxSpeed = chaseSpeed;
                    if (Time.frameCount % 15 == 0) SearchPathIfReady();
                }
                break;

            case AIState.Search:
                ai.canMove = true; ai.isStopped = false; ai.maxSpeed = walkSpeed;
                if (ai.reachedDestination || Vector3.Distance(transform.position, ai.destination) < 1.0f)
                {
                    stateTimer -= Time.deltaTime;
                    transform.Rotate(0, 100f * Time.deltaTime, 0);
                    if (stateTimer <= 0) GoToNextNode();
                }
                break;

            case AIState.InvestigateSound:
                ai.canMove = true; ai.isStopped = false; ai.maxSpeed = chaseSpeed;
                if (!ai.pathPending && (ai.reachedDestination || Vector3.Distance(transform.position, ai.destination) < 2.0f)) StartSearch();
                break;
        }
    }

    private Vector3 GetGroundedTargetPos(Vector3 originalPos)
    {
        if (Mathf.Abs(originalPos.y - transform.position.y) > 2.5f) return originalPos;
        RaycastHit hit;
        if (Physics.Raycast(originalPos + Vector3.up * 0.5f, Vector3.down, out hit, 3.0f, groundMask)) return hit.point;
        return originalPos;
    }

    public void StopHunt()
    {
        StopAllCoroutines();
        if (EnsureAIReady())
        {
            ai.isStopped = true;
            ai.canMove = false;
            ai.destination = transform.position;
        }
        currentState = AIState.Idle;
        stateTimer = 5f;

        // ВАЖНО: полностью забываем игрока на время скримера,
        // иначе HandleVisionJob увидит его в следующем же кадре
        // и переведёт состояние обратно в Chase.
        targetPlayer        = null;
        canSeePlayerNow     = false;
        reactionTimer       = 0f;
        persistenceTimer    = 0f;
        lastSeenPosition    = Vector3.zero;
        ignorePlayerUntilTime = Time.time + 999f; // блокируем зрение до ResetAfterCatch

        StopChaseAudioNow();

        if (aiAnim != null) aiAnim.SetTired(false);
    }

    public void ResetToPatrol()
    {
        canSeePlayerNow = false;
        if (EnsureAIReady())
        {
            ai.canMove = true;
            ai.isStopped = false;
        }
        GoToNextNode();
    }

    private bool EnsureAIReady()
    {
        if (ai == null) ai = GetComponent<RichAI>();
        if (ai == null || !ai.gameObject.activeInHierarchy) return false;

        if (!ai.enabled) ai.enabled = true;
        ai.FindComponents();
        return true;
    }

    private void SearchPathIfReady()
    {
        if (EnsureAIReady()) ai.SearchPath();
    }

    private void GoToNextNode()
    {
        RevertNodeEffects();
        if (aiAnim != null) aiAnim.SetTired(false);
        if (!EnsureAIReady()) return;
        if (patrolNodes == null || patrolNodes.Count == 0) return;

        if (patrolNodes.Count > 1)
        {
            int newIndex = currentNodeIndex;
            int attempts = 0;
            while (newIndex == currentNodeIndex && attempts < 10) { newIndex = Random.Range(0, patrolNodes.Count); attempts++; }
            currentNodeIndex = newIndex;
        }
        else currentNodeIndex = 0;

        Vector3 finalTarget = patrolNodes[currentNodeIndex].position;
        ai.canMove = true; ai.isStopped = false; ai.destination = finalTarget; SearchPathIfReady();
        currentState = AIState.Wander;
    }

    private void CheckPatrolNodeLogic()
    {
        if (effectsApplied) return;
        if (patrolNodes.Count == 0 || currentState != AIState.Wander) return;

        Transform nodeTransform = patrolNodes[currentNodeIndex];
        PatrolNode data = nodeTransform.GetComponent<PatrolNode>();

        if (data != null)
        {
            ApplyNodeEffects(data);
            if (data.nodeType == PatrolNode.NodeType.Special) StartCoroutine(FreezeRoutine(data.freezeSeconds));
            else { currentState = AIState.Idle; stateTimer = data.normalStandSeconds; }
        }
        else GoToNextNode();
    }

    private void ApplyNodeEffects(PatrolNode node)
    {
        if (effectsApplied || node == null) return;
        if (node.nodeType == PatrolNode.NodeType.Special && node.rotateOnArrive)
        {
            ai.updateRotation = false;
            Transform rt = node.rotateTarget ? node.rotateTarget : transform;
            rt.rotation = Quaternion.Euler(0, node.targetYRotation, 0);
        }
        foreach (var t in node.disableOnArrive) PatrolNode.SetSMRActiveOnTransform(t, false);
        foreach (var t in node.enableOnArrive) PatrolNode.SetSMRActiveOnTransform(t, true);
        foreach (var go in node.disableGameObjectOnArrive) PatrolNode.SetActiveOnGameObject(go, false);
        foreach (var go in node.enableGameObjectOnArrive) PatrolNode.SetActiveOnGameObject(go, true);
        if (node.hasSound && node.specialAudio) node.specialAudio.Play();
        lastAppliedNode = node; effectsApplied = true;
    }

    private void RevertNodeEffects()
    {
        if (!effectsApplied || lastAppliedNode == null) return;
        if (ai != null) ai.updateRotation = true;
        foreach (var t in lastAppliedNode.disableOnArrive) PatrolNode.SetSMRActiveOnTransform(t, true);
        foreach (var t in lastAppliedNode.enableOnArrive) PatrolNode.SetSMRActiveOnTransform(t, false);
        foreach (var go in lastAppliedNode.disableGameObjectOnArrive) PatrolNode.SetActiveOnGameObject(go, true);
        foreach (var go in lastAppliedNode.enableGameObjectOnArrive) PatrolNode.SetActiveOnGameObject(go, false);
        
        if (lastAppliedNode.hasSound && lastAppliedNode.specialAudio) lastAppliedNode.specialAudio.Stop();
        effectsApplied = false; lastAppliedNode = null;
    }

    private void StartChase()
    {
        RevertNodeEffects();
        StopAllCoroutines();
        isFreezing = false;
        if (aiAnim != null) aiAnim.SetTired(false);
        currentState = AIState.Chase;
        if (!EnsureAIReady()) return;
        ai.canMove = true; ai.isStopped = false; SearchPathIfReady();
    }

    private void StartSearch()
    {
        RevertNodeEffects();
        if (aiAnim != null) aiAnim.SetTired(false);
        currentState = AIState.Search;
        stateTimer = searchTime;
        if (!EnsureAIReady()) return;
        ai.canMove = true;
        ai.isStopped = false;
        ai.destination = lastSeenPosition;
        SearchPathIfReady();
    }

    public void ForceStopHunt()
    {
        // 1. Забываем игрока
        targetPlayer = null;
        canSeePlayerNow = false;
        lastSeenPosition = Vector3.zero;

        // 2. Даем небольшую паузу (3 сек), чтобы он мгновенно не "унюхал" игрока в машине
        ignorePlayerUntilTime = Time.time + 3.0f;

        // 3. Останавливаем все боевые действия
        StopAllCoroutines();
        isLosingPlayerToSafeZone = false;
        isFreezing = false;
        persistenceTimer = 0f;
        reactionTimer = 0f;

        if (aiAnim != null) 
        {
            aiAnim.SetTired(false);
        }

        // 5. Возвращаем в патруль
        currentState = AIState.Wander;
        if (EnsureAIReady()) 
        {
            ai.canMove = true;
            ai.isStopped = false;
        }
        
        // Идем к следующей точке патруля
        GoToNextNode();
    }

    private bool IsBusyState() => false;
    private IEnumerator FreezeRoutine(float t) { isFreezing = true; if (EnsureAIReady()) ai.isStopped = true; yield return new WaitForSeconds(t); isFreezing = false; if (!IsBusyState()) GoToNextNode(); }
    private void ExitIdle() => GoToNextNode();
    
    private void FindPlayer() 
    { 
        if (Time.time < ignorePlayerUntilTime) return;
        
        GameObject p = GameObject.FindGameObjectWithTag("Player"); 
        if (p != null && !SafeZone.Contains(p.transform)) targetPlayer = p.transform; 
    }

    private void HandleVisionJob()
    {
        if (targetPlayer == null) { ApplyVisionResult(false); return; }
        if (SafeZone.Contains(targetPlayer)) { LosePlayerToSafeZone(); ApplyVisionResult(false); return; }
        float distSqr = (targetPlayer.position - eyePivot.position).sqrMagnitude;
        if (distSqr > detectionRange * detectionRange) { ApplyVisionResult(false); return; }
        Vector3 localTarget = eyePivot.InverseTransformPoint(targetPlayer.position);
        if (localTarget.z < 0) { ApplyVisionResult(false); return; }
        float angleH = Mathf.Atan2(Mathf.Abs(localTarget.x), localTarget.z) * Mathf.Rad2Deg;
        float angleV = Mathf.Atan2(Mathf.Abs(localTarget.y), localTarget.z) * Mathf.Rad2Deg;
        if (angleH > visionAngle * 0.5f || angleV > verticalVisionAngle * 0.5f) { ApplyVisionResult(false); return; }
        Vector3 dirToTarget = (targetPlayer.position - eyePivot.position).normalized;
        rayCommands[0] = new RaycastCommand(eyePivot.position, dirToTarget, Mathf.Sqrt(distSqr), visionMask);
        visionJobHandle = RaycastCommand.ScheduleBatch(rayCommands, rayResults, 1);
        isJobScheduled = true;
    }

    private void LateUpdate()
    {
        if (!isJobScheduled) return;
        visionJobHandle.Complete(); isJobScheduled = false;
        bool see = (rayResults[0].collider != null && rayResults[0].collider.CompareTag("Player"));
        ApplyVisionResult(see);
    }

    private void ApplyVisionResult(bool see)
    {
        if (see)
        {
            canSeePlayerNow = true; persistenceTimer = visionPersistence; lastSeenPosition = targetPlayer.position;
            if (!IsBusyState())
            {
                if (Vector3.Distance(transform.position, targetPlayer.position) < instantReactionRange) StartChase();
                else { reactionTimer += Time.deltaTime; if (reactionTimer >= reactionDelay) StartChase(); }
            }
        }
        else
        {
            persistenceTimer -= Time.deltaTime;
            if (persistenceTimer <= 0)
            {
                canSeePlayerNow = false; reactionTimer = 0;
                if (currentState == AIState.Chase) StartSearch();
            }
        }
    }
private void OnDrawGizmosSelected()
    {
        // Если забыли назначить глаз, берем сам объект
        Transform startNode = eyePivot != null ? eyePivot : transform;

        // Цвет зависит от того, видим ли мы игрока прямо сейчас
        Gizmos.color = canSeePlayerNow ? Color.red : Color.green;

        // 1. Рисуем сферу дальности (проволочную)
        Gizmos.DrawWireSphere(startNode.position, detectionRange);

        // 2. Рисуем Фрустум (пирамиду обзора, как у камеры)
        Vector3 origin = startNode.position;
        float halfH = visionAngle * 0.5f;
        float halfV = verticalVisionAngle * 0.5f;

        // Вычисляем 4 угла пирамиды на максимальной дальности
        // Порядок: TopLeft, TopRight, BottomLeft, BottomRight
        Quaternion leftRayRot = Quaternion.Euler(-halfV, -halfH, 0);
        Quaternion rightRayRot = Quaternion.Euler(-halfV, halfH, 0);
        Quaternion downLeftRayRot = Quaternion.Euler(halfV, -halfH, 0);
        Quaternion downRightRayRot = Quaternion.Euler(halfV, halfH, 0);

        // Направления лучей в мировом пространстве
        Vector3 tl = startNode.rotation * leftRayRot * Vector3.forward * detectionRange;
        Vector3 tr = startNode.rotation * rightRayRot * Vector3.forward * detectionRange;
        Vector3 bl = startNode.rotation * downLeftRayRot * Vector3.forward * detectionRange;
        Vector3 br = startNode.rotation * downRightRayRot * Vector3.forward * detectionRange;

        // Рисуем линии от глаз к углам
        Gizmos.DrawLine(origin, origin + tl);
        Gizmos.DrawLine(origin, origin + tr);
        Gizmos.DrawLine(origin, origin + bl);
        Gizmos.DrawLine(origin, origin + br);

        // Рисуем прямоугольник в конце (экран)
        Gizmos.DrawLine(origin + tl, origin + tr); // Верх
        Gizmos.DrawLine(origin + tr, origin + br); // Право
        Gizmos.DrawLine(origin + br, origin + bl); // Низ
        Gizmos.DrawLine(origin + bl, origin + tl); // Лево
        
        // Линия реакции (для красоты, показывает центр взгляда)
        Gizmos.color = new Color(1, 1, 1, 0.3f);
        Gizmos.DrawRay(startNode.position, startNode.forward * detectionRange);
    }
    private void UpdateAudio()
    {
        if (chaseAudio == null) return;
        bool play = currentState == AIState.Chase && !isLosingPlayerToSafeZone;
        float target = play ? maxAudioVolume : 0f;
        chaseAudio.volume = Mathf.MoveTowards(chaseAudio.volume, target, Time.deltaTime * 3f);
        if (target > 0 && !chaseAudio.isPlaying) chaseAudio.Play();
        else if (target <= 0f && chaseAudio.isPlaying && chaseAudio.volume <= 0.01f) chaseAudio.Stop();
    }

    private void StopChaseAudioNow()
    {
        if (chaseAudio == null) return;

        chaseAudio.volume = 0f;
        chaseAudio.Stop();
    }
}
