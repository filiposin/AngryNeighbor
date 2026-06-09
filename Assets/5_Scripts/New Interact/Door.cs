using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Door : MonoBehaviour, IInteractable
{
    public UnityEvent OnOpen;
    public UnityEvent OnClose;
    public UnityEvent OnLocked;
    public bool isOpen = false;
    public bool isLocked = false;
    [Header("Optional animation")]
    public Animator animator;
    public string openTrigger = "Open";
    public string closeTrigger = "Close";
    public string isLockedTrigger = "Locked";
    public float delay = 0.2f;

    private bool isInteractionCoolingDown;

    private void Start()
    {
        if (animator == null) animator = GetComponentInParent<Animator>();
    }

    public IEnumerator Toggle()
    {
        if (isInteractionCoolingDown) yield break;

        isInteractionCoolingDown = true;

        if (isLocked)
        {
            if(animator) animator.SetTrigger(isLockedTrigger);
            OnLocked?.Invoke();
            yield return WaitForInteractionCooldown();
            isInteractionCoolingDown = false;
            yield break;
        }

        if (isOpen) Close();
        else Open();

        yield return WaitForInteractionCooldown();
        isInteractionCoolingDown = false;
    }
    
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        OnOpen?.Invoke();
        if (animator) animator.SetTrigger(openTrigger);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        OnClose?.Invoke();
        if (animator) animator.SetTrigger(closeTrigger);
    }
    
    public virtual void Interact(GameObject caller)
    {
        StartCoroutine(Toggle());
    }
    
    public virtual string GetInteractText() => "";

    private IEnumerator WaitForInteractionCooldown()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        else yield return null;
    }
}
