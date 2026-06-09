using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public enum SlotArea
    {
        Equipment,
        Backpack
    }

    [Header("Panel")]
    public GameObject inventoryPanel;
    public bool closeOnStart = true;
    [Tooltip("If enabled, RawImages assigned below automatically become drag/drop slots.")]
    public bool autoSetupRawImageSlots = true;

    [Header("Legacy Slot Images")]
    public RawImage slot1Image;
    public RawImage slot2Image;
    public RawImage slot3Image;

    [Header("Slot Images")]
    public RawImage[] equipmentSlotImages;
    public RawImage[] backpackSlotImages;
    public Texture emptyTexture;

    private Inventory inventory;
    private PlayerItemHandler itemHandler;
    private InventoryUISlot draggedSlot;
    private RectTransform dragIconRect;
    private GameObject dragIconObject;
    private Canvas rootCanvas;

    public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        if (inventoryPanel != null && closeOnStart)
            inventoryPanel.SetActive(false);

        AutoBindSlots();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    public void Bind(Inventory inv)
    {
        Bind(inv, null);
    }

    public void Bind(Inventory inv, PlayerItemHandler handler)
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;

        inventory = inv;
        itemHandler = handler;

        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;

        AutoBindSlots();
        Refresh();
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        if (inventoryPanel == null) return;

        inventoryPanel.SetActive(open);

        if (!open)
            ClearDragIcon();
    }

    public void Refresh()
    {
        AutoBindSlots();

        if (equipmentSlotImages == null || equipmentSlotImages.Length == 0)
        {
            SetSlotImage(slot1Image, inventory?.GetAt(0));
            SetSlotImage(slot2Image, inventory?.GetAt(1));
            SetSlotImage(slot3Image, inventory?.GetAt(2));
        }
        else
        {
            for (int i = 0; i < equipmentSlotImages.Length; i++)
                SetSlotImage(equipmentSlotImages[i], inventory?.GetAt(i));
        }

        if (backpackSlotImages != null)
        {
            for (int i = 0; i < backpackSlotImages.Length; i++)
                SetSlotImage(backpackSlotImages[i], inventory?.GetBackpackAt(i));
        }

        InventoryUISlot[] slots = GetComponentsInChildren<InventoryUISlot>(true);
        for (int i = 0; i < slots.Length; i++)
            slots[i].RefreshIcon();
    }

    public ItemDefinition GetItem(SlotArea area, int index)
    {
        if (inventory == null) return null;
        return area == SlotArea.Equipment ? inventory.GetAt(index) : inventory.GetBackpackAt(index);
    }

    public bool HasItem(SlotArea area, int index)
    {
        return GetItem(area, index) != null;
    }

    public void SelectSlot(InventoryUISlot slot)
    {
        if (slot == null || slot.Area != SlotArea.Equipment || itemHandler == null) return;
        itemHandler.ChangeSlot(slot.Index + 1);
    }

    public void BeginDrag(InventoryUISlot slot, PointerEventData eventData)
    {
        if (slot == null || !HasItem(slot.Area, slot.Index)) return;

        draggedSlot = slot;
        CreateDragIcon(GetItem(slot.Area, slot.Index));
        UpdateDrag(eventData);
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (dragIconRect == null || eventData == null) return;
        dragIconRect.position = eventData.position;
    }

    public void EndDrag()
    {
        draggedSlot = null;
        ClearDragIcon();
    }

    public void DropOn(InventoryUISlot target)
    {
        if (draggedSlot == null || target == null || itemHandler == null) return;

        if (draggedSlot.Area == target.Area && draggedSlot.Index == target.Index)
            return;

        itemHandler.TryMoveInventoryItem(draggedSlot.Area, draggedSlot.Index, target.Area, target.Index);
    }

    public void RegisterSlot(InventoryUISlot slot)
    {
        if (slot == null) return;
        slot.SetOwner(this);
        slot.RefreshIcon();
    }

    public void SetSlotImage(RawImage img, ItemDefinition def)
    {
        if (img == null) return;

        if (def != null && def.icon != null)
        {
            img.texture = def.icon.texture;
            img.color = Color.white;
        }
        else
        {
            img.texture = emptyTexture;
            img.color = emptyTexture == null ? new Color(1, 1, 1, 0) : Color.white;
        }
    }

    private void AutoBindSlots()
    {
        if (autoSetupRawImageSlots)
            AutoSetupRawImageSlots();

        InventoryUISlot[] slots = GetComponentsInChildren<InventoryUISlot>(true);
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetOwner(this);
    }

    private void AutoSetupRawImageSlots()
    {
        if (equipmentSlotImages != null && equipmentSlotImages.Length > 0)
        {
            for (int i = 0; i < equipmentSlotImages.Length; i++)
                SetupRawImageSlot(equipmentSlotImages[i], SlotArea.Equipment, i);
        }
        else
        {
            SetupRawImageSlot(slot1Image, SlotArea.Equipment, 0);
            SetupRawImageSlot(slot2Image, SlotArea.Equipment, 1);
            SetupRawImageSlot(slot3Image, SlotArea.Equipment, 2);
        }

        if (backpackSlotImages == null) return;

        for (int i = 0; i < backpackSlotImages.Length; i++)
            SetupRawImageSlot(backpackSlotImages[i], SlotArea.Backpack, i);
    }

    private void SetupRawImageSlot(RawImage image, SlotArea area, int index)
    {
        if (image == null) return;

        image.raycastTarget = true;

        InventoryUISlot slot = image.GetComponent<InventoryUISlot>();
        if (slot == null)
            slot = image.gameObject.AddComponent<InventoryUISlot>();

        slot.SetOwner(this);
        slot.SetIconImage(image);
        slot.Configure(area, index);
    }

    private void CreateDragIcon(ItemDefinition def)
    {
        ClearDragIcon();
        if (def == null || def.icon == null) return;

        Transform parent = rootCanvas != null ? rootCanvas.transform : transform;
        dragIconObject = new GameObject("InventoryDragIcon");
        dragIconObject.transform.SetParent(parent, false);

        Image image = dragIconObject.AddComponent<Image>();
        image.sprite = def.icon;
        image.raycastTarget = false;
        image.preserveAspect = true;

        CanvasGroup group = dragIconObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        dragIconRect = dragIconObject.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(64f, 64f);
    }

    private void ClearDragIcon()
    {
        if (dragIconObject != null)
            Destroy(dragIconObject);

        dragIconObject = null;
        dragIconRect = null;
    }
}
