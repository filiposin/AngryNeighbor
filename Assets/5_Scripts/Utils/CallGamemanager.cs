using UnityEngine;

public class CallGamemanager : MonoBehaviour
{
    public void CallSetSettings(bool isOn)
    {
        GameManager.Instance.SetSettings(isOn);
    }

    public void CallSetPaused(bool isPaused)
    {
        GameManager.Instance.SetPause(isPaused);
    }

    public void CallSettingsFromInventory()
    {
        GameManager.Instance.OpenSettingsFromInventory();
    }
}