using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour {
    public LocalizedLine localizedLine;

    private Text uiText;

    void Awake() {
        uiText = GetComponent<Text>();
    }

    void OnEnable() {
        LanguageEvents.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    void OnDisable() {
        LanguageEvents.OnLanguageChanged -= UpdateText;
    }

    private void UpdateText() {
        if (localizedLine == null || uiText == null) return;
        
        uiText.text = LanguageManager.GetText(localizedLine);
    }
}