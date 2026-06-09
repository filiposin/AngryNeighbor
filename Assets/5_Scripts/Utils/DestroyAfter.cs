using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
    public bool makeSound = false;
    public Transform soundTransform;
    public float maxTime = 3f;

    private void Start()
    {
        if(soundTransform==null) soundTransform = this.transform;
        if(makeSound) AISoundManager.MakeSound(soundTransform.position, 25f); ;
        StartCoroutine(RemoveCountdown(gameObject));
    }

    private IEnumerator RemoveCountdown(GameObject go)
    {
        yield return new WaitForSeconds(maxTime);
        Destroy(go);
    }
}
