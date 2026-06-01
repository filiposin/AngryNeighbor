using UnityEngine;

public class WorldBoundary : MonoBehaviour
{
    [Header("Настройки телепортации")]
    // Координаты, которые вы указали
    private Vector3 targetPosition = new Vector3(4.28999996f, 1.24300003f, -13.0699997f);

    [Header("Локализация")]
    public LocalizedLine warningMessage; // Сюда впишем текст в инспекторе

    [Header("Настройки UI")]
    public float messageDuration = 4.0f; // Сколько секунд висит текст
    public int fontSize = 30; // Размер шрифта

    private bool showMessage = false;
    private float timer = 0f;
    private GUIStyle warningStyle;

    void Awake()
    {
        // Настройка стиля текста (Красный, большой, по центру)
        warningStyle = new GUIStyle();
        warningStyle.normal.textColor = Color.red;
        warningStyle.fontSize = fontSize;
        warningStyle.alignment = TextAnchor.UpperCenter;
        warningStyle.fontStyle = FontStyle.Bold;
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, есть ли на объекте скрипт WorldItem
        if (other.GetComponent<WorldItem>() != null)
        {
            // 1. Сбрасываем физику (чтобы предмет перестал падать по инерции)
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 2. Телепортируем
            other.transform.position = targetPosition;

            // 3. Включаем сообщение
            showMessage = true;
            timer = messageDuration;
        }
    }

    void Update()
    {
        // Таймер исчезновения текста
        if (showMessage)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                showMessage = false;
            }
        }
    }

    void OnGUI()
    {
        if (showMessage)
        {
            // Получаем текст через ваш LanguageManager
            string text = LanguageManager.GetText(warningMessage);

            // Ваш прошлый текст был на 0.15f (15% высоты экрана).
            // Этот ставим на 0.25f (25%), чтобы он был ниже и не перекрывал.
            float yPosition = Screen.height * 0.25f;

            GUI.Label(new Rect(0, yPosition, Screen.width, 100), text, warningStyle);
        }
    }
}
