using UnityEngine;

public class OpenLinkButton : MonoBehaviour
{
    // Теперь метод принимает строку (url) прямо из кнопки On Click
    public void OpenURL(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
            Debug.Log("Открываю ссылку: " + url);
        }
        else
        {
            Debug.LogWarning("Ссылка пустая!");
        }
    }
}