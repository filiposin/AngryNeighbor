using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemKey : ItemBase
{
    [SerializeField] private LayerMask doorMask;
    [SerializeField] private bool consumeOnUse = true;
    private PlayerItemHandler playerItemHandler;

    public override void OnPickup(GameObject holder)
    {
        base.OnPickup(holder);
    }

    public override void OnUse()
    {
        if(playerItemHandler == null) playerItemHandler = PlayerItemHandler.inst;
        
        // Используем камеру из хендлера, если она есть, иначе fallback на main
        Camera cam = playerItemHandler.playerCamera != null ? playerItemHandler.playerCamera : Camera.main;
        
        RaycastHit hit;
        // Используем дистанцию из хендлера для согласованности
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, playerItemHandler.interactDistance, doorMask))
        {
            // Проверяем наличие KeyDoor компонента
            if (hit.collider.TryGetComponent<KeyDoor>(out var door))
            {
                Debug.Log("Trying to unlock door: " + hit.collider.name);
                // Если дверь успешно открылась (ключ подошел), удаляем предмет из рук
                if (door.TryUnlock(definition.id) == true && consumeOnUse) 
                {
                    playerItemHandler.ConsumeHeldItem();
                }
            }
        }
    }
}
