using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyTeleportTrigger : MonoBehaviour
{
    public node_AIMovement targetEnemy;
    public Transform teleportPoint;
    public float ignorePlayerTime = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (targetEnemy != null && other.gameObject == targetEnemy.gameObject)
        {
            TeleportAndResetEnemy();
        }
    }

    private void TeleportAndResetEnemy()
    {
        if (targetEnemy.agent != null && targetEnemy.agent.isActiveAndEnabled)
        {
            targetEnemy.agent.Warp(teleportPoint.position);
        }
        else
        {
            targetEnemy.transform.position = teleportPoint.position;
        }


        targetEnemy.ResetAfterCatch(ignorePlayerTime);
        
        Debug.Log("Сосед умер");
    }
}