using UnityEngine;

public static class AISoundManager
{
    public static void MakeSound(Vector3 position, float radius)
    {
        // Находим всех врагов в радиусе звука
        Collider[] hits = Physics.OverlapSphere(position, radius);
        
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<node_AIMovement>();
            if (enemy == null) enemy = hit.GetComponentInParent<node_AIMovement>();

            if (enemy != null)
            {
                // Сосем хуй
            }
        }
    }
}