using UnityEngine;

public static class LanguageManager {
    private const string PREF_KEY = "game_language";
    public static Language CurrentLanguage { get; private set; } = Language.EN;

    static LanguageManager() {
        if (!PlayerPrefs.HasKey(PREF_KEY)) {
            SystemLanguage sysLang = Application.systemLanguage;
            Language defaultLang = Language.EN;
            
            if (sysLang == SystemLanguage.Russian) defaultLang = Language.RU;
            else if (sysLang == SystemLanguage.Spanish) defaultLang = Language.SP;
            else if (sysLang == SystemLanguage.Turkish) defaultLang = Language.TR;

            CurrentLanguage = defaultLang;
            PlayerPrefs.SetInt(PREF_KEY, (int)defaultLang);
            PlayerPrefs.Save();
        } else {
            int langIndex = PlayerPrefs.GetInt(PREF_KEY, 0);
            CurrentLanguage = (Language)langIndex;
        }
    }

    public static void SetLanguage(Language lang) {
        CurrentLanguage = lang;
        PlayerPrefs.SetInt(PREF_KEY, (int)lang);
        PlayerPrefs.Save();
        LanguageEvents.RaiseLanguageChanged();
    }
    
    public static string GetText(LocalizedLine line) {
        switch (CurrentLanguage) {
            case Language.RU: return line.russianText;
            case Language.SP: return line.spanishText;
            case Language.TR: return line.turkishText;
            default: return line.englishText;
        }
    }
}