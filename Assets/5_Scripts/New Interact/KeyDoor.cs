using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class KeyDoor : MonoBehaviour, IInteractable
{
    public UnityEvent OnOpen;
    public UnityEvent OnClose;
    public UnityEvent OnLocked;
    public bool isOpen = false;
    public bool isLocked = true;

    [Header("Optional animation")]
    public Animator animator;
    public string openTrigger = "Open";
    public string closeTrigger = "Close";
    public string isLockedTrigger = "Locked";
    public float delay = 0.2f;
    public string keyId;

    [SerializeField] private float lockDeleteDelay = 5f;
    [SerializeField] private GameObject doorLock;

    [Header("Plank lock")]
    public bool lockedWithPlanks = false;
    public List<GameObject> planks = new List<GameObject>();

    private bool isLockUsed;
    private readonly Dictionary<GameObject, UnityAction> plankBreakHandlers = new Dictionary<GameObject, UnityAction>();

    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked || IsPlankLockActive();
    public Transform DoorTransform => transform;

    private void OnEnable()
    {
        RegisterPlankBreakHandlers();
        RefreshPlankLock();
    }

    private void Start()
    {
        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    private void OnDisable()
    {
        UnregisterPlankBreakHandlers();
    }

    public IEnumerator Toggle()
    {
        yield return new WaitForSeconds(delay);
        if (IsLocked)
        {
            PlayBlockedFeedback();
            yield break;
        }

        if (isOpen) Close();
        else Open();
    }

    private IEnumerator LockRemove(GameObject lockGameObject)
    {
        yield return new WaitForSeconds(lockDeleteDelay);
        lockGameObject.SetActive(false);
    }

    public void Open()
    {
        if (IsLocked)
        {
            PlayBlockedFeedback();
            return;
        }

        if (isOpen) return;

        isOpen = true;
        OnOpen?.Invoke();
        if (animator) animator.SetTrigger(openTrigger);
        ReleaseLockIfNeeded();
    }

    private void ReleaseLockIfNeeded()
    {
        if (isLockUsed)
        {
            isLocked = false;
            return;
        }

        if (doorLock != null)
        {
            if (doorLock.GetComponent<Rigidbody>() == null)
                doorLock.AddComponent<Rigidbody>();
            StartCoroutine(LockRemove(doorLock));
        }

        isLockUsed = true;
        isLocked = false;
    }

    public void UnlockWithoutOpening()
    {
        ReleaseLockIfNeeded();
    }

    public bool TryUnlock(string id)
    {
        if (id != keyId)
            return false;

        ReleaseLockIfNeeded();
        if (IsPlankLockActive())
            PlayBlockedFeedback();
        else
            Open();

        return true;
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        OnClose?.Invoke();
        if (animator) animator.SetTrigger(closeTrigger);
    }

    public void PlayBlockedFeedback()
    {
        if (animator) animator.SetTrigger(isLockedTrigger);
        OnLocked?.Invoke();
    }

    private bool IsPlankLockActive()
    {
        if (!lockedWithPlanks)
            return false;

        RefreshPlankLock();
        return planks.Count > 0;
    }

    private void RegisterPlankBreakHandlers()
    {
        UnregisterPlankBreakHandlers();

        if (planks == null)
            return;

        for (int i = 0; i < planks.Count; i++)
        {
            RegisterPlankBreakHandler(planks[i]);
        }
    }

    private void RegisterPlankBreakHandler(GameObject plank)
    {
        if (plank == null || plankBreakHandlers.ContainsKey(plank))
            return;

        SimpleReplacer replacer = FindPlankReplacer(plank);
        if (replacer == null)
            return;

        UnityAction handler = () => RemovePlank(plank);
        plankBreakHandlers.Add(plank, handler);
        replacer.onBreak.AddListener(handler);
    }

    private void UnregisterPlankBreakHandlers()
    {
        foreach (var pair in plankBreakHandlers)
        {
            if (pair.Key == null)
                continue;

            SimpleReplacer replacer = FindPlankReplacer(pair.Key);
            if (replacer != null)
                replacer.onBreak.RemoveListener(pair.Value);
        }

        plankBreakHandlers.Clear();
    }

    private SimpleReplacer FindPlankReplacer(GameObject plank)
    {
        SimpleReplacer replacer = plank.GetComponent<SimpleReplacer>();
        if (replacer == null)
            replacer = plank.GetComponentInChildren<SimpleReplacer>();
        if (replacer == null)
            replacer = plank.GetComponentInParent<SimpleReplacer>();

        return replacer;
    }

    private void RemovePlank(GameObject plank)
    {
        if (planks != null)
            planks.Remove(plank);

        if (plank == null)
            return;

        if (plankBreakHandlers.TryGetValue(plank, out UnityAction handler))
        {
            SimpleReplacer replacer = FindPlankReplacer(plank);
            if (replacer != null)
                replacer.onBreak.RemoveListener(handler);

            plankBreakHandlers.Remove(plank);
        }
    }

    private void RefreshPlankLock()
    {
        if (planks == null)
            return;

        for (int i = planks.Count - 1; i >= 0; i--)
        {
            GameObject plank = planks[i];
            if (plank == null)
            {
                planks.RemoveAt(i);
                continue;
            }

            if (!plank.activeInHierarchy)
                RemovePlank(plank);
        }

        for (int i = 0; i < planks.Count; i++)
        {
            RegisterPlankBreakHandler(planks[i]);
        }
    }

    public virtual void Interact(GameObject caller)
    {
        StartCoroutine(Toggle());
    }

    public virtual string GetInteractText() => "";
}
