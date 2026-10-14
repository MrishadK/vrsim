using UnityEngine;
using UnityEditor;

public static class SetupEngineTurbines
{
    [MenuItem("Tools/Setup Engine Turbines")]
    public static void Execute()
    {
        var plane = GameObject.Find("boeing 737-800");
        if (plane == null)
        {
            Debug.LogWarning("boeing 737-800 not found in scene.");
            return;
        }

        // Clean up any old placeholder engine objects
        var oldLeft = GameObject.Find("Left Engine Core");
        if (oldLeft != null) Object.DestroyImmediate(oldLeft);
        var oldRight = GameObject.Find("Right Engine Core");
        if (oldRight != null) Object.DestroyImmediate(oldRight);
        var oldTurbL = GameObject.Find("Left Engine Turbine");
        if (oldTurbL != null) Object.DestroyImmediate(oldTurbL);
        var oldTurbR = GameObject.Find("Right Engine Turbine");
        if (oldTurbR != null) Object.DestroyImmediate(oldTurbR);

        // Load 3D turbine model asset
        GameObject fanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/uploads_files_3639185_A320_Engine_Fan.fbx");
        if (fanPrefab == null)
        {
            Debug.LogWarning("Engine fan FBX not found at Assets/uploads_files_3639185_A320_Engine_Fan.fbx");
            return;
        }

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

        // Create Titanium Turbine Material
        Material turbineMat = new Material(urpShader != null ? urpShader : Shader.Find("Standard"));
        turbineMat.name = "Engine_Turbine_Titanium";
        turbineMat.SetColor("_BaseColor", new Color(0.22f, 0.23f, 0.25f, 1f));
        turbineMat.SetFloat("_Metallic", 0.90f);
        turbineMat.SetFloat("_Smoothness", 0.75f);

        // Left Engine Turbine
        GameObject leftTurbine = (GameObject)PrefabUtility.InstantiatePrefab(fanPrefab);
        leftTurbine.name = "Left Engine Turbine";
        leftTurbine.transform.SetParent(plane.transform, false);
        leftTurbine.transform.position = new Vector3(-68.5f, 0.45f, -31.8f);
        leftTurbine.transform.rotation = plane.transform.rotation;
        leftTurbine.transform.localScale = Vector3.one * 0.85f;
        ApplyMaterialAndSpin(leftTurbine, turbineMat);

        // Right Engine Turbine
        GameObject rightTurbine = (GameObject)PrefabUtility.InstantiatePrefab(fanPrefab);
        rightTurbine.name = "Right Engine Turbine";
        rightTurbine.transform.SetParent(plane.transform, false);
        rightTurbine.transform.position = new Vector3(-67.0f, 0.36f, -39.8f);
        rightTurbine.transform.rotation = plane.transform.rotation;
        rightTurbine.transform.localScale = Vector3.one * 0.85f;
        ApplyMaterialAndSpin(rightTurbine, turbineMat);

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("High-Quality 3D Engine Turbines installed into both nacelles successfully!");
    }

    private static void ApplyMaterialAndSpin(GameObject root, Material mat)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.sharedMaterial = mat;
        }

        if (root.GetComponent<SpinTurbine>() == null)
        {
            root.AddComponent<SpinTurbine>();
        }
    }
}
