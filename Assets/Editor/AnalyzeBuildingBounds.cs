using UnityEngine;
using UnityEditor;

public class AnalyzeBuildingBounds
{
    [MenuItem("Tools/Debug/Analyze Airport Building Bounds")]
    public static void Execute()
    {
        GameObject building = GameObject.Find("uploads_files_6440088_Airport+building (1)");
        if (building == null) 
        {
            Debug.LogError("Building not found!");
            return;
        }
        
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) 
        {
            Debug.LogError("No renderers found in building!");
            return;
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        Debug.Log($"<color=cyan>Building Bounds - Center: {bounds.center}, Size: {bounds.size}, Min: {bounds.min}, Max: {bounds.max}</color>");
    }
}
