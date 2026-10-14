using UnityEngine;
using UnityEditor;

public static class SetupAirportBuildingLighting
{
    [MenuItem("Tools/Generate Airport Building Lighting", false, 11)]
    public static void Execute()
    {
        string buildingName = "uploads_files_6440088_Airport+building (1)";
        GameObject building = GameObject.Find(buildingName);
        if (building == null)
        {
            Debug.LogError($"[AirportLighting] Could not find '{buildingName}' in the scene.");
            return;
        }

        // Clean up old lighting rig if it exists
        Transform oldRig = building.transform.Find("Architectural_Lighting_Rig");
        if (oldRig != null) Object.DestroyImmediate(oldRig.gameObject);

        // Create new rig
        GameObject lightingRig = new GameObject("Architectural_Lighting_Rig");
        lightingRig.transform.SetParent(building.transform, false);

        // Calculate building bounds to determine grid
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError("[AirportLighting] Building has no renderers to calculate bounds.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float yHeight = bounds.max.y - 0.8f; // Place slightly below ceiling
        if (yHeight > 10f) yHeight = 9.5f; // Fallback hardcoded height

        int lightCount = 0;

        // Create Interior Ceiling Grid (Point Lights)
        GameObject interiorGroup = new GameObject("Interior_Ceiling_Grid");
        interiorGroup.transform.SetParent(lightingRig.transform, false);

        float xStart = bounds.min.x + 5f;
        float xEnd = bounds.max.x - 5f;
        float zStart = bounds.min.z + 5f;
        float zEnd = bounds.max.z - 5f;
        float stepSpacing = 10f; // 10 meters between lights

        Color interiorColor = new Color(0.9f, 0.95f, 1.0f); // Cool white/blue LED

        for (float x = xStart; x <= xEnd; x += stepSpacing)
        {
            for (float z = zStart; z <= zEnd; z += stepSpacing)
            {
                CreatePointLight(
                    $"LED_Panel_{lightCount++}",
                    interiorGroup.transform,
                    new Vector3(x, yHeight, z),
                    interiorColor,
                    1.2f, // Intensity
                    15f   // Range
                );
            }
        }

        // Create Exterior Facade Spotlights
        GameObject exteriorGroup = new GameObject("Exterior_Floodlights");
        exteriorGroup.transform.SetParent(lightingRig.transform, false);

        // Assuming Front facade is at Max Z and Rear tarmac is at Min Z based on bounds
        Color exteriorColor = new Color(1.0f, 0.92f, 0.8f); // Warm halogen/sodium

        // Front Entrance Floodlights
        for (float x = xStart + 5f; x <= xEnd - 5f; x += 15f)
        {
            CreateSpotLight(
                $"Front_Floodlight_{lightCount++}",
                exteriorGroup.transform,
                new Vector3(x, bounds.max.y - 1f, bounds.max.z + 1f),
                Quaternion.Euler(45f, 180f, 0f), // Point down and inward
                exteriorColor,
                2.5f,
                30f,
                70f
            );
        }

        // Rear Tarmac Floodlights
        for (float x = xStart + 5f; x <= xEnd - 5f; x += 15f)
        {
            CreateSpotLight(
                $"Rear_Floodlight_{lightCount++}",
                exteriorGroup.transform,
                new Vector3(x, bounds.max.y - 1f, bounds.min.z - 1f),
                Quaternion.Euler(30f, 0f, 0f), // Point down and outward
                exteriorColor,
                4.0f, // Brighter for tarmac
                50f,
                80f
            );
        }

        // Fix Building Materials (Upgrade to URP Lit if necessary)
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].shader.name == "Standard")
                {
                    mats[i].shader = urpShader;
                    changed = true;
                }
            }
            if (changed) r.sharedMaterials = mats;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"<color=#00FF99>[AirportLighting] Generated {lightCount} architectural lights for the airport terminal!</color>");
    }

    private static void CreatePointLight(string name, Transform parent, Vector3 worldPos, Color color, float intensity, float range)
    {
        GameObject fixture = new GameObject(name);
        fixture.transform.SetParent(parent, false);
        fixture.transform.position = worldPos;

        Light l = fixture.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.Soft;
    }

    private static void CreateSpotLight(string name, Transform parent, Vector3 worldPos, Quaternion rotation, Color color, float intensity, float range, float spotAngle)
    {
        GameObject fixture = new GameObject(name);
        fixture.transform.SetParent(parent, false);
        fixture.transform.position = worldPos;
        fixture.transform.rotation = rotation;

        Light l = fixture.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.spotAngle = spotAngle;
        l.shadows = LightShadows.Soft;
    }
}
