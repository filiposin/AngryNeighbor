using UnityEngine;

public static class AISoundManager
{
    // Вызывай это так: AISoundManager.MakeSound(transform.position, 20f);
    public static void MakeSound(Vector3 position, float radius)
    {
        // Находим всех врагов в радиусе звука
        Collider[] hits = Physics.OverlapSphere(position, radius);
        
        foreach (var hit in hits)
        {
            // Ищем компонент контроллера (на самом объекте или родителях)
            var enemy = hit.GetComponent<RichAI_EnemyController>();
            if (enemy == null) enemy = hit.GetComponentInParent<RichAI_EnemyController>();

            if (enemy != null)
            {
                // Сообщаем врагу, где был звук
                enemy.HearSound(position);
            }
        }
    }
}