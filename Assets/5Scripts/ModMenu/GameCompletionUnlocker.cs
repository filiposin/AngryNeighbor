using UnityEngine;

public class GameCompletionUnlocker : MonoBehaviour
{
    void Start()
    {
        // Принудительно ставим, что игра была пройдена хотя бы раз
        PlayerPrefs.SetInt("GameCompleted", 1);

        int diffInt = PlayerPrefs.GetInt("Difficulty", 1); 
        if (GameSettingsManager.Instance != null)
        {
            diffInt = (int)GameSettingsManager.Instance.difficulty;
        }
        
        // Если сложность Nightmare, мы должны дать статус прохождения на Nightmare (1)
        if (diffInt == (int)Difficulty.Nightmare)
        {
            PlayerPrefs.SetInt("NightmareCompleted", 1);
        }

        PlayerPrefs.Save();
    }
}