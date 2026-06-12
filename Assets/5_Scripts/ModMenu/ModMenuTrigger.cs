using UnityEngine;

public class ModMenuTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ModMenuController modMenu = other.GetComponent<ModMenuController>();
            if (modMenu == null)
                modMenu = other.GetComponentInChildren<ModMenuController>();

            if (modMenu != null)
            {
                modMenu.EnableModMenu();
                Destroy(gameObject);
            }
            else
            {
                modMenu = FindFirstObjectByType<ModMenuController>();

                if (modMenu != null)
                {
                    modMenu.EnableModMenu();
                    Destroy(gameObject);
                }
            }
        }
    }
}