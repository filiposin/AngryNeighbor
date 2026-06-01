using UnityEngine;

// Это простой скрипт для того, чтобы объект следовал за другим объектом.
// Он используется для того, чтобы предметы в руке следовали повороту камеры максимально плавно.
// Настройки скорости следования и поворота можно изменить в инспекторе.
// Скрипт не обязателен для работы, но делает взаимодействие с предметами более приятным.
public class item_follow : MonoBehaviour
{
    public Transform tgt;
    public float spd = 100f;
    public float rot_spd = 100f;
    public Vector3 offset;
    
    [Header("Rotation Settings")]
    public bool enableRotationFollow = true;

    void LateUpdate()
    {
        if (!tgt) return;
        
        // Следование позиции
        transform.position = Vector3.Lerp(
            transform.position,
            tgt.position + offset,
            Time.deltaTime * spd
        );

        // Следование повороту (если включено)
        if(enableRotationFollow)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                tgt.rotation,
                Time.deltaTime * rot_spd
            );
        }
    }
}