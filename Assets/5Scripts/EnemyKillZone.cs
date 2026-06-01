using UnityEngine;

public class EnemyKillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Проверяем, что зашел именно Враг (по тегу)
        if (other.CompareTag("Enemy"))
        {
            // 2. Пытаемся получить его главный скрипт
            RichAI_EnemyController neighbor = other.GetComponent<RichAI_EnemyController>();

            // Если скрипт найден — убиваем!
            if (neighbor != null)
            {
                Debug.Log("Сосед попал в ловушку и умер! ☠️");
                neighbor.TakeDamage(); 
            }
        }
    }
}