using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleDeleter : MonoBehaviour
{
    [SerializeField] private float secondsToDelete;
    void Start()
    {
        Delete(secondsToDelete);
    }

    IEnumerator Delete(float secondsToDelete)
    {
        yield return new WaitForSeconds(secondsToDelete);
        Destroy(gameObject);
    }
}
