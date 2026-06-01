using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SimpleMover : MonoBehaviour, IInteractable
{
    [Header("Настройки координат")]
    [Tooltip("На сколько единиц сдвинуть объект (X, Y, Z). Например: 0, 0, 0.5")]
    public Vector3 movementAmount; 
    
    [Tooltip("Если галочка стоит, движение будет учитывать поворот объекта. Если нет - движение строго по мировым осям.")]
    public bool relativeToRotation = true;

    [Tooltip("Скорость движения")]
    public float speed = 2.0f;

    [Header("События")]
    public UnityEvent OnOpen;
    public UnityEvent OnClose;

    // Скрытые переменные для хранения координат
    private Vector3 _startPos;
    private Vector3 _endPos;
    private bool _isOpen = false;
    private Coroutine _currentCoroutine;

    private void Start()
    {
        // 1. Запоминаем текущую координату как стартовую
        _startPos = transform.position;

        // 2. Рассчитываем конечную координату
        if (relativeToRotation)
        {
            // Учитываем поворот объекта (чтобы "Z" было "вперед" относительно шкафа, а не мира)
            _endPos = _startPos + (transform.right * movementAmount.x) 
                                + (transform.up * movementAmount.y) 
                                + (transform.forward * movementAmount.z);
        }
        else
        {
            // Просто прибавляем координаты (строго по миру)
            _endPos = _startPos + movementAmount;
        }
    }

    public void Interact(GameObject caller)
    {
        _isOpen = !_isOpen;

        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);

        if (_isOpen)
        {
            OnOpen?.Invoke();
            _currentCoroutine = StartCoroutine(MoveTo(_endPos));
        }
        else
        {
            OnClose?.Invoke();
            _currentCoroutine = StartCoroutine(MoveTo(_startPos));
        }
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    public string GetInteractText() => _isOpen ? "" : "";
}