using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DoorEasy : MonoBehaviour, IInteractable
{
    public UnityEvent OnOpen;
    public UnityEvent OnClose;
    public UnityEvent OnLocked;
    public bool isOpen = false;
    public bool isLocked = false;
    
    [Header("Animator & Animations")]
    public Animator animator;
    
    [Tooltip("Название состояния (State) открытия в Аниматоре")]
    public string openAnimationName = "Open";
    
    [Tooltip("Название состояния (State) закрытия в Аниматоре")]
    public string closeAnimationName = "Close";
    
    [Tooltip("Название состояния (State) для закрытой двери")]
    public string lockedAnimationName = "Locked";
    
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
            // Проигрываем анимацию закрытой двери с самого начала (0f), на любом слое (-1)
            if (animator != null && !string.IsNullOrEmpty(lockedAnimationName)) 
                animator.Play(lockedAnimationName, -1, 0f);
                
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
        
        // Жестко запускаем стейт открытия
        if (animator != null && !string.IsNullOrEmpty(openAnimationName)) 
            animator.Play(openAnimationName, -1, 0f);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        OnClose?.Invoke();
        
        // Жестко запускаем стейт закрытия
        if (animator != null && !string.IsNullOrEmpty(closeAnimationName)) 
            animator.Play(closeAnimationName, -1, 0f);
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