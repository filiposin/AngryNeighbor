using UnityEngine;
using System.Collections.Generic;

// Единичная фраза внутри реплики
[System.Serializable]
public class AdvancedDialoguePhrase {
    public LocalizedLine text;
    
    [Tooltip("Если пусто — берется из профиля персонажа")]
    public Sprite iconOverride; // Эмоция (если отличается от дефолтной)
    
    public AudioClip voiceClip;
}

// Блок реплики одного персонажа (он говорит несколько фраз подряд)
[System.Serializable]
public class DialogueReplica {
    public DialogueCharacter characterProfile; // Ссылка на профиль
    public List<AdvancedDialoguePhrase> phrases; // Что он говорит
}

[CreateAssetMenu(fileName = "NewAdvancedDialogue", menuName = "Dialogue/Advanced Dialogue")]
public class AdvancedDialogueAsset : ScriptableObject {
    [Tooltip("Список реплик. Каждая реплика привязана к персонажу.")]
    public List<DialogueReplica> replicas;

    [Space]
    public List<DialogueChoice> choices;
}