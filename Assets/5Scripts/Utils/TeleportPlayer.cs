using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Teleport Position")]
    [SerializeField] private Vector3 targetPosition;

    [Header("Teleport Rotation")]
    [SerializeField] private Vector3 targetEulerRotation;

    public void Teleport()
    {
        if (player == null)
        {
            Debug.LogError("Player is not assigned!");
            return;
        }

        player.position = targetPosition;
        player.rotation = Quaternion.Euler(targetEulerRotation);
    }
}