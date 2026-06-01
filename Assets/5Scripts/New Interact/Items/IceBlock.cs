using UnityEngine;

public class IceBlock : MonoBehaviour
{
    [Header("Ice Settings")]
    [SerializeField] private float iceAmount = 100f;
    [Tooltip("Сюда перетащить дочерний объект (модельку), который должен уменьшаться")]
    [SerializeField] private Transform iceVisuals; 

    [Header("Key Reference")]
    [SerializeField] private GameObject hiddenKey;
    [SerializeField] private string interactableLayerName = "Interactable";

    private float maxIceAmount;
    private Vector3 initialVisualScale;

    private void Awake()
    {
        maxIceAmount = iceAmount;
        
        // Если visual не назначен, берем transform (но тогда ключ уменьшится, если он внутри)
        if (iceVisuals == null) 
        {
            iceVisuals = transform;
            Debug.LogWarning("IceVisuals не назначен! Ключ может уменьшаться вместе со льдом.");
        }

        initialVisualScale = iceVisuals.localScale;

        if (hiddenKey != null)
        {
            var keyRb = hiddenKey.GetComponent<Rigidbody>();
            if (keyRb) keyRb.isKinematic = true;
            // Можно выключить коллайдер ключа, чтобы не мешал рейкасту, пока лед целый
            // var keyCol = hiddenKey.GetComponent<Collider>();
            // if (keyCol) keyCol.enabled = false;
        }
    }

    public void Melt(float amount)
    {
        if (iceAmount <= 0) return;

        iceAmount -= amount;

        float percentage = Mathf.Clamp01(iceAmount / maxIceAmount);
        
        // Уменьшаем только визуал!
        iceVisuals.localScale = initialVisualScale * percentage;

        if (iceAmount <= 0)
        {
            ReleaseKey();
            Destroy(gameObject); // Удаляем родителя (IceWrapper)
        }
    }

    private void ReleaseKey()
    {
        if (hiddenKey == null) return;

        // Важно: Вытаскиваем ключ из родителя перед тем как родитель уничтожится
        hiddenKey.transform.SetParent(null);
        
        // Восстанавливаем скейл ключа на всякий случай (1,1,1), если вдруг что-то пошло не так
        hiddenKey.transform.localScale = Vector3.one; 

        var rb = hiddenKey.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }

        // Включаем коллайдер ключа, если выключали
        // var keyCol = hiddenKey.GetComponent<Collider>();
        // if (keyCol) keyCol.enabled = true;

        int layerID = LayerMask.NameToLayer(interactableLayerName);
        if (layerID != -1)
        {
            hiddenKey.layer = layerID;
            foreach (Transform child in hiddenKey.transform)
            {
                child.gameObject.layer = layerID;
            }
        }
    }
}