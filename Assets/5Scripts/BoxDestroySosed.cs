using UnityEngine;

public class BoxDestroySosed : MonoBehaviour
{
    public string hitId = "";

    [Header("Настройки проверки (если нет Rigidbody)")]
    [Tooltip("Радиус проверки коллизий")]
    public float checkRadius = 0.6f;
    [Tooltip("Смещение центра проверки")]
    public Vector3 checkOffset = new Vector3(0, 1f, 0);
    [Tooltip("Слой окна для оптимизации проверки")]
    public LayerMask windowLayer = Physics.AllLayers;
    [Tooltip("Как часто проверять (в сек). 0.1 = 10 раз в сек.")]
    public float checkInterval = 0.1f;

    private Collider[] hitsBuffer = new Collider[5];
    private float timer = 0f;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            
            // Проверяем физику вручную сферой с интервалом
            Vector3 pos = transform.position + transform.TransformDirection(checkOffset);
            int hits = Physics.OverlapSphereNonAlloc(pos, checkRadius, hitsBuffer, windowLayer, QueryTriggerInteraction.Collide);
            
            for (int i = 0; i < hits; i++)
            {
                if (hitsBuffer[i].gameObject != gameObject)
                {
                    ProcessCollision(hitsBuffer[i].gameObject);
                }
            }
        }
    }

    private void ProcessCollision(GameObject targetObj)
    {
        if (!targetObj.CompareTag("Okno")) return;

        var replacer = targetObj.GetComponent<SimpleReplacer>();
        if (replacer != null)
        {
            if (!string.IsNullOrEmpty(hitId))
            {
                replacer.TryReplace(hitId);
            }
            else
            {
                // Если hitId пустой, разбиваем окно моментально
                replacer.Replace();
            }
        }
        else
        {
            Destroy(targetObj);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Vector3 pos = transform.position + transform.TransformDirection(checkOffset);
        Gizmos.DrawWireSphere(pos, checkRadius);
    }
}
