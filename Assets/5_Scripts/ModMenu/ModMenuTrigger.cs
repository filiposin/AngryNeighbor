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
                Debug.Log("Mod menu unlocked via trigger.");

                Destroy(gameObject);
            }
            else
            {
                // Try finding globally just in case
                modMenu = FindFirstObjectByType<ModMenuController>();

                if (modMenu != null)
                {
                    modMenu.EnableModMenu();
                    Debug.Log("Mod menu unlocked via trigger (global find).");

                    Destroy(gameObject);
                }
            }
        }
    }
}