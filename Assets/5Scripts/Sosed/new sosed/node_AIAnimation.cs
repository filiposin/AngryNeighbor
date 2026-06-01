using System.Collections;
using UnityEngine;

public class node_AIAnimation : MonoBehaviour
{
    [Header("Animation States")]
    public string lostAnimName = "E_Lost";
    public string locomotionStateName = "Blend Tree";
    public string idleStateName = "Idle";

    [Header("Old Blend Tree Parameters")]
    public string forwardFloatName = "Forward";
    public string turnFloatName = "Turn";
    public string speedFloatName = "Speed";
    [SerializeField] private float blendTreeDampTime = 0.12f;
    [SerializeField] private float turnDeadZone = 0.05f;

    [SerializeField] private RichAI_EnemyController r;
    [SerializeField] private float lostAnimLockDuration = 1.0f;
    [SerializeField] private Animator m_Animator;

    private float runThreshold = 3.5f;
    private const float WalkThreshold = 0.15f;
    private float lostAnimLockUntilTime;
    private Vector3 lastPosition;

    private int lostHash;
    private int catchHash;
    private int forwardHash;
    private int turnHash;
    private int speedHash;
    private int idleHash;
    private int walkHash;
    private int runHash;
    private int lostStateHash;
    private int locomotionStateHash;
    private int idleStateHash;

    private bool hasCatchTrigger;
    private bool hasCatchBool;
    private bool hasForwardFloat;
    private bool hasTurnFloat;
    private bool hasSpeedFloat;
    private bool hasIdleBool;
    private bool hasWalkBool;
    private bool hasRunBool;
    private bool hasLostState;
    private bool hasLocomotionState;
    private bool hasIdleState;

    private void Start()
    {
        if (r == null) r = GetComponent<RichAI_EnemyController>();
        if (m_Animator == null) m_Animator = GetComponent<Animator>();

        runThreshold = r != null ? (r.walkSpeed + r.chaseSpeed) * 0.5f : 3.5f;
        SyncLastPose();

        CacheAnimatorData();
    }

    private void CacheAnimatorData()
    {
        if (m_Animator == null) return;

        lostHash = Animator.StringToHash(lostAnimName);
        catchHash = Animator.StringToHash("Catch");
        forwardHash = Animator.StringToHash(forwardFloatName);
        turnHash = Animator.StringToHash(turnFloatName);
        speedHash = Animator.StringToHash(speedFloatName);
        idleHash = Animator.StringToHash("Idle");
        walkHash = Animator.StringToHash("Walk");
        runHash = Animator.StringToHash("Run");

        hasCatchTrigger = HasParameter(catchHash, AnimatorControllerParameterType.Trigger);
        hasCatchBool = HasParameter(catchHash, AnimatorControllerParameterType.Bool);
        hasForwardFloat = HasParameter(forwardHash, AnimatorControllerParameterType.Float);
        hasTurnFloat = HasParameter(turnHash, AnimatorControllerParameterType.Float);
        hasSpeedFloat = HasParameter(speedHash, AnimatorControllerParameterType.Float);
        hasIdleBool = HasParameter(idleHash, AnimatorControllerParameterType.Bool);
        hasWalkBool = HasParameter(walkHash, AnimatorControllerParameterType.Bool);
        hasRunBool = HasParameter(runHash, AnimatorControllerParameterType.Bool);
        hasLostState = TryGetStateHash(lostAnimName, out lostStateHash);
        hasLocomotionState = TryGetStateHash(locomotionStateName, out locomotionStateHash);
        hasIdleState = TryGetStateHash(idleStateName, out idleStateHash);
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        if (m_Animator == null || m_Animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter parameter in m_Animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == type) return true;
        }

        return false;
    }

    private bool TryGetStateHash(string stateName, out int stateHash)
    {
        stateHash = Animator.StringToHash(stateName);
        if (m_Animator == null || m_Animator.runtimeAnimatorController == null) return false;

        if (m_Animator.HasState(0, stateHash)) return true;

        string layerName = m_Animator.GetLayerName(0);
        int fullPathHash = Animator.StringToHash(layerName + "." + stateName);
        if (!m_Animator.HasState(0, fullPathHash)) return false;

        stateHash = fullPathHash;
        return true;
    }

    public void UpdateAnimator(float currentSpeed)
    {
        if (m_Animator == null) return;
        if (Time.time < lostAnimLockUntilTime)
        {
            SyncLastPose();
            return;
        }

        UpdateOldBlendTree(currentSpeed);
    }

    public void UpdateAnimator(float fAmount, float tAmount, float smooth, float animSpeed)
    {
        if (m_Animator == null) return;
        if (Time.time < lostAnimLockUntilTime)
        {
            SyncLastPose();
            return;
        }

        if (hasForwardFloat) m_Animator.SetFloat(forwardHash, fAmount, smooth, Time.deltaTime);
        if (hasTurnFloat) m_Animator.SetFloat(turnHash, tAmount, smooth, Time.deltaTime);
        m_Animator.speed = animSpeed;
        SyncLastPose();
    }

    private void UpdateOldBlendTree(float currentSpeed)
    {
        Vector3 velocity = GetWorldVelocity(currentSpeed);
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);

        float forward = 0f;
        float turn = 0f;

        if (currentSpeed > WalkThreshold)
        {
            forward = Mathf.Clamp01(currentSpeed / Mathf.Max(runThreshold, WalkThreshold));
            turn = Mathf.Clamp(Mathf.Atan2(localVelocity.x, Mathf.Max(Mathf.Abs(localVelocity.z), 0.001f)), -1f, 1f);
        }

        if (Mathf.Abs(turn) < turnDeadZone) turn = 0f;

        if (hasForwardFloat) m_Animator.SetFloat(forwardHash, forward, blendTreeDampTime, Time.deltaTime);
        if (hasTurnFloat) m_Animator.SetFloat(turnHash, turn, blendTreeDampTime, Time.deltaTime);
        if (hasSpeedFloat) m_Animator.SetFloat(speedHash, currentSpeed, blendTreeDampTime, Time.deltaTime);
        UpdateLegacyStateBools(currentSpeed);
    }

    private Vector3 GetWorldVelocity(float currentSpeed)
    {
        Vector3 velocity = Vector3.zero;

        if (r != null && r.ai != null)
        {
            velocity = r.ai.velocity;
        }

        if (velocity.sqrMagnitude <= 0.0001f && Time.deltaTime > 0f)
        {
            velocity = (transform.position - lastPosition) / Time.deltaTime;
        }

        lastPosition = transform.position;

        if (velocity.sqrMagnitude <= 0.0001f && currentSpeed > WalkThreshold)
        {
            velocity = transform.forward * currentSpeed;
        }

        return velocity;
    }

    public void PlayCatch()
    {
        if (m_Animator == null) return;

        if (hasCatchTrigger) m_Animator.SetTrigger(catchHash);
        else if (hasCatchBool) SetBoolForOneShot(catchHash);
    }

    public void PlayLost()
    {
        PlayLost(lostAnimLockDuration);
    }

    public void PlayLost(float lockDuration)
    {
        if (m_Animator == null) return;

        lostAnimLockUntilTime = Time.time + Mathf.Max(0f, lockDuration);
        SetMoveStopped();

        if (hasLostState)
        {
            m_Animator.CrossFade(lostStateHash, 0.05f, 0, 0f);
        }
    }

    public void FinishLost()
    {
        if (m_Animator == null) return;

        lostAnimLockUntilTime = 0f;
        SetMoveStopped();
        ReturnToLocomotion();
    }

    public void SetTired(bool isTired)
    {
        // Kept for old RichAI_EnemyController calls. Tired animation is no longer used.
    }

    public void SetHolding(bool isHolding)
    {
        // Kept for old item scripts/events. Holding animation is no longer used.
    }

    private void SetMoveStopped()
    {
        if (hasForwardFloat) m_Animator.SetFloat(forwardHash, 0f);
        if (hasTurnFloat) m_Animator.SetFloat(turnHash, 0f);
        if (hasSpeedFloat) m_Animator.SetFloat(speedHash, 0f);
        SetLegacyStateBools(true, false, false);
    }

    private void ReturnToLocomotion()
    {
        if (hasLocomotionState)
        {
            m_Animator.CrossFade(locomotionStateHash, 0.05f, 0, 0f);
            return;
        }

        if (hasIdleState)
        {
            m_Animator.CrossFade(idleStateHash, 0.05f, 0, 0f);
        }
    }

    private void UpdateLegacyStateBools(float currentSpeed)
    {
        if (!hasIdleBool && !hasWalkBool && !hasRunBool) return;

        bool isMoving = currentSpeed > WalkThreshold;
        bool isRunning = currentSpeed > runThreshold;
        SetLegacyStateBools(!isMoving, isMoving && !isRunning, isRunning);
    }

    private void SetLegacyStateBools(bool idle, bool walk, bool run)
    {
        if (hasIdleBool) m_Animator.SetBool(idleHash, idle);
        if (hasWalkBool) m_Animator.SetBool(walkHash, walk);
        if (hasRunBool) m_Animator.SetBool(runHash, run);
    }

    private void SetBoolForOneShot(int hash)
    {
        m_Animator.SetBool(hash, true);
        StartCoroutine(ResetBoolNextFrame(hash));
    }

    private IEnumerator ResetBoolNextFrame(int hash)
    {
        yield return null;
        if (m_Animator != null) m_Animator.SetBool(hash, false);
    }

    private void SyncLastPose()
    {
        lastPosition = transform.position;
    }

    private void OnAnimatorMove()
    {
        // RichAI owns movement. Handling this callback prevents root motion
        // from also pushing the old NeighborBody animator object around.
    }
}
