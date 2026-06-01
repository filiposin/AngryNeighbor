using UnityEngine;

public class GameCompleteLoader : MonoBehaviour
{
    [Header("Game Complete Panel")]
    [SerializeField] private GameObject gameCompletePanel;
    
    [Header("Game Complete Text")]
    [SerializeField] private GameObject gameCompleteTextOn;
    [SerializeField] private GameObject gameCompleteTextOff;
    [SerializeField] private GameObject gameCompleteTextYep;

    void Start()
    {
        // 1. Проверяем, пройдена ли вообще игра (значение по умолчанию 0, если ключа нет)
        if (PlayerPrefs.GetInt("GameCompleted", 0) == 1)
        {
            // Игра пройдена! Включаем главную панель
            gameCompletePanel.SetActive(true);

            // На всякий случай выключаем все тексты, чтобы не наслоились
            gameCompleteTextOn.SetActive(false);
            gameCompleteTextOff.SetActive(false);
            gameCompleteTextYep.SetActive(false);

            // 2. Смотрим, какой текст нам нужно показать
            int textState = PlayerPrefs.GetInt("CompletionTextState", 0);

            if (textState == 0)
            {
                gameCompleteTextOff.SetActive(true);
            }
            else if (textState == 1)
            {
                gameCompleteTextOn.SetActive(true);
            }
            else if (textState == 2)
            {
                gameCompleteTextYep.SetActive(true);
            }
        }
        else
        {
            // Игра ни разу не пройдена — панель должна быть выключена
            gameCompletePanel.SetActive(false);
        }
    }
}