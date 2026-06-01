using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractOnCollide : MonoBehaviour
{
    public UnityEvent onCollide;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            onCollide?.Invoke();
        }
    }
}
