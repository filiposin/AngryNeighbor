using UnityEngine;

public class CenterOnPlayer : MonoBehaviour
{
    private Transform playerTransform;

    void Start()
    {
        // 1. Ищем игрока
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;

            // 2. СРАЗУ встаем на координаты игрока (оставляем только свою высоту Y)
            SnapToPlayer();
        }
        else
        {
            Debug.LogError("Игрок с тегом 'Player' не найден!");
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null) return;

        // 3. Каждый физический кадр жестко привязываем позицию
        SnapToPlayer();
    }

    void SnapToPlayer()
    {
        // Берем X и Z игрока, а Y оставляем свой
        Vector3 newPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.position = newPos;
    }
}