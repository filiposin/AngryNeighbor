using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    private bool IsOpen = false;
    [SerializeField] private GameObject settingsMenuUI;

    public void ToggleSettingsMenu()
    {
        IsOpen = !IsOpen;
        settingsMenuUI.SetActive(IsOpen);
    }
}