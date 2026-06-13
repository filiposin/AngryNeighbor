using UnityEngine;

public class OpenLinkButton : MonoBehaviour
{
    public void OpenURL(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning("Олух, ссылку назначь");
        }
    }
}