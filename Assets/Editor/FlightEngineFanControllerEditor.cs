using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlightEngineFanController))]
public class FlightEngineFanControllerEditor : Editor
{
    private FlightEngineFanController controller;

    private void OnEnable()
    {
        controller = (FlightEngineFanController)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawFlightDeckHeader();

        EditorGUILayout.Space(6);
        DrawLiveTelemetry();

        EditorGUILayout.Space(10);
        DrawQuickCommandButtons();

        EditorGUILayout.Space(10);
        DrawDefaultInspectorExceptExcluded();

        EditorGUILayout.Space(10);
        DrawMeshExtractionUtilities();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawFlightDeckHeader()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 42);
        EditorGUI.DrawRect(rect, new Color(0.10f, 0.12f, 0.16f));

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.2f, 0.85f, 1.0f) }
        };

        GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.7f, 0.75f, 0.8f) }
        };

        Rect titleRect = new Rect(rect.x + 12, rect.y + 4, rect.width - 24, 18);
        Rect subRect = new Rect(rect.x + 12, rect.y + 22, rect.width - 24, 16);

        EditorGUI.LabelField(titleRect, "✈ FLIGHT ENGINE TURBOFAN CONTROLLER", titleStyle);
        EditorGUI.LabelField(subRect, "Boeing 787 GE GEnx / Trent 1000 Turbofan Simulation", subStyle);
    }

    private void DrawLiveTelemetry()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // State indicator
        string stateLabel = controller.engineState.ToString().ToUpper();
        Color stateColor = Color.gray;

        switch (controller.engineState)
        {
            case FlightEngineFanController.EngineState.Idle:
                stateColor = new Color(0.2f, 0.85f, 0.4f);
                break;
            case FlightEngineFanController.EngineState.Taxi:
                stateColor = new Color(0.3f, 0.75f, 1f);
                break;
            case FlightEngineFanController.EngineState.Cruise:
                stateColor = new Color(1f, 0.8f, 0.2f);
                break;
            case FlightEngineFanController.EngineState.TakeoffTOGA:
                stateColor = new Color(1f, 0.35f, 0.35f);
                break;
            case FlightEngineFanController.EngineState.Cutoff:
            case FlightEngineFanController.EngineState.ShuttingDown:
                stateColor = new Color(0.6f, 0.6f, 0.6f);
                break;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ENGINE STATUS:", EditorStyles.boldLabel, GUILayout.Width(110));
        GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = stateColor } };
        EditorGUILayout.LabelField(stateLabel, stateStyle);
        EditorGUILayout.LabelField($"N1: {controller.N1Percent:F1}%", EditorStyles.boldLabel, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // RPM gauges
        float maxGauge = controller.maxRPM > 0 ? controller.maxRPM : 5000f;
        float leftFrac = Mathf.Clamp01(controller.LeftRPM / maxGauge);
        float rightFrac = Mathf.Clamp01(controller.RightRPM / maxGauge);

        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), leftFrac, $"ENG 1 (LEFT): {controller.LeftRPM:F0} RPM");
        EditorGUILayout.Space(2);
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), rightFrac, $"ENG 2 (RIGHT): {controller.RightRPM:F0} RPM");

        EditorGUILayout.EndVertical();
    }

    private void DrawQuickCommandButtons()
    {
        EditorGUILayout.LabelField("FLIGHT DECK QUICK COMMANDS", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("🛫 START (IDLE)", GUILayout.Height(30)))
        {
            Undo.RecordObject(controller, "Start Flight Engine");
            controller.StartEngines();
            EditorUtility.SetDirty(controller);
        }

        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("🚗 TAXI POWER", GUILayout.Height(30)))
        {
            Undo.RecordObject(controller, "Taxi Power");
            controller.TaxiPower();
            EditorUtility.SetDirty(controller);
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.35f);
        if (GUILayout.Button("🚀 TAKEOFF (TOGA)", GUILayout.Height(30)))
        {
            Undo.RecordObject(controller, "Takeoff TOGA");
            controller.FullTakeoffThrust();
            EditorUtility.SetDirty(controller);
        }

        GUI.backgroundColor = new Color(0.55f, 0.55f, 0.55f);
        if (GUILayout.Button("🛑 CUT ENGINES", GUILayout.Height(30)))
        {
            Undo.RecordObject(controller, "Cut Engines");
            controller.CutEngines();
            EditorUtility.SetDirty(controller);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDefaultInspectorExceptExcluded()
    {
        DrawPropertiesExcluding(serializedObject, "m_Script");
    }

    private void DrawMeshExtractionUtilities()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("ROTOR EXTRACTION UTILITIES", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "If your aircraft has 'Material2.010' combined with both fans in one static mesh, click below to extract them into separate, perfectly centered Left & Right rotor assemblies.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("⚡ Auto-Configure / Extract Rotors Now", GUILayout.Height(28)))
        {
            Undo.RecordObject(controller, "Extract Rotors");
            controller.EnsureRotorsConfigured();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("💾 Save Meshes as Project Assets", GUILayout.Height(28)))
        {
            SaveRotorsAsAssets();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void SaveRotorsAsAssets()
    {
        if (controller.leftTurbofanRotor == null || controller.rightTurbofanRotor == null)
        {
            controller.EnsureRotorsConfigured();
        }

        if (controller.leftTurbofanRotor == null || controller.rightTurbofanRotor == null)
        {
            EditorUtility.DisplayDialog("Notice", "Could not locate Left or Right rotor meshes to save.", "OK");
            return;
        }

        MeshFilter lmf = controller.leftTurbofanRotor.GetComponent<MeshFilter>();
        MeshFilter rmf = controller.rightTurbofanRotor.GetComponent<MeshFilter>();

        if (lmf != null && lmf.sharedMesh != null)
        {
            string pathL = "Assets/GeneratedModels/Boeing787_LeftTurbofan.asset";
            AssetDatabase.CreateAsset(Instantiate(lmf.sharedMesh), pathL);
            lmf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathL);
        }

        if (rmf != null && rmf.sharedMesh != null)
        {
            string pathR = "Assets/GeneratedModels/Boeing787_RightTurbofan.asset";
            AssetDatabase.CreateAsset(Instantiate(rmf.sharedMesh), pathR);
            rmf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathR);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Turbofan meshes saved to Assets/GeneratedModels/!", "OK");
    }

    // =========================================================================
    // 6. GLOBAL MENU ITEMS
    // =========================================================================

    [MenuItem("Tools/Aviation/Setup Flight Engine Fans on Material2.010")]
    public static void SetupOnMaterial010()
    {
        GameObject mat010Go = GameObject.Find("Material2.010");
        if (mat010Go == null)
        {
            // Search all objects including disabled
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go.name == "Material2.010" && go.scene.isLoaded)
                {
                    mat010Go = go;
                    break;
                }
            }
        }

        if (mat010Go == null)
        {
            EditorUtility.DisplayDialog("Not Found", "Could not find a GameObject named 'Material2.010' in the active scene.", "OK");
            return;
        }

        FlightEngineFanController ctrl = mat010Go.GetComponent<FlightEngineFanController>();
        if (ctrl == null)
        {
            ctrl = Undo.AddComponent<FlightEngineFanController>(mat010Go);
        }

        ctrl.EnsureRotorsConfigured();
        EditorUtility.SetDirty(mat010Go);

        Selection.activeGameObject = mat010Go;
        EditorGUIUtility.PingObject(mat010Go);

        EditorUtility.DisplayDialog("Success", "FlightEngineFanController attached and configured on Material2.010! Left and Right fans are ready to spin.", "Awesome!");
    }

    [MenuItem("Tools/Aviation/Spin Flight Engine Fans (Toggle Takeoff Power)")]
    public static void TogglePower()
    {
        var ctrl = Object.FindAnyObjectByType<FlightEngineFanController>();
        if (ctrl == null)
        {
            SetupOnMaterial010();
            ctrl = Object.FindAnyObjectByType<FlightEngineFanController>();
        }

        if (ctrl != null)
        {
            if (ctrl.engineState == FlightEngineFanController.EngineState.TakeoffTOGA)
            {
                ctrl.StartEngines(); // Back to idle
            }
            else
            {
                ctrl.FullTakeoffThrust();
            }
            EditorUtility.SetDirty(ctrl);
        }
    }
}

[InitializeOnLoad]
public static class FlightEngineFanEditorAutoSetup
{
    static FlightEngineFanEditorAutoSetup()
    {
        EditorApplication.delayCall += OnEditorReady;
    }

    private static void OnEditorReady()
    {
        GameObject mat010Go = GameObject.Find("Material2.010");
        if (mat010Go != null && mat010Go.GetComponent<FlightEngineFanController>() == null)
        {
            var ctrl = mat010Go.AddComponent<FlightEngineFanController>();
            ctrl.EnsureRotorsConfigured();
            EditorUtility.SetDirty(mat010Go);
            Debug.Log("<color=#00e676><b>[FlightEngineFanController]</b></color> Auto-configured Material2.010 flight engine fans in active scene!");
        }
    }
}

