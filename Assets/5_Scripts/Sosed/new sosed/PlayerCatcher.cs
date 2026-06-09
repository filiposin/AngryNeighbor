using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerCatcher : MonoBehaviour
{
    public enum ItemDeathMode
    {
        Drop,    // Предметы выпадают как обычно
        TpBack
    }

    [Header("Settings")]
    public Transform headTransform;
    public Transform respawnPoint;
    public bool playCatchSound = true;
    public AudioClip catchClip;
    public AudioSource audioSource;
    public bool playCatchAnimation = true;

        [Header("Item Death Mode")]
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
    bool immediateAudioStop = playCatchAnimation || playCatchSound;

    if (PlayerItemHandler.inst != null)
    {
        PlayerItemHandler.inst.UnequipCurrent();
        PlayerItemHandler.inst.enabled = false;
    }

    if (enemyMovement != null)
    {
        if (playCatchSequence)
        {
            enemyMovement.StopHunt(999f, immediateAudioStop);
            NavMeshAgent agent = enemyMovement.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            Vector3 lookDir = player.transform.position - enemyMovement.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                enemyMovement.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
        else
        {
            enemyMovement.ResetAfterCatch(0.5f, immediateAudioStop);
        }
    }

    player.canControl = false;
    Rigidbody playerRb = player.GetComponent<Rigidbody>();
    if (playerRb != null)
    {
        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.isKinematic = true;
    }

    FP_CameraLook camLook = player.GetComponentInChildren<FP_CameraLook>();
    if (playCatchSequence && camLook != null && headTransform != null)
    {
        camLook.LookTo(headTransform);
    }

    if (playCatchSound && audioSource != null && catchClip != null)
    {
        audioSource.PlayOneShot(catchClip);
    }

    if (playCatchSequence && aiAnimation != null)
    {
        Animator anim = aiAnimation.GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("E_Catch");
    }
    else if (playCatchSequence)
    {
        Animator fallback = null;
        if (enemyMovement != null)
            fallback = enemyMovement.GetComponent<Animator>() ?? enemyMovement.GetComponentInChildren<Animator>();

        if (fallback == null)
            fallback = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (fallback != null)
            fallback.SetTrigger("E_Catch");
    }

    if (playCatchSequence && BlackCock.instance != null)
    {
        BlackCock.instance.PlayDarkenAnimation();
    }

    if (playCatchSequence)
        yield return new WaitForSeconds(1.5f);

    HandleItemsOnDeath();

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
    if (BlackCock.instance != null) BlackCock.instance.PlayLightenAnimation();
    yield return new WaitForSeconds(0.1f);
    
    if (playerRb != null)
    {
        CharacterController characterController = player.GetComponent<CharacterController>();
        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.isKinematic = characterController != null;
    }
    player.canControl = true;
    
    if (PlayerItemHandler.inst != null) PlayerItemHandler.inst.enabled = true;

    if (enemyMovement != null)
    {
        NavMeshAgent agent = enemyMovement.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = false;
        }

        Animator resetAnim = null;
        if (aiAnimation != null) resetAnim = aiAnimation.GetComponent<Animator>();
        if (resetAnim == null) resetAnim = enemyMovement.GetComponent<Animator>() ?? enemyMovement.GetComponentInChildren<Animator>();
        if (resetAnim == null) resetAnim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (resetAnim != null) resetAnim.SetTrigger("E_Idle");

        enemyMovement.ResetAfterCatch(0.5f);
    }

    isTriggered = false;
}


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
        if (itemBase != null && itemBase.Definition != null && !itemBase.Definition.tpBackAfterDeath)
        {
            Destroy(obj);
            return;
        }

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
