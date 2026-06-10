using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class WorldItem : MonoBehaviour
{
    public ItemDefinition itemDefinition;

    [Tooltip("Сколько секунд предмет остаётся полностью замороженным после возврата на место")]
    public float freezeDurationAfterReturn = 2f;

    // Запомненное начальное состояние
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private Vector3 _spawnScale;
    private bool _spawnWasKinematic;
    private RigidbodyConstraints _spawnConstraints;
    private bool _spawnStateRecorded;

    private Coroutine _unfreezeCoroutine;

    private void Awake()
    {
        RecordSpawnState();
    }

    /// <summary>
    /// Запоминает текущее положение, поворот и физическое состояние объекта как начальные.
    /// Вызывается автоматически в Awake, но можно вызвать вручную если объект спавнится динамически.
    /// </summary>
        public void RecordSpawnState()
    {
        _spawnPosition      = transform.position;
        _spawnRotation      = transform.rotation;
        _spawnScale         = transform.localScale;
        var rb              = GetComponent<Rigidbody>();
        _spawnWasKinematic  = rb != null && rb.isKinematic;
        _spawnConstraints   = rb != null ? rb.constraints : RigidbodyConstraints.None;
        _spawnStateRecorded = true;
    }

    /// <summary>
    /// Телепортирует предмет на стартовую позицию/ротацию.
    /// Rigidbody сразу замораживается (isKinematic = true), а через
    /// freezeDurationAfterReturn секунд — восстанавливается до исходного значения.
    /// </summary>
            public void ReturnToSpawnPosition()
    {
        if (!_spawnStateRecorded) return;

        // Останавливаем предыдущий анфриз, если он ещё шёл
        if (_unfreezeCoroutine != null)
        {
            StopCoroutine(_unfreezeCoroutine);
            _unfreezeCoroutine = null;
        }

        // Отсоединяем от рук игрока
        transform.SetParent(null);

        // Восстанавливаем масштаб (в руках он мог быть искажён из-за scale handTransform)
        transform.localScale = _spawnScale;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // ЖЁСТКАЯ ЗАМОРОЗКА: kinematic + FreezeAll + Sleep
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
            rb.constraints     = RigidbodyConstraints.FreezeAll;
        }

        // Телепортируем (физика уже выключена)
        transform.position = _spawnPosition;
        transform.rotation = _spawnRotation;

        if (rb != null) rb.Sleep();

        // Включаем коллайдеры и detectCollisions, восстанавливаем слои —
        // чтобы предмет снова можно было поднять
        RestoreInteractableState();

        gameObject.SetActive(true);

        if (rb != null)
            _unfreezeCoroutine = StartCoroutine(UnfreezeAfterDelay(rb));
    }

    private void RestoreInteractableState()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.detectCollisions = true;

        // Включаем все коллайдеры (PlayerItemHandler/ItemBase отключали их при подборе)
        var cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = true;

        // Возвращаем оригинальные слои (на руке был слой Hand)
        var ib = GetComponent<ItemBase>();
        if (ib != null)
        {
            ib.RestoreLayer();
            // Сбрасываем holder, чтобы предмет снова считался "свободным"
            ib.OnDrop();
        }
    }

    public void CancelFreeze()
    {
        if (_unfreezeCoroutine != null)
        {
            StopCoroutine(_unfreezeCoroutine);
            _unfreezeCoroutine = null;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = _spawnConstraints;
        }
    }

    private void OnDisable()
    {
        if (_unfreezeCoroutine != null)
        {
            _unfreezeCoroutine = null;
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = _spawnConstraints;
            }
        }
    }

    private IEnumerator UnfreezeAfterDelay(Rigidbody rb)
    {
        // Ждём один физический кадр чтобы позиция гарантированно применилась
        yield return new WaitForFixedUpdate();

        // Повторно закрепляем позицию — на случай если что-то успело толкнуть
        if (rb != null)
        {
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            transform.localScale = _spawnScale;
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        yield return new WaitForSeconds(freezeDurationAfterReturn);

        if (rb == null) yield break;

        // Финальный фикс позиции перед разморозкой
        transform.position   = _spawnPosition;
        transform.rotation   = _spawnRotation;
        transform.localScale = _spawnScale;

        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Снимаем заморозку — возвращаем оригинальные значения
        rb.constraints = _spawnConstraints;
        rb.isKinematic = _spawnWasKinematic;

        // Если объект не-кинематический — оставляем Sleep, чтобы он не дёрнулся сразу
        if (!_spawnWasKinematic) rb.Sleep();

        _unfreezeCoroutine = null;
    }

    public void InitializeFromDefinition()
    {
        if (itemDefinition == null) return;
        var ib = GetComponent<ItemBase>();
        if (ib != null)
            ib.Initialize(itemDefinition);
    }
}