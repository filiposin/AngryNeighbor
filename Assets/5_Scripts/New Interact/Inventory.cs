using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public enum PickupMode
    {
        Off,
        Legacy,
        Updated,
        NineSlots,
        NineSlotsUpdated
    }

    [Serializable]
    public class InventorySlot
    {
        public ItemDefinition item;
    }

    [Header("Equipment Slots")]
    [Tooltip("Hotbar slots. These are the slots the player can equip/use.")]
    public int maxSlots = 3;

    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Optional Inventory")]
    public PickupMode pickupMode = PickupMode.Off;
    [Tooltip("Items in this inventory are shown in the optional inventory panel.")]
    public int backpackSlots = 6;

    [SerializeField] private List<InventorySlot> backpack = new List<InventorySlot>();

    public event Action OnInventoryChanged;

    public bool BackpackEnabled => pickupMode != PickupMode.Off && backpackSlots > 0;
    public int BackpackCapacity => Mathf.Max(0, backpackSlots);

    private void Awake()
    {
        EnsureSlotCounts();
    }

    private void OnValidate()
    {
        maxSlots = Mathf.Max(0, maxSlots);
        backpackSlots = Mathf.Max(0, backpackSlots);
        EnsureSlotCounts();
    }

    private void EnsureSlotCounts()
    {
        EnsureListSize(slots, maxSlots);
        EnsureListSize(backpack, BackpackCapacity);
    }

    private void EnsureListSize(List<InventorySlot> list, int size)
    {
        while (list.Count < size)
            list.Add(new InventorySlot());

        while (list.Count > size)
            list.RemoveAt(list.Count - 1);
    }

    public bool Add(ItemDefinition def)
    {
        int index = FindFirstEmptySlot();
        return index >= 0 && SetAt(index, def);
    }

    public bool AddToBackpack(ItemDefinition def)
    {
        int index = FindFirstEmptyBackpackSlot();
        return index >= 0 && SetBackpackAt(index, def);
    }

    public bool SetAt(int index, ItemDefinition def)
    {
        if (index < 0 || index >= maxSlots) return false;
        slots[index].item = def;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool SetBackpackAt(int index, ItemDefinition def)
    {
        if (index < 0 || index >= BackpackCapacity) return false;
        backpack[index].item = def;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public ItemDefinition GetAt(int index)
    {
        if (index >= 0 && index < maxSlots)
            return slots[index].item;

        return null;
    }

    public ItemDefinition GetBackpackAt(int index)
    {
        if (index >= 0 && index < BackpackCapacity)
            return backpack[index].item;

        return null;
    }

    public int GetCountAt(int index)
    {
        return GetAt(index) != null ? 1 : 0;
    }

    public int GetBackpackCountAt(int index)
    {
        return GetBackpackAt(index) != null ? 1 : 0;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= maxSlots) return false;
        if (slots[index].item == null) return false;

        slots[index].item = null;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveBackpackAt(int index)
    {
        if (index < 0 || index >= BackpackCapacity) return false;
        if (backpack[index].item == null) return false;

        backpack[index].item = null;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveBackpackAtAndShift(int index)
    {
        if (index < 0 || index >= BackpackCapacity) return false;
        if (backpack[index].item == null) return false;

        for (int i = index; i < BackpackCapacity - 1; i++)
            backpack[i].item = backpack[i + 1].item;

        if (BackpackCapacity > 0)
            backpack[BackpackCapacity - 1].item = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ClearAt(int index)
    {
        if (index < 0 || index >= maxSlots) return;
        slots[index].item = null;
        OnInventoryChanged?.Invoke();
    }

    public void ClearBackpackAt(int index)
    {
        if (index < 0 || index >= BackpackCapacity) return;
        backpack[index].item = null;
        OnInventoryChanged?.Invoke();
    }

    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (slots[i].item == null)
                return i;
        }

        return -1;
    }

    public int FindFirstEmptyBackpackSlot()
    {
        if (!BackpackEnabled) return -1;

        for (int i = 0; i < BackpackCapacity; i++)
        {
            if (backpack[i].item == null)
                return i;
        }

        return -1;
    }

    public void SelectSlotMobile(int slotNumber)
    {
        if (PlayerItemHandler.inst != null)
        {
            PlayerItemHandler.inst.ChangeSlot(slotNumber);
        }
        else
        {
            Debug.LogWarning("PlayerItemHandler instance not found!");
        }
    }
}
