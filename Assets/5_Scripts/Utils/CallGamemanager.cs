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

    // Повесь этот метод на OnClick() кнопки "Настройки" ВНУТРИ ИНВЕНТАРЯ!
    // Тебе больше не нужно передавать галочки true/false, просто вызови этот метод.
    public void CallSettingsFromInventory()
    {
        GameManager.Instance.OpenSettingsFromInventory();
    }
}