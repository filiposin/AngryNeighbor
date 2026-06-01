using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Safe : InteractableBase
{
    [SerializeField] private GameObject canvasToActivate;
    private FP_Controller playerController;

    private void Start()
    {
        playerController = FindObjectOfType<FP_Controller>();
    }
    
    public override void Interact(GameObject caller)
    {
        onInteract?.Invoke();
        Debug.Log("Safe Interact");
    }
    public override string GetInteractText() => "";
}
