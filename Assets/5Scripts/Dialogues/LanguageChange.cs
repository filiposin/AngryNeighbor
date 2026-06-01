using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanguageChange : MonoBehaviour
{
    public void SetEnglish() => LanguageManager.SetLanguage(Language.EN);
    public void SetRussian() => LanguageManager.SetLanguage(Language.RU);
    public void SetSpanish() => LanguageManager.SetLanguage(Language.SP);
    public void SetTurkish() => LanguageManager.SetLanguage(Language.TR);
}
