using UnityEngine;

// старый скрипт для выхода, можно улучшить а не делать как я раньше

public class exitb : MonoBehaviour
{
    public void quit()
    {
#if UNITY_EDITOR
        Debug.Log("вышел");
#else
            Application.Quit();
#endif
    }
}
