using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SafeZone : MonoBehaviour
{
    [SerializeField] private float staleOccupantTimeout = 0.25f;
    [SerializeField] private RichAI_EnemyController enemyController;
    [SerializeField] private bool findEnemyIfMissing = true;
    [SerializeField] private bool makeEnemyLosePlayerOnEnter = true;

    private static readonly Dictionary<int, int> OccupancyCounts = new Dictionary<int, int>();
    private static readonly Dictionary<int, float> OccupancyTouchTimes = new Dictionary<int, float>();
    private static float globalStaleOccupantTimeout = 0.25f;

    private readonly Dictionary<int, int> localOccupants = new Dictionary<int, int>();

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;

        if (enemyController == null && findEnemyIfMissing)
            enemyController = FindObjectOfType<RichAI_EnemyController>();

        globalStaleOccupantTimeout = Mathf.Max(0.05f, staleOccupantTimeout);
    }

    private void OnValidate()
    {
        globalStaleOccupantTimeout = Mathf.Max(0.05f, staleOccupantTimeout);
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<int, int> entry in localOccupants)
            if (entry.Value > 0)
                RemoveOccupant(entry.Key);

        localOccupants.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterOccupant(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterOccupant(other, false);
    }

    private void OnTriggerExit(Collider other)
    {
        FP_Controller player = other.GetComponentInParent<FP_Controller>();
        if (player == null)
            return;

        int id = player.gameObject.GetInstanceID();
        if (!localOccupants.TryGetValue(id, out int localCount))
            return;

        localCount--;
        if (localCount <= 0)
        {
            localOccupants.Remove(id);
            RemoveOccupant(id);
            return;
        }

        localOccupants[id] = localCount;
    }

    public static bool Contains(Transform target)
    {
        if (target == null)
            return false;

        int id = target.gameObject.GetInstanceID();
        if (!OccupancyCounts.TryGetValue(id, out int count) || count <= 0)
            return false;

        if (OccupancyTouchTimes.TryGetValue(id, out float lastTouchTime) && Time.time - lastTouchTime <= globalStaleOccupantTimeout)
            return true;

        OccupancyCounts.Remove(id);
        OccupancyTouchTimes.Remove(id);
        return false;
    }

    private void RegisterOccupant(Collider other, bool incrementColliderCount)
    {
        FP_Controller player = other.GetComponentInParent<FP_Controller>();
        if (player == null)
            return;

        int id = player.gameObject.GetInstanceID();
        if (!localOccupants.TryGetValue(id, out int localCount))
        {
            localOccupants[id] = 1;
            if (OccupancyCounts.TryGetValue(id, out int count))
                OccupancyCounts[id] = count + 1;
            else
                OccupancyCounts[id] = 1;

            HandlePlayerEntered(player);
        }
        else if (incrementColliderCount)
        {
            localOccupants[id] = localCount + 1;
        }

        OccupancyTouchTimes[id] = Time.time;
    }

    private void HandlePlayerEntered(FP_Controller player)
    {
        if (!makeEnemyLosePlayerOnEnter || player == null)
            return;

        if (enemyController == null && findEnemyIfMissing)
            enemyController = FindObjectOfType<RichAI_EnemyController>();

        if (enemyController != null && enemyController.IsChasingPlayer())
            enemyController.LosePlayerToSafeZone();
    }

    private void RemoveOccupant(int id)
    {
        if (!OccupancyCounts.TryGetValue(id, out int count))
            return;

        count--;
        if (count <= 0)
        {
            OccupancyCounts.Remove(id);
            OccupancyTouchTimes.Remove(id);
            return;
        }

        OccupancyCounts[id] = count;
    }
}
