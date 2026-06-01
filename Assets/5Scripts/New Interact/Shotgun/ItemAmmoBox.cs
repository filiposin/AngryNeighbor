using UnityEngine;

public class ItemAmmoBox : MonoBehaviour, IInteractable
{
    public int ammoAmount = 5;
    
    public void Interact(GameObject player)
    {
        // Проверяем, что в руках у игрока
        var handler = PlayerItemHandler.inst;
        if (handler != null && handler.HeldItem != null)
        {
            // Пытаемся получить компонент Shotgun у предмета в руках
            var shotgun = handler.HeldItem.GetComponent<ItemShotgun>();
            if (shotgun != null)
            {
                // Пробуем зарядить
                if (shotgun.TryReload(ammoAmount))
                {
                    Debug.Log("Shotgun reloaded!");
                    Destroy(gameObject); // Удаляем коробку
                }
                else
                {
                    Debug.Log("Ammo full!");
                }
            }
            else
            {
                Debug.Log("Need a shotgun to pick this up!");
            }
        }
        else
        {
             Debug.Log("Hold a shotgun to load ammo!");
        }
    }

    public string GetInteractText()
    {
        return "Interact to Reload Shotgun";
    }
}