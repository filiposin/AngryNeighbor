using UnityEngine;

public interface IItem
{
    void Initialize(ItemDefinition def);
    void OnPickup(GameObject holder);
    void OnDrop();
    void OnUse(); // primary use
    void OnThrow(UnityEngine.Vector3 velocity);
}

public interface IInteractable
{
    // caller is the GameObject that interacted (player)
    void Interact(GameObject caller);
    string GetInteractText(); // optional: "Open", "Pick up"
}

public interface IDoorController
{
    bool IsOpen { get; }
    bool IsLocked { get; }
    Transform DoorTransform { get; }
    void Open();
    void Close();
    void PlayBlockedFeedback();
}

public interface IHittable
{
    void TryReplace(string id);
    void Replace();
}