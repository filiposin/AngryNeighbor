using System.Collections.Generic;
using UnityEngine;


public class DialogueStarter : MonoBehaviour {
    public AdvancedDialogueAsset dialogues;
    [SerializeField] private Transform headTransform;
    void Start()
    {
        StartRandomDialogue();
    }

    public void StartRandomDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(dialogues);
    }
}
