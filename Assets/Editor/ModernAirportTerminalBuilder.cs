using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Professional Modern Airport Terminal Builder for Unity.
/// Procedurally constructs a state-of-the-art international airport terminal with full interior and exterior details.
/// </summary>
public class ModernAirportTerminalBuilder : EditorWindow
{
    private const string MAT_FOLDER = "Assets/GeneratedMaterials/ModernAirport";
    private const string PREFAB_FOLDER = "Assets/Prefabs/ModernAirport";
    private const string TEX_FOLDER = "Assets/GeneratedTextures/ModernAirport";

    private Vector3 spawnPosition = new Vector3(-30f, 0f, 0f);
    private bool includeInterior = true;
    private bool includeLighting = true;
    private bool includeJetways = true;
    private bool includeRooftopDetails = true;
    private bool includeFIDS = true;

    [MenuItem("Tools/Generate Modern Airport Terminal #&t", false, 4)]
    public static void BuildTerminalDirect()
    {
        var builder = CreateInstance<ModernAirportTerminalBuilder>();
        builder.BuildFullTerminal();
    }

    [MenuItem("Tools/Modern Airport Terminal Builder Window", false, 5)]
    public static void ShowWindow()
    {
        var window = GetWindow<ModernAirportTerminalBuilder>("Airport Terminal Builder");
        window.minSize = new Vector2(460, 560);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("Modern International Airport Terminal Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Constructs a hyper-detailed, architectural international airport terminal featuring curved aerodynamic canopies, panoramic curtain glass, complete check-in hall, security checkpoint, passport e-gates, departure lounge, jetways, FIDS display, and rooftop HVAC/avionics.", MessageType.Info);

        EditorGUILayout.Space(10);
        GUILayout.Label("Configuration & Placement", EditorStyles.boldLabel);
        spawnPosition = EditorGUILayout.Vector3Field("Spawn Origin Position", spawnPosition);

        EditorGUILayout.Space(6);
        includeInterior = EditorGUILayout.Toggle("Include Full Interior Details", includeInterior);
        includeJetways = EditorGUILayout.Toggle("Include Jetways & Apron Gates", includeJetways);
        includeRooftopDetails = EditorGUILayout.Toggle("Include Rooftop HVAC & Radar", includeRooftopDetails);
        includeFIDS = EditorGUILayout.Toggle("Include Live FIDS Display Board", includeFIDS);
        includeLighting = EditorGUILayout.Toggle("Include Point & Accent Lighting", includeLighting);

        EditorGUILayout.Space(15);
        if (GUILayout.Button("Generate & Place Modern Terminal", GUILayout.Height(45)))
        {
            BuildFullTerminal();
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Clear Existing Terminal from Scene", GUILayout.Height(28)))
        {
            ClearExistingTerminal();
        }
    }

    public void BuildFullTerminal()
    {
        ClearExistingTerminal();
        EnsureFolders();

        // 1. Generate PBR Materials
        var mats = GenerateMaterials();

        // 2. Master Root
        GameObject root = new GameObject("Modern_International_Airport_Terminal");
        root.transform.position = spawnPosition;

        // 3. Build Subsystems
        BuildTerminalExterior(root.transform, mats);
        if (includeInterior) BuildTerminalInterior(root.transform, mats);
        if (includeJetways) BuildJetwaysAndGates(root.transform, mats);
        if (includeRooftopDetails) BuildRooftopSystems(root.transform, mats);
        if (includeLighting) BuildTerminalLighting(root.transform, mats);

        // 4. Save as Prefab
        string prefabPath = $"{PREFAB_FOLDER}/Modern_Airport_Terminal.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("<color=#00FFCC>[ModernAirportTerminalBuilder]</color> Modern International Airport Terminal created successfully with full architectural details!");
    }

    private static void EnsureFolders()
    {
        if (!Directory.Exists(MAT_FOLDER)) Directory.CreateDirectory(MAT_FOLDER);
        if (!Directory.Exists(PREFAB_FOLDER)) Directory.CreateDirectory(PREFAB_FOLDER);
        if (!Directory.Exists(TEX_FOLDER)) Directory.CreateDirectory(TEX_FOLDER);
    }

    public static void ClearExistingTerminal()
    {
        var existing = GameObject.Find("Modern_International_Airport_Terminal");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[ModernAirportTerminalBuilder] Cleared existing terminal.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  MATERIALS & TEXTURES
    // ═══════════════════════════════════════════════════════════

    public struct TerminalMaterials
    {
        public Material polishedFloor;
        public Material whiteComposite;
        public Material curtainGlass;
        public Material darkSteel;
        public Material acousticCeiling;
        public Material woodAccents;
        public Material cyanNeon;
        public Material goldSignage;
        public Material conveyorBelt;
        public Material tarmacAsphalt;
        public Material redBeacon;
        public Material concretePillar;
    }

    private TerminalMaterials GenerateMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        TerminalMaterials m = new TerminalMaterials();

        // 1. Polished Marble Terrazzo Floor
        m.polishedFloor = CreateMat("Mat_Terminal_PolishedMarble", litShader, new Color(0.92f, 0.93f, 0.94f), 0.15f, 0.92f);

        // 2. White Architectural Composite Panel
        m.whiteComposite = CreateMat("Mat_Terminal_WhiteComposite", litShader, new Color(0.96f, 0.97f, 0.98f), 0.10f, 0.40f);

        // 3. Panoramic Tinted Curtain Glass
        m.curtainGlass = CreateMat("Mat_Terminal_CurtainGlass", litShader, new Color(0.20f, 0.45f, 0.65f, 0.55f), 0.85f, 0.95f);
        m.curtainGlass.SetFloat("_Surface", 1); // Transparent
        m.curtainGlass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.curtainGlass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.curtainGlass.SetInt("_ZWrite", 0);
        m.curtainGlass.renderQueue = 3000;

        // 4. Dark Steel Structural Mullions & Trusses
        m.darkSteel = CreateMat("Mat_Terminal_DarkSteel", litShader, new Color(0.14f, 0.16f, 0.18f), 0.80f, 0.70f);

        // 5. Perforated Acoustic Ceiling
        m.acousticCeiling = CreateMat("Mat_Terminal_AcousticCeiling", litShader, new Color(0.30f, 0.32f, 0.35f), 0.20f, 0.30f);

        // 6. Warm Natural Wood Wall Slats
        m.woodAccents = CreateMat("Mat_Terminal_WarmWood", litShader, new Color(0.65f, 0.42f, 0.24f), 0.05f, 0.50f);

        // 7. Cyan Architectural Glow Neon
        m.cyanNeon = CreateMat("Mat_Terminal_CyanNeon", litShader, new Color(0.1f, 0.9f, 1f), 0f, 0.9f);
        m.cyanNeon.EnableKeyword("_EMISSION");
        m.cyanNeon.SetColor("_EmissionColor", new Color(0.2f, 1.8f, 2.2f));

        // 8. Illuminated Flight Signage Gold
        m.goldSignage = CreateMat("Mat_Terminal_GoldSignage", litShader, new Color(1f, 0.85f, 0.2f), 0f, 0.8f);
        m.goldSignage.EnableKeyword("_EMISSION");
        m.goldSignage.SetColor("_EmissionColor", new Color(2f, 1.6f, 0.3f));

        // 9. Luggage Conveyor Belt Rubber
        m.conveyorBelt = CreateMat("Mat_Terminal_ConveyorBelt", litShader, new Color(0.12f, 0.12f, 0.13f), 0.10f, 0.20f);

        // 10. Tarmac Apron Asphalt
        m.tarmacAsphalt = CreateMat("Mat_Terminal_ApronAsphalt", litShader, new Color(0.22f, 0.23f, 0.25f), 0.05f, 0.25f);

        // 11. Red Aviation Beacon
        m.redBeacon = CreateMat("Mat_Terminal_RedBeacon", litShader, Color.red, 0f, 0.9f);
        m.redBeacon.EnableKeyword("_EMISSION");
        m.redBeacon.SetColor("_EmissionColor", Color.red * 3f);

        // 12. Concrete Support Pillars
        m.concretePillar = CreateMat("Mat_Terminal_ConcretePillar", litShader, new Color(0.85f, 0.86f, 0.87f), 0.05f, 0.35f);

        return m;
    }

    private static Material CreateMat(string name, Shader shader, Color baseColor, float metallic, float smoothness)
    {
        string path = $"{MAT_FOLDER}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_Color", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // ═══════════════════════════════════════════════════════════
    //  EXTERIOR ENVELOPE & ARCHITECTURE
    // ═══════════════════════════════════════════════════════════

    private void BuildTerminalExterior(Transform parent, TerminalMaterials mats)
    {
        GameObject ext = new GameObject("01_Exterior_Architecture");
        ext.transform.SetParent(parent, false);

        // --- Foundation / Ground Slab ---
        CreateBox("Terminal_Floor_Slab", ext.transform, new Vector3(0, -0.5f, 0), new Vector3(120f, 1f, 80f), mats.polishedFloor);
        CreateBox("Apron_Tarmac_Ground", ext.transform, new Vector3(0, -0.6f, 70f), new Vector3(180f, 0.8f, 60f), mats.tarmacAsphalt);

        // --- Aerodynamic Wave Roof Canopy ---
        GameObject roof = new GameObject("Aerodynamic_Roof_Canopy");
        roof.transform.SetParent(ext.transform, false);

        // Central High Roof Vault
        CreateBox("Roof_Central_Vault", roof.transform, new Vector3(0, 18.5f, 0), new Vector3(126f, 1.2f, 86f), mats.whiteComposite);
        // Roof Skylight Glass Ribbon
        CreateBox("Roof_Skylight_Ribbon", roof.transform, new Vector3(0, 19f, 0), new Vector3(110f, 0.4f, 12f), mats.curtainGlass);
        // Overhanging Front & Rear Eaves
        CreateBox("Roof_Front_Overhang", roof.transform, new Vector3(0, 17f, -44f), new Vector3(130f, 0.8f, 10f), mats.whiteComposite);
        CreateBox("Roof_Airside_Overhang", roof.transform, new Vector3(0, 17f, 44f), new Vector3(130f, 0.8f, 10f), mats.whiteComposite);

        // --- Structural Y-Tree Support Columns ---
        GameObject columns = new GameObject("Structural_Tree_Columns");
        columns.transform.SetParent(ext.transform, false);

        float[] xCols = new float[] { -45f, -15f, 15f, 45f };
        float[] zCols = new float[] { -25f, 0f, 25f };

        foreach (float x in xCols)
        {
            foreach (float z in zCols)
            {
                // Main Vertical Column
                CreateCylinder($"Col_{x}_{z}", columns.transform, new Vector3(x, 8.5f, z), new Vector3(1.6f, 8.5f, 1.6f), mats.concretePillar);
                // Diagonal Tree Braces
                CreateBox($"Brace_L_{x}_{z}", columns.transform, new Vector3(x - 2f, 16.8f, z), new Vector3(5f, 0.5f, 0.8f), mats.darkSteel, Quaternion.Euler(0, 0, 25f));
                CreateBox($"Brace_R_{x}_{z}", columns.transform, new Vector3(x + 2f, 16.8f, z), new Vector3(5f, 0.5f, 0.8f), mats.darkSteel, Quaternion.Euler(0, 0, -25f));
            }
        }

        // --- Panoramic Curtain Glass Facades ---
        GameObject facade = new GameObject("Curtain_Glass_Facades");
        facade.transform.SetParent(ext.transform, false);

        // Front Landside Entrance Glass Wall
        CreateBox("Facade_Landside_Glass", facade.transform, new Vector3(0, 8.5f, -39.8f), new Vector3(118f, 17f, 0.3f), mats.curtainGlass);
        // Airside Runway Viewing Glass Wall
        CreateBox("Facade_Airside_Glass", facade.transform, new Vector3(0, 8.5f, 39.8f), new Vector3(118f, 17f, 0.3f), mats.curtainGlass);
        // East & West Side Walls (Insulated White Panels + Accent Wood)
        CreateBox("Wall_East_Composite", facade.transform, new Vector3(59.8f, 8.5f, 0), new Vector3(0.4f, 17f, 80f), mats.whiteComposite);
        CreateBox("Wall_West_Composite", facade.transform, new Vector3(-59.8f, 8.5f, 0), new Vector3(0.4f, 17f, 80f), mats.whiteComposite);

        // Steel Mullion Grids
        for (float x = -50f; x <= 50f; x += 10f)
        {
            CreateBox($"Mullion_Front_{x}", facade.transform, new Vector3(x, 8.5f, -39.7f), new Vector3(0.3f, 17f, 0.5f), mats.darkSteel);
            CreateBox($"Mullion_Back_{x}", facade.transform, new Vector3(x, 8.5f, 39.7f), new Vector3(0.3f, 17f, 0.5f), mats.darkSteel);
        }

        // --- Landside Drop-Off Viaduct & Automated Entrance Portals ---
        GameObject dropoff = new GameObject("Landside_Dropoff_Viaduct");
        dropoff.transform.SetParent(ext.transform, false);

        // Viaduct Roadway & Sidewalk
        CreateBox("Sidewalk_Curb", dropoff.transform, new Vector3(0, 0.15f, -44f), new Vector3(120f, 0.3f, 8f), mats.concretePillar);
        CreateBox("Dropoff_Roadway", dropoff.transform, new Vector3(0, 0.05f, -54f), new Vector3(120f, 0.1f, 12f), mats.tarmacAsphalt);

        // Automated Sliding Glass Doors (3 Portals: Doors 1, 2, 3)
        float[] doorPositions = new float[] { -30f, 0f, 30f };
        for (int i = 0; i < doorPositions.Length; i++)
        {
            float dx = doorPositions[i];
            GameObject portal = new GameObject($"Entrance_Portal_Gate_{i + 1}");
            portal.transform.SetParent(dropoff.transform, false);
            portal.transform.localPosition = new Vector3(dx, 0, -39.8f);

            // Door Frame
            CreateBox("Portal_Frame_Top", portal.transform, new Vector3(0, 3.8f, 0), new Vector3(6.5f, 0.4f, 0.6f), mats.darkSteel);
            CreateBox("Portal_Frame_L", portal.transform, new Vector3(-3.1f, 1.9f, 0), new Vector3(0.3f, 3.8f, 0.6f), mats.darkSteel);
            CreateBox("Portal_Frame_R", portal.transform, new Vector3(3.1f, 1.9f, 0), new Vector3(0.3f, 3.8f, 0.6f), mats.darkSteel);

            // Left & Right Glass Door Leaves with script
            GameObject leftLeaf = CreateBox("Door_Leaf_Left", portal.transform, new Vector3(-1.4f, 1.8f, 0), new Vector3(2.6f, 3.4f, 0.1f), mats.curtainGlass);
            GameObject rightLeaf = CreateBox("Door_Leaf_Right", portal.transform, new Vector3(1.4f, 1.8f, 0), new Vector3(2.6f, 3.4f, 0.1f), mats.curtainGlass);

            // Trigger Zone
            var triggerBox = portal.AddComponent<BoxCollider>();
            triggerBox.isTrigger = true;
            triggerBox.size = new Vector3(7f, 4f, 5f);
            triggerBox.center = new Vector3(0, 2f, 0);

            var doorScript = portal.AddComponent<AirportAutomaticDoor>();
            SetPrivateField(doorScript, "leftDoorLeaf", leftLeaf.transform);
            SetPrivateField(doorScript, "rightDoorLeaf", rightLeaf.transform);
            SetPrivateField(doorScript, "slideDistance", 2.2f);
        }

        // Entrance Overhang Canopy Signage
        GameObject signObj = CreateBox("Sign_Terminal_Name", dropoff.transform, new Vector3(0, 16.5f, -48.2f), new Vector3(45f, 2.5f, 0.5f), mats.darkSteel);
        CreateTextMesh(signObj.transform, "AEROSIM INTERNATIONAL AIRPORT • TERMINAL 1", new Vector3(0, 0, -0.3f), 8f, Color.white);
    }

    // ═══════════════════════════════════════════════════════════
    //  INTERIOR PASSENGER FLOW & ZONES
    // ═══════════════════════════════════════════════════════════

    private void BuildTerminalInterior(Transform parent, TerminalMaterials mats)
    {
        GameObject interior = new GameObject("02_Interior_Terminal");
        interior.transform.SetParent(parent, false);

        // ──────────────────────────────────────────
        // ZONE 1: CHECK-IN HALL (Z: -35 to -15)
        // ──────────────────────────────────────────
        GameObject checkinZone = new GameObject("Zone_1_CheckIn_Hall");
        checkinZone.transform.SetParent(interior.transform, false);

        // 4 Large Island Check-In Counters
        float[] checkInX = new float[] { -36f, -12f, 12f, 36f };
        for (int i = 0; i < checkInX.Length; i++)
        {
            float cx = checkInX[i];
            GameObject island = new GameObject($"CheckIn_Island_{i + 1}");
            island.transform.SetParent(checkinZone.transform, false);
            island.transform.localPosition = new Vector3(cx, 0, -26f);

            // Main Counter Base (Polished Wood & Composite)
            CreateBox("Counter_Desk", island.transform, new Vector3(0, 0.6f, 0), new Vector3(14f, 1.2f, 2.2f), mats.whiteComposite);
            CreateBox("Wood_Counter_Top", island.transform, new Vector3(0, 1.25f, 0), new Vector3(14.4f, 0.1f, 2.4f), mats.woodAccents);
            CreateBox("Cyan_Accent_Strip", island.transform, new Vector3(0, 0.15f, -1.15f), new Vector3(14f, 0.1f, 0.1f), mats.cyanNeon);

            // Staff Agent Computer Screens & Scales
            for (float deskX = -5f; deskX <= 5f; deskX += 2.5f)
            {
                CreateBox($"Agent_Monitor_{deskX}", island.transform, new Vector3(deskX, 1.6f, -0.2f), new Vector3(0.6f, 0.45f, 0.1f), mats.darkSteel);
                CreateBox($"Scale_Platform_{deskX}", island.transform, new Vector3(deskX + 0.8f, 0.2f, -1.8f), new Vector3(1.2f, 0.1f, 1.4f), mats.darkSteel);
            }

            // Luggage Intake Conveyor Belt behind counter
            GameObject belt = CreateBox("Luggage_Conveyor_Belt", island.transform, new Vector3(0, 0.35f, 1.8f), new Vector3(16f, 0.5f, 1.2f), mats.conveyorBelt);
            var convScript = belt.AddComponent<AirportLuggageConveyor>();
            SetPrivateField(convScript, "speed", 1.2f);
            SetPrivateField(convScript, "moveDirection", Vector3.right);

            // Overhead Island Number Sign
            GameObject islandSign = CreateBox("Overhead_Island_Sign", island.transform, new Vector3(0, 5.5f, 0), new Vector3(8f, 1.2f, 0.3f), mats.darkSteel);
            CreateTextMesh(islandSign.transform, $"CHECK-IN ISLAND 0{i + 1}", new Vector3(0, 0, -0.2f), 5f, Color.cyan);
        }

        // Central Giant Flight Information Display Board (FIDS)
        if (includeFIDS)
        {
            GameObject fidsRoot = new GameObject("Central_FIDS_Display_Board");
            fidsRoot.transform.SetParent(checkinZone.transform, false);
            fidsRoot.transform.localPosition = new Vector3(0, 7.5f, -14f);

            // Main Board Housing
            CreateBox("FIDS_Frame", fidsRoot.transform, Vector3.zero, new Vector3(22f, 8f, 0.6f), mats.darkSteel);
            CreateBox("FIDS_Neon_Border", fidsRoot.transform, new Vector3(0, 4.05f, 0), new Vector3(22.2f, 0.15f, 0.7f), mats.cyanNeon);

            // Live Text Component
            GameObject canvasGo = new GameObject("FIDS_Canvas");
            canvasGo.transform.SetParent(fidsRoot.transform, false);
            canvasGo.transform.localPosition = new Vector3(0, 0, -0.35f);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 360);
            canvasGo.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

            GameObject textObj = new GameObject("FIDS_Text");
            textObj.transform.SetParent(canvasGo.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.fontSize = 24;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(960, 340);
            tmp.rectTransform.anchoredPosition = new Vector2(0, 0);

            var fidsScript = fidsRoot.AddComponent<FIDSDisplay>();
            SetPrivateField(fidsScript, "boardText", tmp);
        }

        // Self-Service Check-In Kiosks
        GameObject kioskRow = new GameObject("Self_Service_Kiosks");
        kioskRow.transform.SetParent(checkinZone.transform, false);
        for (float kx = -24f; kx <= 24f; kx += 8f)
        {
            GameObject kiosk = new GameObject($"Kiosk_{kx}");
            kiosk.transform.SetParent(kioskRow.transform, false);
            kiosk.transform.localPosition = new Vector3(kx, 0, -34f);

            CreateBox("Kiosk_Pillar", kiosk.transform, new Vector3(0, 0.75f, 0), new Vector3(0.6f, 1.5f, 0.5f), mats.whiteComposite);
            CreateBox("Touchscreen", kiosk.transform, new Vector3(0, 1.35f, -0.2f), new Vector3(0.5f, 0.6f, 0.05f), mats.cyanNeon, Quaternion.Euler(-25f, 0, 0));
        }

        // ──────────────────────────────────────────
        // ZONE 2: SECURITY CHECKPOINT (Z: -5 to +5)
        // ──────────────────────────────────────────
        GameObject securityZone = new GameObject("Zone_2_Security_Checkpoint");
        securityZone.transform.SetParent(interior.transform, false);

        // Security Divider Glass Partition
        CreateBox("Security_Glass_Divider_L", securityZone.transform, new Vector3(-35f, 1.8f, -4f), new Vector3(45f, 3.6f, 0.2f), mats.curtainGlass);
        CreateBox("Security_Glass_Divider_R", securityZone.transform, new Vector3(35f, 1.8f, -4f), new Vector3(45f, 3.6f, 0.2f), mats.curtainGlass);

        // 3 Security Screening Lanes
        float[] secX = new float[] { -10f, 0f, 10f };
        for (int i = 0; i < secX.Length; i++)
        {
            float sx = secX[i];
            GameObject lane = new GameObject($"Security_Lane_{i + 1}");
            lane.transform.SetParent(securityZone.transform, false);
            lane.transform.localPosition = new Vector3(sx, 0, 0);

            // Metal Detector Arch
            GameObject arch = new GameObject("Metal_Detector_Arch");
            arch.transform.SetParent(lane.transform, false);
            arch.transform.localPosition = new Vector3(-2.5f, 0, 0);

            CreateBox("Arch_L", arch.transform, new Vector3(-0.6f, 1.25f, 0), new Vector3(0.18f, 2.5f, 0.6f), mats.darkSteel);
            CreateBox("Arch_R", arch.transform, new Vector3(0.6f, 1.25f, 0), new Vector3(0.18f, 2.5f, 0.6f), mats.darkSteel);
            CreateBox("Arch_Top", arch.transform, new Vector3(0, 2.4f, 0), new Vector3(1.38f, 0.25f, 0.6f), mats.darkSteel);
            CreateBox("Arch_Indicator_LED", arch.transform, new Vector3(0, 2.4f, -0.32f), new Vector3(0.8f, 0.08f, 0.05f), mats.cyanNeon);

            // X-Ray Baggage Tunnel Scanner
            GameObject xray = new GameObject("XRay_Scanner");
            xray.transform.SetParent(lane.transform, false);
            xray.transform.localPosition = new Vector3(1.5f, 0, 0);

            CreateBox("XRay_Body", xray.transform, new Vector3(0, 1.1f, 0), new Vector3(1.4f, 1.5f, 2.4f), mats.whiteComposite);
            CreateBox("XRay_Tunnel_Opening", xray.transform, new Vector3(0, 1.0f, 0), new Vector3(1.1f, 0.9f, 2.45f), mats.darkSteel);
            CreateBox("Conveyor_Entry_Table", xray.transform, new Vector3(0, 0.65f, -2.2f), new Vector3(1.0f, 0.1f, 2.0f), mats.conveyorBelt);
            CreateBox("Conveyor_Exit_Table", xray.transform, new Vector3(0, 0.65f, 2.2f), new Vector3(1.0f, 0.1f, 2.0f), mats.conveyorBelt);
            CreateBox("Operator_Monitor", xray.transform, new Vector3(-0.9f, 1.6f, 0), new Vector3(0.5f, 0.4f, 0.1f), mats.darkSteel, Quaternion.Euler(0, 45f, 0));
        }

        // ──────────────────────────────────────────
        // ZONE 3: PASSPORT e-GATES (Z: 10 to 20)
        // ──────────────────────────────────────────
        GameObject passportZone = new GameObject("Zone_3_Passport_eGates");
        passportZone.transform.SetParent(interior.transform, false);

        for (float px = -15f; px <= 15f; px += 5f)
        {
            GameObject egate = new GameObject($"eGate_{px}");
            egate.transform.SetParent(passportZone.transform, false);
            egate.transform.localPosition = new Vector3(px, 0, 15f);

            CreateBox("eGate_Pillar_L", egate.transform, new Vector3(-1f, 0.6f, 0), new Vector3(0.25f, 1.2f, 2.2f), mats.whiteComposite);
            CreateBox("eGate_Pillar_R", egate.transform, new Vector3(1f, 0.6f, 0), new Vector3(0.25f, 1.2f, 2.2f), mats.whiteComposite);
            CreateBox("Glass_Flap_L", egate.transform, new Vector3(-0.4f, 0.65f, 0), new Vector3(0.8f, 0.8f, 0.05f), mats.cyanNeon);
            CreateBox("Glass_Flap_R", egate.transform, new Vector3(0.4f, 0.65f, 0), new Vector3(0.8f, 0.8f, 0.05f), mats.cyanNeon);
            CreateBox("Passport_Scanner_Glass", egate.transform, new Vector3(1f, 1.05f, -0.6f), new Vector3(0.2f, 0.1f, 0.3f), mats.cyanNeon);
        }

        // ──────────────────────────────────────────
        // ZONE 4: DEPARTURE LOUNGE & GATES (Z: 22 to 38)
        // ──────────────────────────────────────────
        GameObject loungeZone = new GameObject("Zone_4_Departure_Lounge");
        loungeZone.transform.SetParent(interior.transform, false);

        // Ergonomic Airport Beam Seating (Tandem rows)
        for (float lx = -45f; lx <= 45f; lx += 18f)
        {
            for (float lz = 24f; lz <= 32f; lz += 4f)
            {
                CreateAirportSeatRow($"Seats_{lx}_{lz}", loungeZone.transform, new Vector3(lx, 0, lz), mats.darkSteel, mats.whiteComposite);
            }
        }

        // Duty Free & Cafe Storefront Boutiques
        GameObject retail = new GameObject("Retail_Boutiques");
        retail.transform.SetParent(loungeZone.transform, false);

        // Duty Free Shop (West Side)
        GameObject dutyFree = CreateBox("Duty_Free_Shopfront", retail.transform, new Vector3(-48f, 3f, 28f), new Vector3(18f, 6f, 14f), mats.darkSteel);
        CreateBox("DutyFree_Glass_Front", dutyFree.transform, new Vector3(0, 0, -6.9f), new Vector3(17f, 5.5f, 0.2f), mats.curtainGlass);
        CreateTextMesh(dutyFree.transform, "DUTY FREE LUXURY", new Vector3(0, 2.2f, -7.1f), 4f, new Color(1f, 0.85f, 0.2f));

        // Sky Lounge Cafe (East Side)
        GameObject cafe = CreateBox("Sky_Lounge_Cafe", retail.transform, new Vector3(48f, 3f, 28f), new Vector3(18f, 6f, 14f), mats.woodAccents);
        CreateBox("Cafe_Glass_Front", cafe.transform, new Vector3(0, 0, -6.9f), new Vector3(17f, 5.5f, 0.2f), mats.curtainGlass);
        CreateTextMesh(cafe.transform, "ESPRESSO SKY LOUNGE", new Vector3(0, 2.2f, -7.1f), 4f, Color.white);

        // Overhead Directional Navigational Signage
        GameObject navSign1 = CreateBox("Nav_Sign_Gates", loungeZone.transform, new Vector3(0, 9f, 22f), new Vector3(16f, 1.5f, 0.3f), mats.darkSteel);
        CreateTextMesh(navSign1.transform, "GATES A01 - A12 ➔   |   VIP LOUNGE ⬆   |   RESTROOMS 🚻", new Vector3(0, 0, -0.2f), 4.2f, Color.yellow);
    }

    private void CreateAirportSeatRow(string name, Transform parent, Vector3 localPos, Material frameMat, Material seatMat)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.transform.localPosition = localPos;

        // Long Beam Bar
        CreateBox("Beam_Bar", row.transform, new Vector3(0, 0.35f, 0), new Vector3(7.5f, 0.1f, 0.1f), frameMat);
        CreateBox("Leg_L", row.transform, new Vector3(-3.2f, 0.18f, 0), new Vector3(0.1f, 0.36f, 0.6f), frameMat);
        CreateBox("Leg_R", row.transform, new Vector3(3.2f, 0.18f, 0), new Vector3(0.1f, 0.36f, 0.6f), frameMat);

        // 5 Individual Ergonomic Seats
        for (float sx = -3f; sx <= 3f; sx += 1.5f)
        {
            CreateBox($"Seat_Base_{sx}", row.transform, new Vector3(sx, 0.45f, 0), new Vector3(1.1f, 0.08f, 0.9f), seatMat);
            CreateBox($"Seat_Back_{sx}", row.transform, new Vector3(sx, 0.85f, 0.42f), new Vector3(1.1f, 0.75f, 0.08f), seatMat, Quaternion.Euler(-10f, 0, 0));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  AIRSIDE JETWAY GATES & BOARDING BRIDGES
    // ═══════════════════════════════════════════════════════════

    private void BuildJetwaysAndGates(Transform parent, TerminalMaterials mats)
    {
        GameObject gatesRoot = new GameObject("03_Airside_Jetways_Gates");
        gatesRoot.transform.SetParent(parent, false);

        float[] gateX = new float[] { -38f, 0f, 38f };
        string[] gateNames = new string[] { "GATE A01", "GATE A02", "GATE A03" };

        for (int i = 0; i < gateX.Length; i++)
        {
            float gx = gateX[i];
            string gName = gateNames[i];

            GameObject gate = new GameObject($"Boarding_Gate_{i + 1}");
            gate.transform.SetParent(gatesRoot.transform, false);
            gate.transform.localPosition = new Vector3(gx, 0, 39.8f);

            // Gate Desk Inside Terminal
            CreateBox("Gate_Desk", gate.transform, new Vector3(0, 0.6f, -3f), new Vector3(4f, 1.2f, 1.2f), mats.whiteComposite);
            GameObject gateDisplay = CreateBox("Gate_Screen", gate.transform, new Vector3(0, 3.5f, -3f), new Vector3(3f, 1.2f, 0.2f), mats.darkSteel);
            CreateTextMesh(gateDisplay.transform, $"{gName}\n<color=#00FFCC>BOARDING</color>", new Vector3(0, 0, -0.15f), 3.5f, Color.white);

            // Telescopic Boarding Bridge (Jetway) Extrusion to Tarmac
            GameObject jetway = new GameObject("Telescopic_Jetway");
            jetway.transform.SetParent(gate.transform, false);

            // Fixed Rotunda
            CreateCylinder("Rotunda_Pivot", jetway.transform, new Vector3(0, 6f, 3f), new Vector3(4f, 3f, 4f), mats.whiteComposite);

            // Main Telescopic Tunnel extending towards aircraft
            CreateBox("Jetway_Tunnel_Segment_1", jetway.transform, new Vector3(0, 5.5f, 14f), new Vector3(3.2f, 3.2f, 20f), mats.whiteComposite);
            CreateBox("Jetway_Glass_Ribbon_L", jetway.transform, new Vector3(-1.62f, 5.5f, 14f), new Vector3(0.08f, 1.6f, 18f), mats.curtainGlass);
            CreateBox("Jetway_Glass_Ribbon_R", jetway.transform, new Vector3(1.62f, 5.5f, 14f), new Vector3(0.08f, 1.6f, 18f), mats.curtainGlass);

            // Aircraft Docking Cab / Accordion Bellows
            CreateBox("Aircraft_Docking_Cab", jetway.transform, new Vector3(0, 5.0f, 25f), new Vector3(3.6f, 3.4f, 3f), mats.darkSteel);
            CreateBox("Rubber_Accordion_Bellows", jetway.transform, new Vector3(0, 5.0f, 26.8f), new Vector3(3.8f, 3.6f, 0.8f), mats.conveyorBelt);

            // Vertical Elevation Support Pillars
            CreateCylinder("Support_Pillar_1", jetway.transform, new Vector3(-1.8f, 2.5f, 20f), new Vector3(0.4f, 2.5f, 0.4f), mats.darkSteel);
            CreateCylinder("Support_Pillar_2", jetway.transform, new Vector3(1.8f, 2.5f, 20f), new Vector3(0.4f, 2.5f, 0.4f), mats.darkSteel);
            CreateCylinder("Wheel_Bogie", jetway.transform, new Vector3(0, 0.6f, 20f), new Vector3(4.5f, 0.6f, 0.6f), mats.darkSteel);

            // Visual Docking Guidance System (VDGS)
            GameObject vdgs = CreateBox("VDGS_Docking_Guidance", gate.transform, new Vector3(0, 10f, 3f), new Vector3(2.5f, 2.5f, 0.4f), mats.darkSteel);
            CreateTextMesh(vdgs.transform, "STOP\n<color=yellow>B737</color>", new Vector3(0, 0, 0.25f), 4f, Color.red);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  ROOFTOP SYSTEMS & AVIONICS
    // ═══════════════════════════════════════════════════════════

    private void BuildRooftopSystems(Transform parent, TerminalMaterials mats)
    {
        GameObject roofSystems = new GameObject("04_Rooftop_HVAC_Avionics");
        roofSystems.transform.SetParent(parent, false);

        // Industrial HVAC Chiller Units
        for (float hx = -35f; hx <= 35f; hx += 22f)
        {
            GameObject hvac = new GameObject($"HVAC_Unit_{hx}");
            hvac.transform.SetParent(roofSystems.transform, false);
            hvac.transform.localPosition = new Vector3(hx, 19f, -18f);

            CreateBox("Chiller_Housing", hvac.transform, new Vector3(0, 1.5f, 0), new Vector3(6f, 3f, 8f), mats.darkSteel);
            CreateCylinder("Exhaust_Fan_1", hvac.transform, new Vector3(-1.5f, 3.1f, 0), new Vector3(2.2f, 0.2f, 2.2f), mats.whiteComposite);
            CreateCylinder("Exhaust_Fan_2", hvac.transform, new Vector3(1.5f, 3.1f, 0), new Vector3(2.2f, 0.2f, 2.2f), mats.whiteComposite);
        }

        // Airport Radar Dome & Communication Mast
        GameObject radar = new GameObject("Airport_Surface_Radar");
        radar.transform.SetParent(roofSystems.transform, false);
        radar.transform.localPosition = new Vector3(45f, 19f, 20f);

        CreateCylinder("Radar_Tower_Base", radar.transform, new Vector3(0, 3f, 0), new Vector3(2.5f, 3f, 2.5f), mats.whiteComposite);
        CreateSphere("Radar_Radome", radar.transform, new Vector3(0, 7f, 0), new Vector3(4f, 4f, 4f), mats.whiteComposite);

        // Aircraft Warning Beacon Masts
        float[] beaconX = new float[] { -55f, 55f };
        foreach (float bx in beaconX)
        {
            GameObject beacon = new GameObject($"Avionics_Beacon_{bx}");
            beacon.transform.SetParent(roofSystems.transform, false);
            beacon.transform.localPosition = new Vector3(bx, 19f, 0);

            CreateCylinder("Mast", beacon.transform, new Vector3(0, 4f, 0), new Vector3(0.3f, 4f, 0.3f), mats.darkSteel);
            CreateSphere("Pulsing_Red_Beacon", beacon.transform, new Vector3(0, 8.2f, 0), new Vector3(0.8f, 0.8f, 0.8f), mats.redBeacon);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  INTERIOR & APRON LIGHTING
    // ═══════════════════════════════════════════════════════════

    private void BuildTerminalLighting(Transform parent, TerminalMaterials mats)
    {
        GameObject lighting = new GameObject("05_Lighting_System");
        lighting.transform.SetParent(parent, false);

        // Ceiling Recessed Downlights Grid
        for (float lx = -45f; lx <= 45f; lx += 30f)
        {
            for (float lz = -25f; lz <= 25f; lz += 25f)
            {
                GameObject lightGo = new GameObject($"Ceiling_Light_{lx}_{lz}");
                lightGo.transform.SetParent(lighting.transform, false);
                lightGo.transform.localPosition = new Vector3(lx, 16f, lz);

                Light pt = lightGo.AddComponent<Light>();
                pt.type = LightType.Point;
                pt.color = new Color(0.95f, 0.98f, 1.0f);
                pt.intensity = 2.5f;
                pt.range = 35f;
            }
        }

        // Apron Floodlight Towers
        float[] floodX = new float[] { -40f, 40f };
        foreach (float fx in floodX)
        {
            GameObject flood = new GameObject($"Apron_Floodlight_{fx}");
            flood.transform.SetParent(lighting.transform, false);
            flood.transform.localPosition = new Vector3(fx, 17f, 42f);

            Light fl = flood.AddComponent<Light>();
            fl.type = LightType.Spot;
            fl.color = new Color(1.0f, 0.95f, 0.85f);
            fl.intensity = 4.0f;
            fl.range = 65f;
            fl.spotAngle = 75f;
            flood.transform.localRotation = Quaternion.Euler(45f, 0, 0);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PRIMITIVE & HELPER BUILDERS
    // ═══════════════════════════════════════════════════════════

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, Quaternion? rot = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        if (rot.HasValue) go.transform.localRotation = rot.Value;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            mr.receiveShadows = true;
        }

        return go;
    }

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            mr.receiveShadows = true;
        }

        return go;
    }

    private static GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            mr.receiveShadows = true;
        }

        return go;
    }

    private static void CreateTextMesh(Transform parent, string text, Vector3 localPos, float fontSize, Color color)
    {
        GameObject canvasGo = new GameObject("Text_Canvas");
        canvasGo.transform.SetParent(parent, false);
        canvasGo.transform.localPosition = localPos;

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 80);
        canvasGo.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize * 10f;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(400, 80);
        tmp.rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (field != null) field.SetValue(target, value);
    }
}
