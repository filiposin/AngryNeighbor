using UnityEngine;

public class Ladder : MonoBehaviour
{
   public float GroundCheckDistance = 1.2f;
   private FP_Controller currentController = null;
   public float topY;
void Start()
{
    Collider col = GetComponent<Collider>();
    if (col != null)
        topY = col.bounds.max.y;
    else
        topY = transform.position.y + 2.0f; // запасный вариант
}
    private void OnTriggerEnter(Collider other)
    {
       if (!other.CompareTag("Player")) return;
       
       // Безопасно получаем компонент при входе
       if (other.TryGetComponent<FP_Controller>(out FP_Controller controller))
       {
           currentController = controller;
           currentController.OnLadderEnter();
       }
    }

    private void OnTriggerStay(Collider other)
{
    if (!other.CompareTag("Player")) return;

    // Если currentController еще не установился, но это тот же игрок — попробуем взять
    if (currentController == null)
    {
        if (!other.TryGetComponent<FP_Controller>(out currentController)) return;
    }

    FP_Controller controller = currentController;
    if (controller == null) return;

    float verticalInput = controller.playerInput.UseMobileInput ?
          controller.playerInput.MoveInput().z : Input.GetAxis("Vertical");

    // 1) Если игрок спускается вниз — проверяем землю от стопы (игнорируя триггеры)
    if (verticalInput < -0.1f)
    {
        // взять позицию стопы (нижняя граница контроллера) и сделать небольшой отступ вверх
        Vector3 footOrigin = controller.controller.bounds.min + Vector3.up * 0.05f;
        // явно игнорируем триггеры, чтобы не "поймать" сам триггер лестницы
        bool hitGround = Physics.Raycast(footOrigin, Vector3.down, GroundCheckDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        #if UNITY_EDITOR
        Debug.DrawRay(footOrigin, Vector3.down * GroundCheckDistance, hitGround ? Color.green : Color.red, 0.2f);
        #endif

        if (hitGround)
        {
            controller.OnLadderExit();
            currentController = null;
            return;
        }
    }

    // 2) Защитный выход сверху: если поднялись выше вершины лестницы — выключаем режим лестницы
    if (verticalInput > 0.1f)
    {
        float playerY = controller.transform.position.y;
        if (playerY >= topY - 0.3f)
        {
            // аккуратно выключаем и (опционально) сдвигаем игрока чуть выше вершины
            controller.OnLadderExit();
            currentController = null;

            // опционально: подвинуть игрока на верх лестницы чтобы не застрять
            Vector3 p = controller.transform.position;
            p.y = topY + 0.05f;
            controller.transform.position = p;
            return;
        }
    }
}

    private void OnTriggerExit(Collider other)
    {
       if (!other.CompareTag("Player")) return;

       // ИСПРАВЛЕНИЕ: Не полагаемся на переменную currentController.
       // Получаем компонент напрямую из того объекта, который вышел из триггера.
       // Это гарантирует, что режим лестницы выключится, даже если ссылка потерялась.
       FP_Controller controller = other.GetComponent<FP_Controller>();
       
       if (controller != null)
       {
          controller.OnLadderExit();
       }

       // Сбрасываем кэш, только если это был текущий контроллер
       if (currentController == controller)
       {
           currentController = null;
       }
    }
}