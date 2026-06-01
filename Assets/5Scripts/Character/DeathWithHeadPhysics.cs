using UnityEngine;
using System.Collections;

public class DeathWithHeadPhysics : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Точка респавна")]
    public Transform respawnPoint;

    [Header("Physics Settings")]
    [Tooltip("Сила выстрела головы (взрывной толчок)")]
    public float explosionForce = 15f;

    [Tooltip("Сила кручения головы (чтобы вертелась в воздухе)")]
    public float tumbleForce = 10f;

    [Header("sound")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioSource audioSource;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        // Проверяем тег Player
        if (other.CompareTag("Player"))
        {
            // Пытаемся найти контроллер, даже если триггер задела рука/нога
            FP_Controller controller = other.GetComponentInParent<FP_Controller>();

            if (controller != null)
            {
                isTriggered = true;
                PlayDeadAudio(deathClip);
                StartCoroutine(HeadDropRoutine(controller));
            }
        }
    }
    private void PlayDeadAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    private IEnumerator HeadDropRoutine(FP_Controller controller)
    {
        GameObject player = controller.gameObject;

        // 1. ИЩЕМ FP_CameraLook и playerHead
        FP_CameraLook camLook = player.GetComponentInChildren<FP_CameraLook>();

        if (camLook == null || camLook.PlayerHead == null)
        {
            Debug.LogError("ОШИБКА: Не найден компонент FP_CameraLook или не назначена переменная playerHead!");
            isTriggered = false;
            yield break;
        }

        Transform headTransform = camLook.PlayerHead;
        CharacterController charController = player.GetComponent<CharacterController>();

        // 2. ОТКЛЮЧАЕМ ТОЛЬКО УПРАВЛЕНИЕ (CC остается включенным, тело стоит)
        controller.canControl = false; 

        // Запоминаем исходное положение головы
        Vector3 originalLocalPos = headTransform.localPosition;
        Quaternion originalLocalRot = headTransform.localRotation;

        // 3. ДОБАВЛЯЕМ ФИЗИКУ НА playerHead
        BoxCollider headBox = headTransform.gameObject.AddComponent<BoxCollider>();
        Rigidbody headRb = headTransform.gameObject.AddComponent<Rigidbody>();

        // Настройка коллайдера
        headBox.size = Vector3.one * 0.3f; 

        // Настройка Rigidbody
        headRb.mass = 1f;
        headRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 4. ЛЮТАЯ СИЛА (ВЗРЫВ)
        // Направление: Случайно + немного вверх, чтобы красиво летела дугой
        Vector3 explosionDir = (Random.insideUnitSphere + Vector3.up).normalized;
        
        // Impulse - прикладывает силу мгновенно, как выстрел
        headRb.AddForce(explosionDir * explosionForce, ForceMode.Impulse);
        
        // Добавляем вращение
        headRb.AddTorque(Random.insideUnitSphere * tumbleForce, ForceMode.Impulse);

        // 5. ЖДЕМ 1 СЕКУНДУ (пока голова летает)
        yield return new WaitForSeconds(1.0f);

        // 6. УДАЛЯЕМ ФИЗИКУ
        Destroy(headRb);
        Destroy(headBox);

        // Ждем 1 кадр, чтобы Unity удалила компоненты
        yield return null;

        // Возвращаем голову на шею
        headTransform.localPosition = originalLocalPos;
        headTransform.localRotation = originalLocalRot;

        // 7. ТЕЛЕПОРТАЦИЯ
        if (respawnPoint != null)
        {
            // ВАЖНО: CharacterController нужно выключить на момент переноса, 
            // иначе он может "сопротивляться" телепортации или вернуть игрока назад.
            if (charController != null) charController.enabled = false;

            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;

            // Включаем обратно сразу после переноса
            if (charController != null) charController.enabled = true;
        }
        BlackCock.instance.PlayAnimation();
        // 8. ВОЗВРАЩАЕМ УПРАВЛЕНИЕ
        controller.canControl = true;

        // Пауза перед следующим срабатыванием
        yield return new WaitForSeconds(0.5f);
        isTriggered = false;
    }
}