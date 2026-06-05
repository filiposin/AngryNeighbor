using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerCatcher : MonoBehaviour
{
    public enum ItemDeathMode
    {
        Drop,    // Предметы выпадают как обычно
        TpBack   // Предметы возвращаются на место спавна
    }

    [Header("Settings")]
    public Transform headTransform;
    public Transform respawnPoint;
    public bool playCatchSound = true;
    public AudioClip catchClip;
    public AudioSource audioSource;
    public bool playCatchAnimation = true;

        [Header("Item Death Mode")]
    [Tooltip("Drop — предметы выпадают. TpBack — предметы возвращаются на место спавна.")]
    public ItemDeathMode itemDeathMode = ItemDeathMode.Drop;

    [Header("Enemy AI")]
    public node_AIMovement enemyMovement;
    public node_AIAnimation aiAnimation;
    private bool isTriggered = false;

    private void Start()
{
    if (enemyMovement == null)
    {
        enemyMovement = GetComponent<node_AIMovement>();
        if (enemyMovement == null) enemyMovement = GetComponentInParent<node_AIMovement>();
    }

    // Попробуем автоматически подставить скрипт анимаций, если поле не заполнено в инспекторе
    if (aiAnimation == null)
    {
        if (enemyMovement != null)
        {
            aiAnimation = enemyMovement.GetComponent<node_AIAnimation>();
            if (aiAnimation == null) aiAnimation = enemyMovement.GetComponentInChildren<node_AIAnimation>();
        }

        if (aiAnimation == null)
        {
            aiAnimation = GetComponent<node_AIAnimation>();
            if (aiAnimation == null) aiAnimation = GetComponentInChildren<node_AIAnimation>();
        }
    }
}


    private void OnTriggerEnter(Collider other)
    {
        TryCatchPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCatchPlayer(other);
    }

    private void TryCatchPlayer(Collider other)
    {
        if (isTriggered) return;
        if (other == null) return;

        if (ModMenuController.Instance != null && ModMenuController.Instance.IsGodModeActive()) return;

        FP_Controller playerController = other.GetComponentInParent<FP_Controller>();

        if (playerController != null)
        {
            isTriggered = true;
            StartCoroutine(CatchRoutine(playerController));
        }
    }

    private IEnumerator CatchRoutine(FP_Controller player)
{
    bool playCatchSequence = playCatchAnimation;

    // 1. БЛОКИРУЕМ СТРЕЛЬБУ СРАЗУ (Фикс бага с убийством во время скримера)
    if (PlayerItemHandler.inst != null)
    {
        PlayerItemHandler.inst.UnequipCurrent(); // Убираем предмет из рук
        PlayerItemHandler.inst.enabled = false;  // Полностью отключаем скрипт рук
    }

    // 2. ОСТАНАВЛИВАЕМ ИИ
    if (enemyMovement != null)
    {
        if (playCatchSequence)
        {
            enemyMovement.StopHunt(999f);
            NavMeshAgent agent = enemyMovement.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
            }
        }
        else
        {
            enemyMovement.ResetAfterCatch(0.5f);
        }
    }

    // 3. БЛОКИРУЕМ ДВИЖЕНИЕ ИГРОКА
    player.canControl = false;
    Rigidbody playerRb = player.GetComponent<Rigidbody>();
    if (playerRb != null)
    {
        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.isKinematic = true;
    }

    // 4. ПОВОРАЧИВАЕМ КАМЕРУ
    FP_CameraLook camLook = player.GetComponentInChildren<FP_CameraLook>();
    if (playCatchSequence && camLook != null && headTransform != null)
    {
        camLook.LookTo(headTransform);
    }

    // 5. ЗВУК
    if (playCatchSound && audioSource != null && catchClip != null)
    {
        audioSource.PlayOneShot(catchClip);
    }

    // 5.1 Проигрываем анимацию "Catch" через явно назначенное поле aiAnimation
    if (playCatchSequence && aiAnimation != null)
    {
        Animator anim = aiAnimation.GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Catch");
    }
    else if (playCatchSequence)
    {
        // fallback: попробуем найти Animator и поставить триггер напрямую
        Animator fallback = null;
        if (enemyMovement != null)
            fallback = enemyMovement.GetComponent<Animator>() ?? enemyMovement.GetComponentInChildren<Animator>();

        if (fallback == null)
            fallback = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (fallback != null)
            fallback.SetTrigger("Catch");
    }

    // 6. ЖДЕМ СЦЕНКУ
    if (playCatchSequence)
        yield return new WaitForSeconds(1.5f);

        // 7. ОБРАБАТЫВАЕМ ПРЕДМЕТЫ
    HandleItemsOnDeath();

    // 8. РЕСПАВН
    if (respawnPoint != null)
    {
        player.transform.position = respawnPoint.position;
        player.transform.rotation = respawnPoint.rotation;
        
        if (camLook != null)
        {
            camLook.transform.localRotation = Quaternion.identity;
            camLook.StopLook();
        }
    }
    if (BlackCock.instance != null) BlackCock.instance.PlayAnimation();
    yield return new WaitForSeconds(0.1f);
    
    // 9. ВОЗВРАЩАЕМ УПРАВЛЕНИЕ
    if (playerRb != null)
    {
        CharacterController characterController = player.GetComponent<CharacterController>();
        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.isKinematic = characterController != null;
    }
    player.canControl = true;
    
    // Включаем руки обратно
    if (PlayerItemHandler.inst != null) PlayerItemHandler.inst.enabled = true;

        // 10. СБРОС ИИ — полный сброс памяти об игроке + игнор-таймер,
    //     чтобы сосед не возобновил погоню сразу после респавна.
    if (enemyMovement != null)
    {
        NavMeshAgent agent = enemyMovement.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = false;
        }
        enemyMovement.ResetAfterCatch(0.5f);
    }

    isTriggered = false;
}


        // ─────────────────────────────────────────────
    //  Обработка предметов при смерти
    // ─────────────────────────────────────────────
    private void HandleItemsOnDeath()
    {
        if (PlayerItemHandler.inst == null)
        {
            Debug.LogWarning("PlayerItemHandler instance is missing, items not handled.");
            return;
        }

        switch (itemDeathMode)
        {
            case ItemDeathMode.Drop:
                PlayerItemHandler.inst.DropAllItems();
                break;

            case ItemDeathMode.TpBack:
                ReturnAllItemsToSpawn();
                break;
        }
    }

    /// <summary>
    /// Возвращает все предметы инвентаря на их позицию спавна и очищает инвентарь.
    /// </summary>
    private void ReturnAllItemsToSpawn()
    {
        var handler   = PlayerItemHandler.inst;
        var inventory = handler.GetInventory();

        if (inventory != null)
        {
            GameObject[] slotModels = handler.GetSlotModels();
            if (slotModels != null)
            {
                for (int i = 0; i < slotModels.Length; i++)
                    TryReturnWorldItem(slotModels[i]);
            }

            if (inventory.BackpackEnabled)
            {
                GameObject[] backpackModels = handler.GetBackpackModels();
                if (backpackModels != null)
                {
                    for (int i = 0; i < backpackModels.Length; i++)
                        TryReturnWorldItem(backpackModels[i]);
                }
            }
        }

        handler.ClearAllInventory();
    }

    private void TryReturnWorldItem(GameObject obj)
    {
        if (obj == null) return;

        var itemBase = obj.GetComponent<ItemBase>();
        if (itemBase != null) itemBase.RestoreLayer();

        var worldItem = obj.GetComponent<WorldItem>();
        if (worldItem != null)
            worldItem.ReturnToSpawnPosition();
        else
        {
            obj.transform.SetParent(null);
            obj.SetActive(false);
        }
    }
}
