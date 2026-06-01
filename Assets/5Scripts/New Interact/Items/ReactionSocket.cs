using UnityEngine;

// Наследуемся от вашего PlacementSocket, делая этот скрипт ЗАВИСИМЫМ от него
public class ReactionSocket : PlacementSocket
{
    [Header("Настройки реакции")]
    [Tooltip("Объект, у которого изменится слой и включится коллайдер")]
    public GameObject targetObject; 

    [Tooltip("Аниматор, в котором проиграется анимация")]
    public Animator targetAnimator;

    // Переопределяем метод PlaceItem
    public override void PlaceItem(GameObject item)
    {
        // 1. Сначала выполняем логику базового скрипта (занимаем слот, сохраняем item)
        base.PlaceItem(item);

        // 2. Выполняем дополнительную логику
        HandleReaction();
    }

    private void HandleReaction()
    {
        // Логика для объекта
        if (targetObject != null)
        {
            // Устанавливаем слой "Interactable"
            // Важно: Слой должен существовать в настройках Unity (Tags and Layers)
            int layerIndex = LayerMask.NameToLayer("Interactable");
            if (layerIndex != -1)
            {
                targetObject.layer = layerIndex;
            }
            else
            {
                Debug.LogWarning("Слой 'Interactable' не найден в настройках проекта!");
            }

            // Включаем коллайдер
            Collider col = targetObject.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
            else
            {
                Debug.LogWarning($"На объекте {targetObject.name} нет компонента Collider!");
            }
        }

        // Логика для анимации
        if (targetAnimator != null)
        {
            targetAnimator.Play("RedKeyGateOpen");
        }
    }
}