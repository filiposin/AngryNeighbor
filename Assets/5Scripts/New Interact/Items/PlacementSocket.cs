using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlacementSocket : MonoBehaviour
{
    public Transform snapPoint; // Точка, куда встанет домкрат (создай пустышку SnapPos внутри)
    public List<string> allowedItemIds;
    public event Action<GameObject> OnRemove;
    public event Action OnSpawn;
    public GameObject CurrentItem
    {
        get => currentItem;
        private set => currentItem = value;
    }

    private bool isOccupied = false;
    internal GameObject currentItem;

    private void Awake() 
    {
        if(snapPoint == null) snapPoint = transform;
        // AudioSource[] sources = Resources.FindObjectsOfTypeAll(typeof(AudioSource)).Cast<AudioSource>();
    }

    public bool CanAcceptItem(string id)
    {
        if (isOccupied) return false;
        if (allowedItemIds.Count == 0) return true;
        return allowedItemIds.Contains(id);
    }

    public virtual void PlaceItem(GameObject item)
    {
        isOccupied = true;
        currentItem = item;
        OnRemove?.Invoke(item);
    }

    public void RemoveItem()
    {
        isOccupied = false;
        currentItem = null;
        OnSpawn?.Invoke();
    }
}