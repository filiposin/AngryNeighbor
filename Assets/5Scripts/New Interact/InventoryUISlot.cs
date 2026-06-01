using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUISlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] private InventoryUI owner;
    [SerializeField] private InventoryUI.SlotArea area = InventoryUI.SlotArea.Equipment;
    [SerializeField] private int index;
    [SerializeField] private RawImage iconImage;

    public InventoryUI.SlotArea Area => area;
    public int Index => index;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<InventoryUI>();

        if (iconImage == null)
            iconImage = GetComponentInChildren<RawImage>();
    }

    private void OnEnable()
    {
        if (owner != null)
            owner.RegisterSlot(this);
    }

    public void SetOwner(InventoryUI ui)
    {
        owner = ui;
    }

    public void SetIconImage(RawImage image)
    {
        iconImage = image;
    }

    public void Configure(InventoryUI.SlotArea slotArea, int slotIndex)
    {
        area = slotArea;
        index = slotIndex;
        RefreshIcon();
    }

    public void RefreshIcon()
    {
        if (owner == null) return;

        if (iconImage == null)
            iconImage = GetComponentInChildren<RawImage>();

        owner.SetSlotImage(iconImage, owner.GetItem(area, index));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
        owner?.SelectSlot(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null || !owner.HasItem(area, index)) return;
        owner.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        owner?.DropOn(this);
    }
}
