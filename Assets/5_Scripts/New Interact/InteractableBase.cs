using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteractableBase : MonoBehaviour, IInteractable
{
    public UnityEvent onInteract;
    public string interactText = "";

    public virtual void Interact(GameObject caller)
    {
        onInteract?.Invoke();
    }

    public virtual string GetInteractText() => interactText;
}