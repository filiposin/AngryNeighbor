using UnityEngine;

public class DestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] private float seconds = 3f;

    private void Start()
    {
        Destroy(gameObject, seconds);
    }
}