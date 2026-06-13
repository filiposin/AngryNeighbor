using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SimpleReplacer : MonoBehaviour, IHittable
{
    [Tooltip("Префаб, которым заменить этот объект")]
    public GameObject replacementPrefab;
    public string[] usedIds = new string[] { "4" }; 
    public int maxHits = 1;
    // минимальные опции — выключены по-умолчанию
    public bool copyParent = false;
    public bool copyScale = false;
    public bool copyVelocity = false;
    
    public UnityEvent onBreak;

    private int currentHits = 0;
    public GameObject LastSpawnedReplacement { get; private set; }
    public bool LastReplaceSucceeded { get; private set; }

    public void TryReplace(string id)
    {
        LastSpawnedReplacement = null;
        LastReplaceSucceeded = false;

        bool isValid = false;
        if (usedIds != null)
        {
            for (int i = 0; i < usedIds.Length; i++)
            {
                if (usedIds[i] == id)
                {
                    isValid = true;
                    break;
                }
            }
        }

        if (!isValid) return;

        if (currentHits < maxHits) currentHits++;
        
        if (currentHits >= maxHits)
        {
            currentHits = 0;
            Replace();
        }
        else 
        {
            Debug.Log("hitten object. hits: " + currentHits);
        }
    }

    public void Replace()
    {
        LastSpawnedReplacement = null;
        LastReplaceSucceeded = false;

        if (replacementPrefab == null)
        {
            LastReplaceSucceeded = true;
            Destroy(gameObject);
            return;
        }

        GameObject spawned = Instantiate(replacementPrefab, transform.position, transform.rotation);
        onBreak?.Invoke();

        if (copyParent) spawned.transform.SetParent(transform.parent, true);
        if (copyScale) spawned.transform.localScale = transform.localScale;
        if (copyVelocity)
        {
            var src = GetComponent<Rigidbody>();
            if (src != null)
            {
                var dst = spawned.GetComponent<Rigidbody>() ?? spawned.GetComponentInChildren<Rigidbody>();
                if (dst != null) dst.velocity = src.velocity;
            }
        }
        LastSpawnedReplacement = spawned;
        LastReplaceSucceeded = true;

        Destroy(gameObject);
    }
}