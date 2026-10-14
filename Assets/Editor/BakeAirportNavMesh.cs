using UnityEngine;
using UnityEditor;

public class BakeAirportNavMesh
{
    [MenuItem("Tools/Bake Airport NavMesh", false, 60)]
    public static void Execute()
    {
        Debug.Log("[NavMeshBake] Preparing to bake NavMesh...");
        
        Collider[] allColliders = Object.FindObjectsOfType<Collider>();
        int count = 0;
        
        foreach (Collider col in allColliders)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic) continue;
                
            if (col.gameObject.name.Contains("NPC") || col.gameObject.name.Contains("Player") || col.gameObject.name.Contains("XR") || col.gameObject.name.Contains("Trolley"))
                continue;
            
            // Mark as Navigation Static
            GameObjectUtility.SetStaticEditorFlags(col.gameObject, GameObjectUtility.GetStaticEditorFlags(col.gameObject) | StaticEditorFlags.NavigationStatic);
            count++;
        }
        
        Debug.Log($"[NavMeshBake] Marked {count} objects as Navigation Static. Baking now...");
        
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        
        Debug.Log("[NavMeshBake] NavMesh baking complete! NPCs should no longer clip through objects.");
    }
}
