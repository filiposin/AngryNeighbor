using UnityEngine;

public class SoselLoose : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter()
    {
        RichAI_EnemyController[] enemies = FindObjectsOfType<RichAI_EnemyController>();
        foreach (var enemy in enemies)
        {
            enemy.ForceStopHunt();
        }
    }
}
