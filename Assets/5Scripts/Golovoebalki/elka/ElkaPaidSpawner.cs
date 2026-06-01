using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class ElkaPaidSpawner : MonoBehaviour
{
    [Header("Настройки спавна")]
    [Tooltip("Префаб елки из папки Project (то, что получит игрок)")]
    [SerializeField] private GameObject elkaPrefab;               // ЧТО спавним (оригинал-префаб)
    [SerializeField] private Transform elkaSpawnPlace;            // ГДЕ спавним
    
    [Header("Цена")]
    [SerializeField] private ItemDefinition acceptableDefinition; // Какую коробку принимаем
    [SerializeField] private int requiredAmount = 3;              // Цена

    [Header("Витрина (Визуал)")]
    [Tooltip("Список объектов на сцене. Они будут просто удаляться.")]
    [SerializeField] private List<GameObject> visualsOnShelf;     // То, что стоит на полке и исчезает

    [Header("События")]
    public UnityEvent OnBought;
    public UnityEvent OnRaskupleno;

    private List<GameObject> collectedItems = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        // Если визуальные елки закончились — считаем, что товар кончился
        if (visualsOnShelf.Count == 0) return;

        if(other.TryGetComponent<WorldItem>(out var def))
        {
            if(def.itemDefinition.id == acceptableDefinition.id)
            {
                if (!collectedItems.Contains(other.gameObject))
                {
                    collectedItems.Add(other.gameObject);
                    Debug.Log($"Оплата: {collectedItems.Count}/{requiredAmount}");

                    if (collectedItems.Count >= requiredAmount)
                    {
                        TrySpawnElka();
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (collectedItems.Contains(other.gameObject))
        {
            collectedItems.Remove(other.gameObject);
        }
    }

    private void TrySpawnElka()
    {
        // 1. Проверки на дурака
        if (elkaPrefab == null)
        {
            Debug.LogError("ОШИБКА: Не назначен 'Elka Prefab' в инспекторе!");
            return;
        }
        if (elkaSpawnPlace == null)
        {
            Debug.LogError("ОШИБКА: Не назначен 'Elka Spawn Place' (точка спавна)!");
            return;
        }
        if (visualsOnShelf.Count == 0) return;

        Debug.Log("Покупка успешна! Спавним елку.");

        // 2. СПАВНИМ НОВУЮ ЕЛКУ (Для игрока)
        // Используем префаб, чтобы она была чистой и новой
        GameObject newElka = Instantiate(elkaPrefab, elkaSpawnPlace.position, Quaternion.identity);
        
        // На всякий случай включаем её и ставим нормальный размер, если вдруг префаб кривой
        newElka.SetActive(true); 
        // newElka.transform.localScale = Vector3.one; // Раскомментируй, если спавнится мелкой/огромной

        // 3. УДАЛЯЕМ ЕЛКУ С ВИТРИНЫ (Визуал)
        GameObject visualToRemove = visualsOnShelf[0];
        if (visualToRemove != null)
        {
            Destroy(visualToRemove);
        }
        visualsOnShelf.RemoveAt(0);

        // 4. УДАЛЯЕМ КОРОБКИ (Оплату)
        foreach (var item in collectedItems)
        {
            if (item != null) Destroy(item);
        }
        collectedItems.Clear();

        // 5. СОБЫТИЯ
        OnBought?.Invoke();

        if (visualsOnShelf.Count == 0)
        {
            Debug.Log("Магазин пуст!");
            OnRaskupleno?.Invoke();
            Destroy(this);
        }
    }
}