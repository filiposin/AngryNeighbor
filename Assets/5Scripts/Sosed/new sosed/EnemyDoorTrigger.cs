using UnityEngine;

public class EnemyDoorTrigger : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float castRadius = 0.5f;
    [SerializeField] private float forwardRange = 2.5f;
    [SerializeField] private float backwardRange = 1.0f;
    [SerializeField] private float rayHeight = 1.0f;
    [SerializeField] private LayerMask doorLayer;

    [Header("Door Logic")]
    [SerializeField] private float closeDelay = 0.8f;
    [SerializeField] private bool debugLogs = true;

    // ИЗМЕНЕНО: Теперь мы используем класс Door вместо IDoorController
    private Door currentDoor; 
    private float looseDoorTimer = 0f;
    private readonly RaycastHit[] hitsBuffer = new RaycastHit[10];

    private void Update()
    {
        Door hitDoor = FindDoorWithXray(); // ИЗМЕНЕНО

        if (hitDoor != null)
        {
            looseDoorTimer = 0f;
            currentDoor = hitDoor;

            // ИЗМЕНЕНО: Используем переменные isOpen и isLocked (с маленькой буквы) из скрипта Door
            if (!hitDoor.isOpen && !hitDoor.isLocked)
            {
                // ИЗМЕНЕНО: Обращаемся напрямую к transform.name
                if (debugLogs) Debug.LogWarning($"<color=green>[DoorRay] OPENING: {hitDoor.transform.name}</color>");
                hitDoor.Open();
            }
        }
        else
        {
            if (currentDoor == null)
                return;

            looseDoorTimer += Time.deltaTime;
            if (looseDoorTimer < closeDelay)
                return;

            // ИЗМЕНЕНО: Используем isOpen
            if (currentDoor.isOpen)
            {
                if (debugLogs) Debug.LogWarning($"<color=cyan>[DoorRay] Time out. Closing: {currentDoor.transform.name}</color>");
                currentDoor.Close();
            }

            currentDoor = null;
            looseDoorTimer = 0f;
        }
    }

    private Door FindDoorWithXray() // ИЗМЕНЕНО
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Vector3 forward = transform.forward;

        int hitsCount = Physics.SphereCastNonAlloc(origin - (forward * 0.5f), castRadius, forward, hitsBuffer, forwardRange, doorLayer, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitsCount; i++)
        {
            if (hitsBuffer[i].transform == transform) continue;

            Door door = GetDoorComponent(hitsBuffer[i].collider);
            if (door != null) return door;
        }

        hitsCount = Physics.SphereCastNonAlloc(origin, castRadius, -forward, hitsBuffer, backwardRange, doorLayer, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitsCount; i++)
        {
            if (hitsBuffer[i].transform == transform) continue;

            Door door = GetDoorComponent(hitsBuffer[i].collider);
            if (door != null) return door;
        }

        return null;
    }

    // ИЗМЕНЕНО: Полностью упрощенный поиск компонента Door
    private Door GetDoorComponent(Collider col)
    {
        if (col == null) return null;

        // GetComponentInParent проверяет сначала сам объект, а если не находит, идет вверх по родителям.
        // Это заменяет весь старый сложный цикл.
        return col.GetComponentInParent<Door>();
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origin, castRadius);
        Gizmos.DrawLine(origin, origin + transform.forward * forwardRange);
        Gizmos.DrawWireSphere(origin + transform.forward * forwardRange, castRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawLine(origin, origin - transform.forward * backwardRange);
    }
}