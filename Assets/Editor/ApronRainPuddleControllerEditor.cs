using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ApronRainPuddleController))]
public class ApronRainPuddleControllerEditor : Editor
{
    private ApronRainPuddleController controller;

    private void OnEnable()
    {
        controller = (ApronRainPuddleController)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawWeatherHeader();

        EditorGUILayout.Space(6);
        DrawLiveTelemetry();

        EditorGUILayout.Space(8);
        DrawQuickCommandButtons();

        EditorGUILayout.Space(8);
        DrawDefaultInspectorExceptExcluded();

        EditorGUILayout.Space(10);
        DrawSetupUtilities();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWeatherHeader()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 42);
        EditorGUI.DrawRect(rect, new Color(0.08f, 0.12f, 0.18f));

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.35f, 0.85f, 1.0f) }
        };

        GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.70f, 0.80f, 0.90f) }
        };

        Rect titleRect = new Rect(rect.x + 12, rect.y + 4, rect.width - 24, 18);
        Rect subRect = new Rect(rect.x + 12, rect.y + 22, rect.width - 24, 16);

        EditorGUI.LabelField(titleRect, "🌧 APRON TARMAC RAIN RIPPLES & PUDDLES", titleStyle);
        EditorGUI.LabelField(subRect, "Dynamic Surface Wetness, Expanding Wave Ripples & Ground Splashes", subStyle);
    }

    private void DrawLiveTelemetry()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        string wetState = controller.isRaining ? (controller.rainIntensity > 0.85f ? "DRENCHED RAINSTORM" : "SOAKING WET") : (controller.rainIntensity > 0.05f ? "EVAPORATING / DRYING" : "BONE DRY TARMAC");
        Color stateColor = controller.isRaining ? new Color(0.3f, 0.85f, 1.0f) : new Color(0.95f, 0.75f, 0.3f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("SURFACE STATE:", EditorStyles.boldLabel, GUILayout.Width(110));
        GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = stateColor } };
        EditorGUILayout.LabelField(wetState, stateStyle);
        EditorGUILayout.LabelField($"Puddles: {(controller.puddleCoverage * 100f):F0}%", EditorStyles.boldLabel, GUILayout.Width(95));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), controller.rainIntensity, $"WETNESS INTENSITY: {(controller.rainIntensity * 100f):F0}%");

        EditorGUILayout.EndVertical();
    }

    private void DrawQuickCommandButtons()
    {
        EditorGUILayout.LabelField("WEATHER SURFACE CONTROLS", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.75f, 1.0f);
        if (GUILayout.Button("🌧 RAIN STORM (Puddles & Ripples)", GUILayout.Height(32)))
        {
            Undo.RecordObject(controller, "Set Rain Active");
            controller.SetInstantRain(true);
            EditorUtility.SetDirty(controller);
        }

        GUI.backgroundColor = new Color(1.0f, 0.85f, 0.3f);
        if (GUILayout.Button("☀ CLEAR / DRY (Evaporate)", GUILayout.Height(32)))
        {
            Undo.RecordObject(controller, "Set Rain Inactive");
            controller.SetInstantRain(false);
            EditorUtility.SetDirty(controller);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDefaultInspectorExceptExcluded()
    {
        DrawPropertiesExcluding(serializedObject, "m_Script");
    }

    private void DrawSetupUtilities()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("MATERIAL & PARTICLES SETUP", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Click below to automatically configure the ApronTarmac material with the custom Rain Puddle & Ripple Shader, Puddle Mask, and Ground Splash Rings.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("💧 Rebuild / Apply Puddle Shader to Tarmac", GUILayout.Height(28)))
        {
            Undo.RecordObject(controller, "Configure Material");
            controller.EnsureMaterialConfigured();
            controller.SetupSplashParticleSystem();
            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    // =========================================================================
    // GLOBAL AVIATION MENU ITEM
    // =========================================================================

    [MenuItem("Tools/Aviation/Setup Rain Puddles & Ripples on ApronTarmac")]
    public static void SetupOnApronTarmac()
    {
        GameObject tarmacGo = GameObject.Find("ApronTarmac");
        if (tarmacGo == null)
        {
            // Search all loaded gameobjects
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go.name == "ApronTarmac" && go.scene.isLoaded)
                {
                    tarmacGo = go;
                    break;
                }
            }
        }

        if (tarmacGo == null)
        {
            EditorUtility.DisplayDialog("Not Found", "Could not find GameObject named 'ApronTarmac' in the active scene.", "OK");
            return;
        }

        ApronRainPuddleController ctrl = tarmacGo.GetComponent<ApronRainPuddleController>();
        if (ctrl == null)
        {
            ctrl = Undo.AddComponent<ApronRainPuddleController>(tarmacGo);
        }

        ctrl.EnsureMaterialConfigured();
        ctrl.SetupSplashParticleSystem();
        EditorUtility.SetDirty(tarmacGo);

        Selection.activeGameObject = tarmacGo;
        EditorGUIUtility.PingObject(tarmacGo);

        EditorUtility.DisplayDialog("Success", "ApronRainPuddleController and Rain Puddle/Ripple Shader successfully applied to ApronTarmac!", "Awesome!");
    }
}

[InitializeOnLoad]
public static class ApronRainPuddleAutoInitializer
{
    static ApronRainPuddleAutoInitializer()
    {
        EditorApplication.delayCall += InitializeApronTarmacIfPresent;
    }

    public static void InitializeApronTarmacIfPresent()
    {
        string matPath = "Assets/Materials/ApronTarmac_WetWeather.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader puddleShader = Shader.Find("AeroSim/ApronTarmac_RainPuddle");

        if (puddleShader != null && mat == null)
        {
            mat = new Material(puddleShader);
            mat.name = "ApronTarmac_WetWeather";

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/pebbled-asphalt1-unity/pebbled_asphalt_albedo.png");
            var norm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/pebbled-asphalt1-unity/pebbled_asphalt_Normal-ogl.png");
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/pebbled-asphalt1-unity/pebbled_asphalt_ao.png");
            var puddleMask = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/planetextures/Apron_Puddle_Mask.png");
            var rippleNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/planetextures/Rain_Ripple_Normal.png");

            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            if (norm != null) mat.SetTexture("_BumpMap", norm);
            if (ao != null) mat.SetTexture("_OcclusionMap", ao);
            if (puddleMask != null) mat.SetTexture("_PuddleMask", puddleMask);
            if (rippleNorm != null) mat.SetTexture("_RippleNormalMap", rippleNorm);

            mat.SetTextureScale("_BaseMap", new Vector2(40f, 40f));
            mat.SetFloat("_PuddleTiling", 2.0f);
            mat.SetFloat("_PuddleAmount", 0.85f);
            mat.SetFloat("_PuddleSmoothness", 0.98f);
            mat.SetFloat("_RippleTiling", 45.0f);
            mat.SetFloat("_RippleSpeed", 2.4f);
            mat.SetFloat("_RippleIntensity", 1.6f);
            mat.SetFloat("_RainIntensity", 1.0f);
            mat.SetFloat("_ReceiveShadows", 1.0f);

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
        }

        GameObject tarmacGo = GameObject.Find("ApronTarmac");
        if (tarmacGo != null)
        {
            var ctrl = tarmacGo.GetComponent<ApronRainPuddleController>();
            if (ctrl == null)
            {
                ctrl = tarmacGo.AddComponent<ApronRainPuddleController>();
            }
            if (mat != null)
            {
                var rend = tarmacGo.GetComponent<MeshRenderer>();
                if (rend != null && (rend.sharedMaterial == null || rend.sharedMaterial.shader != puddleShader))
                {
                    rend.sharedMaterial = mat;
                    ctrl.tarmacMaterial = mat;
                }
            }
            ctrl.EnsureMaterialConfigured();
            ctrl.SetupSplashParticleSystem();
            EditorUtility.SetDirty(tarmacGo);
        }
    }
}

