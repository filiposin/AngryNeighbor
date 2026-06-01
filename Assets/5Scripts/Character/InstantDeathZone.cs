using UnityEngine;

public class InstantDeathZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Точка, куда переместить игрока (Respawn Point)")]
    public Transform respawnPoint;

    [Tooltip("Поворачивать ли игрока лицом туда, куда смотрит точка спавна?")]
    public bool resetRotation = true;

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем тег (убедись, что на игроке стоит тег "Player")
        if (other.CompareTag("Player"))
        {
            Respawn(other.gameObject);
        }
    }

    private void Respawn(GameObject player)
    {
        // 1. Сбрасываем инерцию Rigidbody (чтобы не пролетел сквозь пол на спавне)
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero;
        }

        // 2. Если используется CharacterController (на всякий случай), его надо выключить перед телепортом
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 3. Телепортация
        if (respawnPoint != null)
        {
            player.transform.position = respawnPoint.position;

            if (resetRotation)
            {
                player.transform.rotation = respawnPoint.rotation;
                
                // Если нужно сбросить наклон камеры (вверх/вниз), можно попытаться найти компонент:
                // var camLook = player.GetComponentInChildren<FP_CameraLook>();
                // if (camLook != null) camLook.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            Debug.LogError("Не назначена точка RespawnPoint в скрипте InstantDeathZone!");
        }
        BlackCock.instance.PlayAnimation();
        // Включаем CharacterController обратно
        if (cc != null) cc.enabled = true;
    }
}
