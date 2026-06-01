using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class LocalizedLine {
    [TextArea(10,10)] public string englishText;
    [TextArea(10,10)] public string russianText;
    [TextArea(10,10)] public string spanishText;
    [TextArea(10,10)] public string turkishText;
}

// --- НОВОЕ: Класс для одной кнопки выбора ---
[System.Serializable]
public class DialogueChoice {
    public LocalizedLine buttonText; 
    
    [Header("Куда ведет кнопка")]
    public AdvancedDialogueAsset nextAdvancedDialogue; // <--- НОВОЕ ПОЛЕ
    public DialogueAsset nextDialogue; // Старое поле (оставляем для совместимости)
}

[System.Serializable]
public class DialogueLine {
    public string characterName;
    public Sprite characterIcon;
    public LocalizedLine text;
    public AudioClip voiceClip; 
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Localized")]
public class DialogueAsset : ScriptableObject {
    public List<DialogueLine> lines;

    // --- НОВОЕ: Список выборов в конце этого блока диалога ---
    // Если список пуст — диалог просто закроется.
    public List<DialogueChoice> choices;
}

// Enum и LanguageEvents без изменений
public enum Language { EN, RU, SP, TR }

public static class LanguageEvents {
    public static Action OnLanguageChanged;
    public static void RaiseLanguageChanged() {
        OnLanguageChanged?.Invoke();
    }
}