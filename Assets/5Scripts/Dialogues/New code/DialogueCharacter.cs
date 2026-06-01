using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Dialogue/Character Profile")]
public class DialogueCharacter : ScriptableObject {
    public string characterName; // Имя, которое будет отображаться
    public Sprite defaultIcon;   // Стандартная иконка
    public AudioClip defaultVoiceBeep; // (Опционально) звук голоса персонажа
}