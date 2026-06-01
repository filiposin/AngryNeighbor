using UnityEngine;

public class DialogueOnTrigger : MonoBehaviour
{
    [SerializeField] private float waitTime = 3f;
    [SerializeField] AdvancedDialogueAsset dialogue;
    [SerializeField] private Transform headTransform; // На кого смотреть
    [SerializeField] private bool isOnce;
    private float timer = 0f;
    private bool playerInside = false;
    private DialogueManager dialogueManager;
    
    // Ссылки на компоненты игрока
    private FP_Controller playerController;
    private FP_CameraLook playerCamera;

    private void Start()
    {
        // Находим менеджера диалогов
        dialogueManager = FindObjectOfType<DialogueManager>();

        // Ищем игрока по тегу Player и кэшируем компоненты
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<FP_Controller>();
            playerCamera = player.GetComponent<FP_CameraLook>();
        }
        else
        {
            Debug.LogError("Игрок с тегом 'Player' не найден!");
        }
    }

    void Update()
    {
        if (playerInside)
        {
            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                TriggerDialogue();
                playerInside = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            timer = 0f;
            // Обновим ссылки на случай, если игрок заспавнился позже
            if (playerController == null)
            {
                playerController = other.GetComponent<FP_Controller>();
                playerCamera = other.GetComponent<FP_CameraLook>();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            timer = 0f;
        }
    }

    private void TriggerDialogue()
    {
        if (dialogueManager != null)
        {
            // 1. Останавливаем игрока
            if (playerController != null) 
                playerController.canControl = false;

            // 2. Поворачиваем камеру
            if (playerCamera != null && headTransform != null) 
                playerCamera.LookTo(headTransform);

            // 3. Запускаем диалог
            dialogueManager.StartDialogue(dialogue);
            if (isOnce) Destroy(this);
        }
    }
}