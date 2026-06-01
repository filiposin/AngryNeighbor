using UnityEngine;

public class GameCompleteSaver : MonoBehaviour
{
    void Start()
    {
        // 1. Игрок вообще попал в катсцену — значит игра пройдена.
        PlayerPrefs.SetInt("GameCompleted", 1);

        // Получаем сложность: сначала из GameSettingsManager, если нет - из PlayerPrefs
        int diffInt = PlayerPrefs.GetInt("Difficulty", 1); // По умолчанию Normal (1)
        if (GameSettingsManager.Instance != null)
        {
            diffInt = (int)GameSettingsManager.Instance.difficulty;
        }
        Difficulty currentDiff = (Difficulty)diffInt;

        // 2. Проверяем сложность
        if (currentDiff == Difficulty.Nightmare)
        {
            // Проверяем, проходил ли он УЖЕ на Найтмере до этого
            if (PlayerPrefs.GetInt("NightmareCompleted", 0) == 1)
            {
                // Уже проходил -> нужно показать текст Yep (сохраняем код 2)
                PlayerPrefs.SetInt("CompletionTextState", 2);
            }
            else
            {
                // Это первый раз на Найтмере -> нужно показать текст On (сохраняем код 1)
                PlayerPrefs.SetInt("NightmareCompleted", 1); // Запоминаем, что теперь он прошел на найтмере
                PlayerPrefs.SetInt("CompletionTextState", 1);
            }
        }
        else
        {
            // Любая другая сложность, не Найтмер
            if (PlayerPrefs.GetInt("NightmareCompleted", 0) == 1)
            {
                // Но если он уже проходил Найтмер раньше, восстанавливаем код 2 (мод меню уже есть)
                PlayerPrefs.SetInt("CompletionTextState", 2);
            }
            else
            {
                PlayerPrefs.SetInt("CompletionTextState", 0);
            }
        }

        // Принудительно сохраняем данные
        PlayerPrefs.Save();
        Debug.Log("Данные о прохождении сохранены!");
    }
}