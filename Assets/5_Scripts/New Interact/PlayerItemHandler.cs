using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItemHandler : MonoBehaviour
{
    [Header("General Settings")]
    public Camera playerCamera;
    public Transform handTransform;
    public LayerMask interactableLayers;
    public LayerMask hittableLayers;
    public float interactDistance = 2.5f;

    [Header("Released Item Physics")]
    public float releasedItemMaxDepenetrationSpeed = 10f;

    [Header("UI References")]
    public Text interactionInfoText;

    [Header("Inputs")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode throwKey = KeyCode.Mouse1;
    public KeyCode useKey = KeyCode.Mouse0;
    public KeyCode inventoryKey = KeyCode.Tab;

    [Header("References")]
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public PlayerAnimationController animationController;

    [Header("Optional Inventory UI")]
    public bool pausePlayerControlWhenInventoryOpen = true;

    public static PlayerItemHandler inst = null;

    private GameObject heldObject;
    private ItemBase heldItemBase;
    public ItemBase HeldItem => heldItemBase;
    private ItemDefinition heldDefinition;
    private int heldSourceSlot = -1;

    private GameObject[] slotModels;
    private GameObject[] backpackModels;
    private static HashSet<ItemDefinition> warmedDefinitions = new HashSet<ItemDefinition>();
    private RaycastHit[] raycastHits = new RaycastHit[4];
    private Transform camTransform;
    private PoolManager pool => PoolManager.Instance;
    private int interactMask;
    private float nextUseTime = 0f;
    private bool isDroppingItem = false;
    private FP_Controller fpController;
    private bool storedControlState;
    private bool inventoryControlApplied;

    void Reset() { playerCamera = Camera.main; }

    void Start()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        camTransform = playerCamera.transform;
        interactMask = interactableLayers.value | hittableLayers.value;

        if (inst == null) inst = this;

        if (handTransform == null) Debug.LogWarning("Hand transform missing!");

        fpController = GetComponent<FP_Controller>();

        if (animationController != null && animationController.fpController == null)
            animationController.fpController = fpController;

        if (inventory != null)
        {
            slotModels = new GameObject[inventory.maxSlots];
            backpackModels = new GameObject[inventory.BackpackCapacity];
            if (inventoryUI != null) inventoryUI.Bind(inventory, this);
        }

        if (interactionInfoText != null)
            interactionInfoText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsSettingsOpen || GameManager.Instance.IsPauseOpen))
            return;

        if (Input.GetKeyDown(inventoryKey))
            ToggleInventoryUI();

        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            if (interactionInfoText != null && interactionInfoText.gameObject.activeSelf)
                interactionInfoText.gameObject.SetActive(false);

            return;
        }

        UpdateInteractUI();

        if (Input.GetKeyDown(interactKey)) TryInteractOrPickup();

        if (inventory != null)
        {
            int maxKeyboardSlots = Mathf.Min(inventory.maxSlots, 9);
            for (int i = 0; i < maxKeyboardSlots; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                    ChangeSlot(i + 1);
            }
        }

        if (heldObject != null && !isDroppingItem)
        {
            if (Input.GetKeyDown(useKey)) AttemptUseHeldItem();
            if (Input.GetKeyDown(throwKey)) RequestThrowHeldItem();
        }
    }

    private void UpdateInteractUI()
    {
        if (interactionInfoText == null) return;

        RaycastHit hit;
        bool hasHit = DoRaycast(out hit);

        if (hasHit)
        {
            if (IsInLayerMask(hit.collider.gameObject, hittableLayers))
            {
                interactionInfoText.gameObject.SetActive(false);
                return;
            }

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactionInfoText.gameObject.SetActive(true);
                interactionInfoText.text = interactable.GetInteractText();
                return;
            }
        }

        if (interactionInfoText.gameObject.activeSelf)
            interactionInfoText.gameObject.SetActive(false);
    }

    private bool DoRaycast(out RaycastHit bestHit)
    {
        bestHit = new RaycastHit();
        Vector3 origin = camTransform.position;
        Vector3 dir = camTransform.forward;

        int hits = Physics.RaycastNonAlloc(origin, dir, raycastHits, interactDistance, interactMask, QueryTriggerInteraction.Collide);
        if (hits <= 0) return false;

        bestHit = raycastHits[0];
        float minD = bestHit.distance;
        for (int i = 1; i < hits; i++)
        {
            if (raycastHits[i].distance < minD)
            {
                minD = raycastHits[i].distance;
                bestHit = raycastHits[i];
            }
        }
        return true;
    }

    private void TryInteractOrPickup()
    {
        if (isDroppingItem) return;

        RaycastHit best;
        bool hasHit = DoRaycast(out best);

        if (hasHit && IsInLayerMask(best.collider.gameObject, hittableLayers))
        {
            if (heldItemBase != null)
            {
                AttemptUseHeldItem();
                return;
            }
        }

        if (hasHit)
        {
            if (heldItemBase != null && heldItemBase is ItemKey)
            {
                if (best.collider.GetComponentInParent<KeyDoor>() != null)
                {
                    AttemptUseHeldItem();
                    return;
                }
            }

            var inter = best.collider.GetComponentInParent<IInteractable>();
            if (inter != null)
            {
                inter.Interact(gameObject);
                return;
            }

            var item = best.collider.GetComponentInParent<ItemBase>();
            if (item != null)
            {
                PickupExistingWorldItem(item.gameObject);
                return;
            }
        }

        if (heldItemBase != null)
            AttemptUseHeldItem();
    }

    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }

    public void ChangeSlot(int slot)
    {
        if (isDroppingItem || inventory == null || slot < 1 || slot > inventory.maxSlots) return;

        int index = slot - 1;
        if (heldSourceSlot == index && heldObject != null)
        {
            UnequipCurrent();
            return;
        }

        EquipSlot(index);
    }

    public void EquipSlot(int slotIndex)
    {
        if (isDroppingItem || inventory == null) return;
        EnsureModelArrays();

        var def = inventory.GetAt(slotIndex);
        if (heldObject != null)
            ClearHeldReferences(true);

        if (def == null) return;

        if (slotIndex >= 0 && slotIndex < slotModels.Length && slotModels[slotIndex] != null)
        {
            GameObject cachedObj = slotModels[slotIndex];
            cachedObj.SetActive(true);
            var cachedBase = cachedObj.GetComponent<ItemBase>();
            heldObject = cachedObj;
            heldItemBase = cachedBase;
            heldDefinition = def;
            heldSourceSlot = slotIndex;
            nextUseTime = Time.time;
            animationController?.SetHeldItem(def);
            if (cachedBase != null) cachedBase.SetHandLayer();
            return;
        }

        SpawnAndAttachFromDefinition(def, slotIndex);
    }

    public void UnequipCurrent()
    {
        if (isDroppingItem) return;
        ClearHeldReferences(true);
    }

    private void PickupExistingWorldItem(GameObject obj)
    {
        if (obj == null) return;

        var itemBaseLocal = obj.GetComponent<ItemBase>();
        if (itemBaseLocal == null) return;

        ItemDefinition def = itemBaseLocal.Definition;
        if (def == null)
        {
            var world = obj.GetComponent<WorldItem>();
            if (world != null && world.itemDefinition != null)
            {
                def = world.itemDefinition;
                itemBaseLocal.Initialize(def);
            }
        }

        if (def == null) return;

        if (def.uniqueInInventory && inventory != null && inventory.Contains(def))
        {
            // У игрока уже есть такой уникальный предмет, подбираем "в никуда" (удаляем со сцены)
            itemBaseLocal.OnPickup(gameObject); // Проигрываем звук подбора

            if (def.itemPrefab != null && pool != null)
                pool.ReturnToPool(def.itemPrefab, obj);
            else
                Destroy(obj);

            return;
        }

        if (inventory != null)
        {
            EnsureModelArrays();

            switch (inventory.pickupMode)
            {
                case Inventory.PickupMode.Legacy:
                    TryPickupToBackpack(def, obj, out _);
                    break;

                case Inventory.PickupMode.Updated:
                    if (TryPickupToBackpack(def, obj, out int backpackIndex))
                    {
                        int emptyQuickSlot = FindFirstEmptySlot();
                        if (emptyQuickSlot != -1)
                        {
                            AssignQuickSlotFromBackpack(backpackIndex, emptyQuickSlot);
                            if (heldObject == null)
                                EquipSlot(emptyQuickSlot);
                        }
                    }
                    break;

                case Inventory.PickupMode.NineSlots:
                    TryPickupToBackpack(def, obj, out _);
                    break;

                case Inventory.PickupMode.NineSlotsUpdated:
                    if (!TryPickupToEquipmentSlot(def, obj, true, false))
                    {
                        TryPickupToBackpack(def, obj, out _);
                    }
                    break;

                default:
                    TryPickupToEquipmentSlot(def, obj, true, true);
                    break;
            }

            return;
        }

        if (heldObject == null)
        {
            AttachObjectToHandAnchor(obj);
            SetupHeldReferences(obj, itemBaseLocal, def, -1);
        }
    }

    private bool TryPickupToEquipmentSlot(ItemDefinition def, GameObject obj, bool autoEquip, bool allowSwapWhenFull)
    {
        int emptySlot = FindFirstEmptySlot();
        if (emptySlot != -1)
        {
            if (!inventory.SetAt(emptySlot, def)) return false;

            AttachObjectToHandAnchor(obj);
            slotModels[emptySlot] = obj;

            if (autoEquip && heldObject == null) EquipSlot(emptySlot);
            else obj.SetActive(false);

            return true;
        }

        if (!allowSwapWhenFull || heldObject == null || heldSourceSlot == -1)
            return false;

        ThrowHeldItem();
        int newEmptySlot = FindFirstEmptySlot();
        if (newEmptySlot == -1 || !inventory.SetAt(newEmptySlot, def))
            return false;

        AttachObjectToHandAnchor(obj);
        slotModels[newEmptySlot] = obj;
        EquipSlot(newEmptySlot);
        return true;
    }

    private bool TryPickupToBackpack(ItemDefinition def, GameObject obj, out int backpackIndex)
    {
        backpackIndex = -1;

        if (inventory == null || !inventory.BackpackEnabled) return false;

        int emptyBackpackSlot = inventory.FindFirstEmptyBackpackSlot();
        if (emptyBackpackSlot == -1) return false;
        if (!inventory.SetBackpackAt(emptyBackpackSlot, def)) return false;

        EnsureModelArrays();
        AttachObjectToHandAnchor(obj);
        backpackModels[emptyBackpackSlot] = obj;
        obj.SetActive(false);
        backpackIndex = emptyBackpackSlot;
        return true;
    }

    private bool AssignQuickSlotFromBackpack(int backpackIndex, int quickSlotIndex)
    {
        if (inventory == null || !inventory.BackpackEnabled) return false;
        if (backpackIndex < 0 || backpackIndex >= inventory.BackpackCapacity) return false;
        if (quickSlotIndex < 0 || quickSlotIndex >= inventory.maxSlots) return false;

        ItemDefinition def = inventory.GetBackpackAt(backpackIndex);
        if (def == null) return false;

        EnsureModelArrays();
        inventory.SetAt(quickSlotIndex, def);
        slotModels[quickSlotIndex] = backpackModels[backpackIndex];

        if (slotModels[quickSlotIndex] != null && slotModels[quickSlotIndex] != heldObject)
            slotModels[quickSlotIndex].SetActive(false);

        return true;
    }

    private void SpawnAndAttachFromDefinition(ItemDefinition def, int slotIndex)
    {
        if (def == null) return;

        if (!warmedDefinitions.Contains(def) && def.itemPrefab != null && pool != null)
        {
            pool.WarmPool(def.itemPrefab, Mathf.Max(1, def.poolSize));
            warmedDefinitions.Add(def);
        }

        Vector3 spawnPos = camTransform.position;
        GameObject go;
        if (def.itemPrefab != null && pool != null)
            go = pool.GetFromPool(def.itemPrefab, spawnPos, Quaternion.identity);
        else if (def.itemPrefab != null)
            go = Instantiate(def.itemPrefab, spawnPos, Quaternion.identity);
        else
            go = new GameObject("Temp_" + def.displayName);

        var itemBase = go.GetComponent<ItemBase>();
        if (itemBase != null) itemBase.Initialize(def);

        AttachObjectToHandAnchor(go);
        if (slotIndex >= 0 && slotIndex < slotModels.Length)
            slotModels[slotIndex] = go;

        SetupHeldReferences(go, itemBase, def, slotIndex);
    }

    private void AttachObjectToHandAnchor(GameObject go)
    {
        go.transform.SetParent(handTransform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var ib = go.GetComponent<ItemBase>();
        if (ib != null) go.transform.localScale = ib.InitialScale;

        if (ib != null && ib.Definition != null)
        {
            go.transform.localPosition = ib.Definition.holdPosition;
            go.transform.localRotation = Quaternion.Euler(ib.Definition.holdEulerRotation);
        }

        var rb = go.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (ib != null)
        {
            ib.OnPickup(gameObject);
            ib.SetHandLayer();
        }
    }

    private void SetupHeldReferences(GameObject go, ItemBase ib, ItemDefinition def, int slot)
    {
        heldObject = go;
        heldItemBase = ib;
        heldDefinition = def;
        heldSourceSlot = slot;
        nextUseTime = Time.time;
        animationController?.SetHeldItem(def);
    }

    private void RequestThrowHeldItem()
    {
        if (heldObject == null || isDroppingItem) return;

        if (animationController == null)
        {
            ThrowHeldItem();
            return;
        }

        isDroppingItem = true;
        animationController.PlayDropAnimation(() =>
        {
            ThrowHeldItem();
            isDroppingItem = false;
        });
    }

    private void ThrowHeldItem()
    {
        if (heldObject == null) return;

        float spawnDist = heldDefinition != null ? heldDefinition.spawnDistanceFromCamera : 0.6f;
        float force = heldDefinition != null ? heldDefinition.throwForce : 8f;
        Vector3 throwVelocity = camTransform.forward * force;

        bool consume = heldDefinition == null || heldDefinition.consumeOnThrow;

        if (consume)
        {
            if (inventory != null && inventory.BackpackEnabled && (inventory.pickupMode == Inventory.PickupMode.Legacy || inventory.pickupMode == Inventory.PickupMode.Updated))
            {
                RemoveHeldItemFromLinkedInventory();
            }
            else if (heldSourceSlot >= 0 && inventory != null)
            {
                if (heldSourceSlot < slotModels.Length) slotModels[heldSourceSlot] = null;
                inventory.RemoveAt(heldSourceSlot);
            }

            ReleaseHeldObject(camTransform.position + camTransform.forward * spawnDist, camTransform.rotation, throwVelocity);

            heldObject = null;
            heldItemBase = null;
            heldDefinition = null;
            heldSourceSlot = -1;
            animationController?.SetHeldItem(null);
        }
        else
        {
            // Бесконечное кидание (предмет остается в руке)
            Vector3 spawnPos = camTransform.position + camTransform.forward * spawnDist;
            Quaternion spawnRot = camTransform.rotation;
            
            // Спец логика для зеленой коробки (спавним в игроке)
            bool isGreenBox = heldObject.GetComponent<ItemGreenBox>() != null;
            if (isGreenBox)
            {
                spawnPos = transform.position;
                spawnRot = transform.rotation;
                throwVelocity = Vector3.zero;
            }

            GameObject copy;
            if (heldDefinition != null && heldDefinition.itemPrefab != null && pool != null)
            {
                copy = pool.GetFromPool(heldDefinition.itemPrefab, spawnPos, spawnRot);
            }
            else
            {
                copy = Instantiate(heldObject, spawnPos, spawnRot);
            }

            copy.transform.SetParent(null);
            copy.transform.localScale = heldItemBase != null ? heldItemBase.InitialScale : copy.transform.localScale;
            copy.SetActive(true);

            ItemBase copyBase = copy.GetComponent<ItemBase>();
            if (copyBase != null)
            {
                copyBase.Initialize(heldDefinition);
                copyBase.RestoreLayer();
                copyBase.OnThrow(throwVelocity);
                
                if (isGreenBox)
                {
                    ((ItemGreenBox)copyBase).OnPlaced(gameObject);
                }
            }

            Rigidbody rb = copy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (isGreenBox)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                else
                {
                    ConfigureReleasedRigidbody(rb, throwVelocity);
                }
            }
        }
        
        isDroppingItem = false;
    }

    private void ReleaseHeldObject(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        heldObject.transform.SetParent(null);
        if (heldItemBase != null) heldObject.transform.localScale = heldItemBase.InitialScale;
        heldObject.transform.position = position;
        heldObject.transform.rotation = rotation;

        if (heldItemBase != null)
        {
            heldItemBase.RestoreLayer();
            heldItemBase.OnThrow(velocity);
        }

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        ConfigureReleasedRigidbody(rb, velocity);
    }

    private void ConfigureReleasedRigidbody(Rigidbody rb, Vector3 velocity)
    {
        if (rb == null) return;

        rb.isKinematic = false;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxDepenetrationVelocity = releasedItemMaxDepenetrationSpeed;
        rb.velocity = velocity;
    }

    public bool TryMoveInventoryItem(InventoryUI.SlotArea fromArea, int fromIndex, InventoryUI.SlotArea toArea, int toIndex)
    {
        if (inventory == null || isDroppingItem) return false;
        EnsureModelArrays();

        if (!IsValidAreaIndex(fromArea, fromIndex) || !IsValidAreaIndex(toArea, toIndex))
            return false;

        ItemDefinition fromDef = GetDefinition(fromArea, fromIndex);
        if (fromDef == null) return false;

        if (inventory.BackpackEnabled)
        {
            if (inventory.pickupMode == Inventory.PickupMode.Legacy || inventory.pickupMode == Inventory.PickupMode.Updated)
            {
                bool movedLinkedItem = TryMoveLinkedInventoryItem(fromArea, fromIndex, toArea, toIndex);
                inventoryUI?.Refresh();
                return movedLinkedItem;
            }
        }

        ItemDefinition toDef = GetDefinition(toArea, toIndex);
        GameObject fromModel = GetModel(fromArea, fromIndex);
        GameObject toModel = GetModel(toArea, toIndex);

        int reequipSlot = -1;
        if (heldObject != null)
        {
            if (fromArea == InventoryUI.SlotArea.Equipment && heldSourceSlot == fromIndex)
                reequipSlot = fromIndex;
            else if (toArea == InventoryUI.SlotArea.Equipment && heldSourceSlot == toIndex)
                reequipSlot = toIndex;

            if (reequipSlot != -1)
                UnequipCurrent();
        }

        SetDefinition(fromArea, fromIndex, toDef);
        SetDefinition(toArea, toIndex, fromDef);
        SetModel(fromArea, fromIndex, toModel);
        SetModel(toArea, toIndex, fromModel);

        HideStoredModel(GetModel(fromArea, fromIndex));
        HideStoredModel(GetModel(toArea, toIndex));

        if (reequipSlot != -1 && inventory.GetAt(reequipSlot) != null)
            EquipSlot(reequipSlot);

        inventoryUI?.Refresh();
        return true;
    }

    private bool TryMoveLinkedInventoryItem(InventoryUI.SlotArea fromArea, int fromIndex, InventoryUI.SlotArea toArea, int toIndex)
    {
        if (fromArea == InventoryUI.SlotArea.Backpack && toArea == InventoryUI.SlotArea.Equipment)
            return AssignBackpackItemToQuickSlot(fromIndex, toIndex);

        if (fromArea == InventoryUI.SlotArea.Backpack && toArea == InventoryUI.SlotArea.Backpack)
            return SwapBackpackSlots(fromIndex, toIndex);

        if (fromArea == InventoryUI.SlotArea.Equipment && toArea == InventoryUI.SlotArea.Equipment)
            return SwapQuickSlots(fromIndex, toIndex);

        if (fromArea == InventoryUI.SlotArea.Equipment && toArea == InventoryUI.SlotArea.Backpack)
            return ClearQuickSlot(fromIndex);

        return false;
    }

    private bool AssignBackpackItemToQuickSlot(int backpackIndex, int quickSlotIndex)
    {
        GameObject assignedModel = backpackIndex >= 0 && backpackIndex < backpackModels.Length ? backpackModels[backpackIndex] : null;
        ItemDefinition assignedDefinition = inventory.GetBackpackAt(backpackIndex);
        int previousQuickSlot = FindQuickSlotReferencing(assignedModel, assignedDefinition);
        bool wasHeld = heldSourceSlot == quickSlotIndex || heldSourceSlot == previousQuickSlot;

        if (wasHeld)
            UnequipCurrent();

        if (previousQuickSlot != -1 && previousQuickSlot != quickSlotIndex)
            ClearQuickSlot(previousQuickSlot);

        bool assigned = AssignQuickSlotFromBackpack(backpackIndex, quickSlotIndex);

        if (assigned && wasHeld)
            EquipSlot(quickSlotIndex);

        return assigned;
    }

    private int FindQuickSlotReferencing(GameObject model, ItemDefinition fallbackDefinition)
    {
        if (inventory == null || slotModels == null) return -1;

        for (int i = 0; i < inventory.maxSlots; i++)
        {
            bool sameModel = model != null && i < slotModels.Length && slotModels[i] == model;
            bool sameDefinitionWithoutModel = model == null && fallbackDefinition != null && inventory.GetAt(i) == fallbackDefinition;

            if (sameModel || sameDefinitionWithoutModel)
                return i;
        }

        return -1;
    }

    private bool SwapBackpackSlots(int firstIndex, int secondIndex)
    {
        if (firstIndex == secondIndex) return false;

        ItemDefinition firstDef = inventory.GetBackpackAt(firstIndex);
        ItemDefinition secondDef = inventory.GetBackpackAt(secondIndex);
        GameObject firstModel = backpackModels[firstIndex];
        GameObject secondModel = backpackModels[secondIndex];

        inventory.SetBackpackAt(firstIndex, secondDef);
        inventory.SetBackpackAt(secondIndex, firstDef);
        backpackModels[firstIndex] = secondModel;
        backpackModels[secondIndex] = firstModel;
        return true;
    }

    private bool SwapQuickSlots(int firstIndex, int secondIndex)
    {
        if (firstIndex == secondIndex) return false;

        int reequipSlot = -1;
        if (heldSourceSlot == firstIndex || heldSourceSlot == secondIndex)
        {
            reequipSlot = heldSourceSlot == firstIndex ? secondIndex : firstIndex;
            UnequipCurrent();
        }

        ItemDefinition firstDef = inventory.GetAt(firstIndex);
        ItemDefinition secondDef = inventory.GetAt(secondIndex);
        GameObject firstModel = slotModels[firstIndex];
        GameObject secondModel = slotModels[secondIndex];

        inventory.SetAt(firstIndex, secondDef);
        inventory.SetAt(secondIndex, firstDef);
        slotModels[firstIndex] = secondModel;
        slotModels[secondIndex] = firstModel;

        if (reequipSlot != -1 && inventory.GetAt(reequipSlot) != null)
            EquipSlot(reequipSlot);

        return true;
    }

    private bool ClearQuickSlot(int quickSlotIndex)
    {
        if (quickSlotIndex < 0 || quickSlotIndex >= inventory.maxSlots) return false;

        if (heldSourceSlot == quickSlotIndex)
            UnequipCurrent();

        inventory.ClearAt(quickSlotIndex);
        if (quickSlotIndex < slotModels.Length)
            slotModels[quickSlotIndex] = null;

        return true;
    }

    public void OnItemPlacedSuccess()
    {
        if (inventory != null && inventory.BackpackEnabled && (inventory.pickupMode == Inventory.PickupMode.Legacy || inventory.pickupMode == Inventory.PickupMode.Updated))
        {
            RemoveHeldItemFromLinkedInventory();
        }
        else if (heldSourceSlot >= 0 && inventory != null)
        {
            if (heldSourceSlot < slotModels.Length) slotModels[heldSourceSlot] = null;
            inventory.RemoveAt(heldSourceSlot);
        }

        if (heldItemBase != null) heldItemBase.RestoreLayer();
        ClearHeldReferences(false);
        isDroppingItem = false;
    }

    public void ConsumeHeldItem()
    {
        if (heldObject == null) return;
        if (heldItemBase != null) heldItemBase.RestoreLayer();

        int slotToClear = heldSourceSlot;
        bool removeFromLinkedInventory = inventory != null && inventory.BackpackEnabled && (inventory.pickupMode == Inventory.PickupMode.Legacy || inventory.pickupMode == Inventory.PickupMode.Updated);

        if (removeFromLinkedInventory)
            RemoveHeldItemFromLinkedInventory();

        if (heldDefinition != null && heldDefinition.itemPrefab != null && pool != null)
            pool.ReturnToPool(heldDefinition.itemPrefab, heldObject);
        else
            Destroy(heldObject);

        if (!removeFromLinkedInventory && slotToClear >= 0)
        {
            if (slotToClear < slotModels.Length) slotModels[slotToClear] = null;
            if (inventory != null) inventory.RemoveAt(slotToClear);
        }

        heldObject = null;
        heldItemBase = null;
        heldDefinition = null;
        heldSourceSlot = -1;
        isDroppingItem = false;
        animationController?.SetHeldItem(null);
    }

    public void AttemptUseHeldItem()
    {
        if (heldItemBase == null || isDroppingItem) return;
        if (Time.time < nextUseTime) return;

        heldItemBase.OnUse();
        float cooldown = heldDefinition != null ? heldDefinition.useCooldown : 0.2f;
        nextUseTime = Time.time + cooldown;
    }

    public void OnUseButton() => AttemptUseHeldItem();
    public void OnThrowButton() => RequestThrowHeldItem();
    public void OnInteractButton() => TryInteractOrPickup();
    public void OnInventoryButton() => ToggleInventoryUI();

    public Inventory GetInventory()           => inventory;
    public GameObject[] GetSlotModels()       => slotModels;
    public GameObject[] GetBackpackModels()   => backpackModels;

    public void ClearAllInventory()
    {
        if (inventory == null) return;
        EnsureModelArrays();

        if (!inventory.BackpackEnabled)
        {
            for (int i = 0; i < inventory.maxSlots; i++)
            {
                slotModels[i] = null;
                inventory.ClearAt(i);
            }
        }
        else
        {
            for (int i = 0; i < inventory.BackpackCapacity; i++)
            {
                backpackModels[i] = null;
                inventory.ClearBackpackAt(i);
            }
            for (int i = 0; i < inventory.maxSlots; i++)
            {
                slotModels[i] = null;
                inventory.ClearAt(i);
            }
        }

        heldObject      = null;
        heldItemBase    = null;
        heldDefinition  = null;
        heldSourceSlot  = -1;
        isDroppingItem  = false;
        animationController?.SetHeldItem(null);
    }

    public void ToggleInventoryUI()
    {
        if (inventoryUI == null) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsSettingsOpen || GameManager.Instance.IsPauseOpen))
            return;
        SetInventoryOpen(!inventoryUI.IsOpen);
    }

    public void SetInventoryOpen(bool open)
    {
        if (inventoryUI == null) return;

        inventoryUI.SetOpen(open);

        if (!pausePlayerControlWhenInventoryOpen || fpController == null)
        {
            if (fpController != null) fpController.ForceCursorUnlock(open);
            return;
        }

        if (open)
        {
            if (!inventoryControlApplied)
            {
                storedControlState = fpController.canControl;
                inventoryControlApplied = true;
            }

            fpController.canControl = false;
        }
        else if (inventoryControlApplied)
        {
            fpController.canControl = storedControlState;
            inventoryControlApplied = false;
        }

        fpController.ForceCursorUnlock(open);
    }

    public void SetExternalUIOpen(bool open)
    {
        if (fpController == null)
            return;

        if (open)
        {
            if (!inventoryControlApplied)
            {
                storedControlState = fpController.canControl;
                inventoryControlApplied = true;
            }

            fpController.canControl = false;
        }
        else if (inventoryControlApplied)
        {
            fpController.canControl = storedControlState;
            inventoryControlApplied = false;
        }

        fpController.ForceCursorUnlock(open);
    }

    private int FindFirstEmptySlot()
    {
        return inventory == null ? -1 : inventory.FindFirstEmptySlot();
    }

    public void RefreshHand()
    {
        if (heldSourceSlot != -1)
            EquipSlot(heldSourceSlot);
    }

    public void DropAllItems()
    {
        if (inventory == null) return;
        EnsureModelArrays();

        if (!inventory.BackpackEnabled)
        {
            for (int i = 0; i < inventory.maxSlots; i++)
            {
                DropStoredItem(slotModels, inventory.GetAt(i), i);
                inventory.ClearAt(i);
            }

        }
        else
        {
            for (int i = 0; i < inventory.BackpackCapacity; i++)
            {
                DropStoredItem(backpackModels, inventory.GetBackpackAt(i), i);
                inventory.ClearBackpackAt(i);
            }

            for (int i = 0; i < inventory.maxSlots; i++)
            {
                if (inventory.pickupMode != Inventory.PickupMode.Legacy && inventory.pickupMode != Inventory.PickupMode.Updated)
                {
                    DropStoredItem(slotModels, inventory.GetAt(i), i);
                }
                inventory.ClearAt(i);
                if (i < slotModels.Length)
                    slotModels[i] = null;
            }
        }

        heldObject = null;
        heldItemBase = null;
        heldDefinition = null;
        heldSourceSlot = -1;
        isDroppingItem = false;

        if (animationController != null) animationController.SetHeldItem(null);
    }

    private void RemoveHeldItemFromLinkedInventory()
    {
        if (inventory == null || !inventory.BackpackEnabled) return;

        GameObject removedModel = heldObject;
        int backpackIndex = FindBackpackIndexForModel(removedModel);

        if (backpackIndex == -1)
            backpackIndex = FindBackpackIndexForDefinition(heldDefinition);

        ClearQuickSlotsReferencing(removedModel, heldDefinition);

        if (backpackIndex != -1)
        {
            inventory.RemoveBackpackAtAndShift(backpackIndex);
            ShiftBackpackModelsLeft(backpackIndex);
        }
    }

    private int FindBackpackIndexForModel(GameObject model)
    {
        if (model == null || backpackModels == null) return -1;

        for (int i = 0; i < backpackModels.Length; i++)
        {
            if (backpackModels[i] == model)
                return i;
        }

        return -1;
    }

    private int FindBackpackIndexForDefinition(ItemDefinition def)
    {
        if (def == null || inventory == null) return -1;

        for (int i = 0; i < inventory.BackpackCapacity; i++)
        {
            if (inventory.GetBackpackAt(i) == def)
                return i;
        }

        return -1;
    }

    private void ClearQuickSlotsReferencing(GameObject model, ItemDefinition fallbackDefinition)
    {
        if (inventory == null || slotModels == null) return;

        for (int i = 0; i < inventory.maxSlots; i++)
        {
            bool sameModel = model != null && i < slotModels.Length && slotModels[i] == model;
            bool sameHeldSlot = i == heldSourceSlot;
            bool sameDefinitionWithoutModel = model == null && fallbackDefinition != null && inventory.GetAt(i) == fallbackDefinition;

            if (!sameModel && !sameHeldSlot && !sameDefinitionWithoutModel)
                continue;

            inventory.ClearAt(i);
            if (i < slotModels.Length)
                slotModels[i] = null;
        }
    }

    private void ShiftBackpackModelsLeft(int removedIndex)
    {
        if (backpackModels == null || removedIndex < 0 || removedIndex >= backpackModels.Length)
            return;

        for (int i = removedIndex; i < backpackModels.Length - 1; i++)
            backpackModels[i] = backpackModels[i + 1];

        if (backpackModels.Length > 0)
            backpackModels[backpackModels.Length - 1] = null;
    }

    private void DropStoredItem(GameObject[] models, ItemDefinition def, int index)
    {
        if (def == null) return;

        GameObject itemObj = index >= 0 && index < models.Length ? models[index] : null;
        if (itemObj == null && def.itemPrefab != null)
            itemObj = Instantiate(def.itemPrefab);

        if (itemObj == null) return;

        itemObj.SetActive(true);
        itemObj.transform.SetParent(null);
        itemObj.transform.position = transform.position + Vector3.up * 1.5f + UnityEngine.Random.insideUnitSphere * 0.5f;
        itemObj.transform.rotation = UnityEngine.Random.rotation;

        Vector3 throwDir = (UnityEngine.Random.insideUnitSphere + Vector3.up).normalized;
        Vector3 throwVelocity = throwDir * 6f;

        ItemBase ib = itemObj.GetComponent<ItemBase>();
        if (ib != null)
        {
            if (ib.Definition == null) ib.Initialize(def);
            ib.RestoreLayer();
            ib.OnThrow(throwVelocity);
            itemObj.transform.localScale = ib.InitialScale;
        }

        Rigidbody rb = itemObj.GetComponent<Rigidbody>();
        if (rb == null) rb = itemObj.AddComponent<Rigidbody>();
        ConfigureReleasedRigidbody(rb, throwVelocity);

        if (index >= 0 && index < models.Length)
            models[index] = null;
    }

    private void EnsureModelArrays()
    {
        if (inventory == null) return;

        if (slotModels == null || slotModels.Length != inventory.maxSlots)
            System.Array.Resize(ref slotModels, inventory.maxSlots);

        if (backpackModels == null || backpackModels.Length != inventory.BackpackCapacity)
            System.Array.Resize(ref backpackModels, inventory.BackpackCapacity);
    }

    private bool IsValidAreaIndex(InventoryUI.SlotArea area, int index)
    {
        if (inventory == null || index < 0) return false;
        return area == InventoryUI.SlotArea.Equipment
            ? index < inventory.maxSlots
            : inventory.BackpackEnabled && index < inventory.BackpackCapacity;
    }

    private ItemDefinition GetDefinition(InventoryUI.SlotArea area, int index)
    {
        return area == InventoryUI.SlotArea.Equipment ? inventory.GetAt(index) : inventory.GetBackpackAt(index);
    }

    private bool SetDefinition(InventoryUI.SlotArea area, int index, ItemDefinition def)
    {
        return area == InventoryUI.SlotArea.Equipment
            ? inventory.SetAt(index, def)
            : inventory.SetBackpackAt(index, def);
    }

    private GameObject GetModel(InventoryUI.SlotArea area, int index)
    {
        GameObject[] models = area == InventoryUI.SlotArea.Equipment ? slotModels : backpackModels;
        return index >= 0 && index < models.Length ? models[index] : null;
    }

    private void SetModel(InventoryUI.SlotArea area, int index, GameObject model)
    {
        GameObject[] models = area == InventoryUI.SlotArea.Equipment ? slotModels : backpackModels;
        if (index >= 0 && index < models.Length)
            models[index] = model;
    }

    private void HideStoredModel(GameObject model)
    {
        if (model != null && model != heldObject)
            model.SetActive(false);
    }

    private void ClearHeldReferences(bool hideObject)
    {
        if (heldObject != null && hideObject)
            heldObject.SetActive(false);

        heldObject = null;
        heldItemBase = null;
        heldDefinition = null;
        heldSourceSlot = -1;
        animationController?.SetHeldItem(null);
    }
}
