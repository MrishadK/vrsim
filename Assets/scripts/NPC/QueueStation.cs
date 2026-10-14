using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a single-file line of passengers waiting for service at an airport station.
/// Front of line is served at the station counter; other passengers wait in line behind them.
/// </summary>
public class QueueStation : MonoBehaviour
{
    [Tooltip("Spacing between NPCs in line (meters)")]
    public float spacing = 1.4f;

    [Tooltip("Max capacity for this queue before new arrivals should wait or pick another station")]
    public int maxQueueCapacity = 8;

    [Tooltip("Optional custom queue line direction. If Vector3.zero, defaults to -transform.forward")]
    public Vector3 customQueueDirection = Vector3.zero;

    [Tooltip("Current NPCs waiting in this line")]
    public List<AirportNPC> queue = new List<AirportNPC>();

    public Vector3 QueueDirection
    {
        get
        {
            if (customQueueDirection != Vector3.zero)
                return customQueueDirection.normalized;
            return -transform.forward;
        }
    }

    /// <summary>
    /// Gets the target position for a specific NPC in the queue.
    /// </summary>
    public Vector3 GetQueuePosition(AirportNPC npc)
    {
        int index = queue.IndexOf(npc);
        if (index == -1)
        {
            index = queue.Count;
        }

        Vector3 qDir = QueueDirection;
        Vector3 basePos = transform.position + (qDir * (index * spacing));

        if (index == 0) return transform.position; // First person stands right at the counter

        // Deterministic organic jitter without breaking global RNG
        float jitterOffset = Mathf.Sin(npc.GetInstanceID() * 0.173f) * 0.25f;
        Vector3 rightVec = Vector3.Cross(Vector3.up, qDir).normalized;

        return basePos + (rightVec * jitterOffset);
    }

    /// <summary>
    /// Joins the queue.
    /// </summary>
    public void JoinQueue(AirportNPC npc)
    {
        if (npc != null && !queue.Contains(npc))
        {
            queue.Add(npc);
        }
    }

    /// <summary>
    /// Leaves the queue and notifies everyone else to step forward.
    /// </summary>
    public void LeaveQueue(AirportNPC npc)
    {
        if (npc != null && queue.Contains(npc))
        {
            queue.Remove(npc);

            // Clean any dead or null references in the queue
            queue.RemoveAll(p => p == null || p.gameObject == null);

            // Tell everyone remaining in the queue to step forward
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i] != null)
                {
                    queue[i].NotifyQueueAdvanced();
                }
            }
        }
    }

    /// <summary>
    /// Is the NPC currently first in line?
    /// </summary>
    public bool IsFirstInLine(AirportNPC npc)
    {
        queue.RemoveAll(p => p == null || p.gameObject == null);
        if (queue.Count == 0) return false;
        return queue[0] == npc;
    }

    public int QueueCount => queue.Count;
}
