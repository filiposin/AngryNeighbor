using UnityEngine;

public class Toggler : MonoBehaviour
{
    public bool isToggled = false;
    public GameObject targetObject;
    public void Toggle()
    {
        isToggled = !isToggled;
        if (targetObject != null)
        {
            targetObject.SetActive(isToggled);
        }
    }
    public void SetState(bool state)
    {
        isToggled = state;
        if (targetObject != null)
        {
            targetObject.SetActive(isToggled);
        }
    }
}
