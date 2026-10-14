using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Linq;

public class FixTreesAndSetupQueues
{
    [MenuItem("Tools/Fix Trees and Setup Queues", false, 65)]
    public static void Execute()
    {
        Debug.Log("[FixTreesAndSetupQueues] Starting fixes...");

        // 1. Fix moving trees by stripping AI components
        int treesFixed = 0;
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("tree") || obj.name.ToLower().Contains("stylized"))
            {
                // Remove rogue NavMeshAgent
                NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    Object.DestroyImmediate(agent);
                    treesFixed++;
                }

                // Remove rogue custom AI scripts if present (WanderingAI, etc)
                // The easiest way is to look for MonoBehaviours and remove if they sound like AI
                Component[] comps = obj.GetComponents<MonoBehaviour>();
                foreach (Component c in comps)
                {
                    string cName = c.GetType().Name.ToLower();
                    if (cName.Contains("ai") || cName.Contains("npc") || cName.Contains("vehicle"))
                    {
                        Object.DestroyImmediate(c);
                        treesFixed++;
                    }
                }
            }
        }
        
        Debug.Log($"[FixTreesAndSetupQueues] Stripped rogue AI components from {treesFixed} trees.");

        // 2. Attach QueueStation to waypoints
        AirportRouteManager routeManager = Object.FindObjectOfType<AirportRouteManager>();
        int queuesAdded = 0;
        if (routeManager != null)
        {
            var allWaypointLists = new[]
            {
                routeManager.entryPoints,
                routeManager.checkInPoints,
                routeManager.securityPoints,
                routeManager.immigrationPoints
            };

            foreach (var list in allWaypointLists)
            {
                if (list == null) continue;
                foreach (Transform t in list)
                {
                    if (t != null && t.GetComponent<QueueStation>() == null)
                    {
                        t.gameObject.AddComponent<QueueStation>();
                        queuesAdded++;
                    }
                }
            }
        }

        Debug.Log($"[FixTreesAndSetupQueues] Attached {queuesAdded} QueueStation scripts to waypoints.");
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("[FixTreesAndSetupQueues] Done! Play the game to test.");
    }
}
