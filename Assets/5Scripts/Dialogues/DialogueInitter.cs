using UnityEngine;

public class DialogueInitter : MonoBehaviour
{
    [SerializeField] private Transform lookTo;
    [SerializeField] private AdvancedDialogueAsset advDialogue;
    [SerializeField] private DialogueAsset dialogue;
    [SerializeField] private bool isOneTime = true;
    private DialogueManager dm;
    private FP_Controller contr;
    private FP_CameraLook cam;

    private void Start()
    {
        dm = FindFirstObjectByType<DialogueManager>();
        if (dm == null) Debug.Log("kys dm null");
        FindPlayer();
    }
    public void InitDialogue()
    {
        if (cam == null) FindPlayer(); if (contr == null) FindPlayer();
        contr.canControl = false;
        cam.LookTo(lookTo);
        if (advDialogue != null) dm.StartDialogue(advDialogue);
        else if (dialogue != null) dm.StartDialogue(dialogue);
        if (isOneTime) { Destroy(this); Collider col = GetComponent<Collider>(); if (col != null) col.enabled = false; }
    }
    public void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.TryGetComponent<FP_Controller>(out var controller);
        contr = controller;
        player.TryGetComponent<FP_CameraLook>(out var cameral);
        cam = cameral;
    }
}
