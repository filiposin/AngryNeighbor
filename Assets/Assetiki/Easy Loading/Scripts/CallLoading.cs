using UnityEngine;

public class CallLoading : MonoBehaviour
{
    public void Call_ChangeSceneWithLoading(string sceneName)
    {
        if (EasyLoading.Instance != null)
        {
            EasyLoading.Instance.ChangeSceneWithLoading(sceneName);
        }
        else
        {
            EasyLoading loader = FindObjectOfType<EasyLoading>();
            if (loader != null)
            {
                loader.ChangeSceneWithLoading(sceneName);
            }
            else
            {
                Debug.LogError("ОШИБКА: На сцене нет объекта со скриптом EasyLoading! Проверь иерархию.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }
    }

    public void Call_ChangeScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}