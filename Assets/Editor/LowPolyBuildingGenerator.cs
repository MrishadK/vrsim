using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class LowPolyBuildingGenerator : EditorWindow
{
    private const string PREFAB_PATH = "Assets/Prefabs/LowPolyBuildings";
    private const string MESH_PATH = "Assets/Models/LowPolyBuildings";
    private const string MAT_PATH = "Assets/GeneratedMaterials/LowPoly";
    private const string TEX_PATH = "Assets/GeneratedTextures/LowPoly";

    static LowPolyBuildingGenerator()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Directory.Exists(PREFAB_PATH) || Directory.GetFiles(PREFAB_PATH, "*.prefab").Length == 0)
            {
                GenerateAllBuildingAssets();
            }
        };
    }

    // Placement Settings
    private Vector3 skylineCenter = new Vector3(0f, 0f, 250f);
    private float skylineWidth = 400f;
    private float skylineDepth = 150f;
    private int buildingCount = 28;
    private float minScale = 0.8f;
    private float maxScale = 2.4f;
    private float groundY = 0f;
    private bool arrangeInArc = true;
    private float arcRadius = 260f;

    [MenuItem("Tools/Low-Poly Skyline & Buildings Generator", false, 10)]
    public static void ShowWindow()
    {
        var window = GetWindow<LowPolyBuildingGenerator>("Low-Poly Buildings");
        window.minSize = new Vector2(420, 520);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        GUILayout.Label("Low-Poly Realistic Building Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Generate ultra-optimized, aesthetic low-poly buildings (20-120 triangles each) and place them as a scenic distant skyline backdrop behind the airport & playground.", MessageType.Info);
        
        EditorGUILayout.Space(8);
        GUILayout.Label("1. Generate Prefabs & Assets", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate Building Prefabs & Materials", GUILayout.Height(36)))
        {
            GenerateAllBuildingAssets();
        }

        EditorGUILayout.Space(12);
        GUILayout.Label("2. Skyline Placement Settings", EditorStyles.boldLabel);
        skylineCenter = EditorGUILayout.Vector3Field("Center Position", skylineCenter);
        groundY = EditorGUILayout.FloatField("Ground Y Level", groundY);
        buildingCount = EditorGUILayout.IntSlider("Number of Buildings", buildingCount, 5, 80);
        minScale = EditorGUILayout.Slider("Min Height Scale", minScale, 0.5f, 2.0f);
        maxScale = EditorGUILayout.Slider("Max Height Scale", maxScale, 1.0f, 4.0f);
        
        arrangeInArc = EditorGUILayout.Toggle("Arrange in Horizon Arc", arrangeInArc);
        if (arrangeInArc)
        {
            arcRadius = EditorGUILayout.FloatField("Arc Distance / Radius", arcRadius);
        }
        else
        {
            skylineWidth = EditorGUILayout.FloatField("Skyline Width (X)", skylineWidth);
            skylineDepth = EditorGUILayout.FloatField("Skyline Depth (Z)", skylineDepth);
        }

        EditorGUILayout.Space(12);
        GUILayout.Label("3. Populate Scene", EditorStyles.boldLabel);
        if (GUILayout.Button("Place Background Skyline in Scene", GUILayout.Height(40)))
        {
            PlaceSkylineInScene();
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Clear Existing Background Skyline", GUILayout.Height(26)))
        {
            ClearExistingSkyline();
        }
    }

    [MenuItem("Tools/Quick Place Background Skyline")]
    public static void QuickPlace()
    {
        GenerateAllBuildingAssets();
        var window = CreateInstance<LowPolyBuildingGenerator>();
        window.PlaceSkylineInScene();
    }

    public static void GenerateAllBuildingAssets()
    {
        EnsureDirectories();

        // 1. Generate Textures & Materials
        Material glassMat = GetOrCreateGlassMaterial();
        Material concreteMat = GetOrCreateConcreteMaterial();
        Material steelMat = GetOrCreateDarkSteelMaterial();
        Material roofMat = GetOrCreateRoofMaterial();
        Material beaconMat = GetOrCreateBeaconMaterial();
        Material hangarMat = GetOrCreateHangarMaterial();

        // 2. Generate Building Meshes & Prefabs
        CreateBuildingPrefab_ModernTower(glassMat, concreteMat, steelMat, beaconMat);
        CreateBuildingPrefab_SteppedTower(concreteMat, glassMat, steelMat, beaconMat);
        CreateBuildingPrefab_SkylineBlock(concreteMat, glassMat, roofMat);
        CreateBuildingPrefab_AviationHangar(hangarMat, concreteMat, glassMat);
        CreateBuildingPrefab_HotelPlaza(glassMat, concreteMat, steelMat);
        CreateBuildingPrefab_ControlTower(concreteMat, glassMat, steelMat, beaconMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[LowPolyBuildingGenerator]</color> 6 Ultra Low-Poly Building Prefabs & Materials created successfully!");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists(PREFAB_PATH)) Directory.CreateDirectory(PREFAB_PATH);
        if (!Directory.Exists(MESH_PATH)) Directory.CreateDirectory(MESH_PATH);
        if (!Directory.Exists(MAT_PATH)) Directory.CreateDirectory(MAT_PATH);
        if (!Directory.Exists(TEX_PATH)) Directory.CreateDirectory(TEX_PATH);
    }

    // ── MATERIAL & TEXTURE GENERATION ──

    private static Material GetOrCreateGlassMaterial()
    {
        string path = $"{MAT_PATH}/Mat_LP_ModernGlass.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "Mat_LP_ModernGlass";
            mat.SetColor("_BaseColor", new Color(0.18f, 0.35f, 0.48f, 1f));
            mat.SetFloat("_Metallic", 0.85f);
            mat.SetFloat("_Smoothness", 0.92f);
            
            // Subtle night-window emission
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.15f, 0.28f, 0.40f) * 0.4f);
            
            Texture2D tex = CreateProceduralWindowTexture(new Color(0.18f, 0.35f, 0.48f), new Color(0.90f, 0.95f, 1.0f));
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Material GetOrCreateConcreteMaterial()
    {
        string path = $"{MAT_PATH}/Mat_LP_ConcreteFacade.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "Mat_LP_ConcreteFacade";
            mat.SetColor("_BaseColor", new Color(0.72f, 0.73f, 0.75f, 1f));
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Material GetOrCreateDarkSteelMaterial()
    {
        string path = $"{MAT_PATH}/Mat_LP_DarkSteel.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "Mat_LP_DarkSteel";
            mat.SetColor("_BaseColor", new Color(0.16f, 0.18f, 0.22f, 1f));
            mat.SetFloat("_Metallic", 0.70f);
            mat.SetFloat("_Smoothness", 0.60f);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Material GetOrCreateRoofMaterial()
    {
        string path = $"{MAT_PATH}/Mat_LP_RoofTar.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "Mat_LP_RoofTar";
            mat.SetColor("_BaseColor", new Color(0.24f, 0.25f, 0.27f, 1f));
            mat.SetFloat("_Metallic", 0.10f);
            mat.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Material GetOrCreateBeaconMaterial()
    {
        string path = $"{MAT_PATH}/Mat_LP_RedBeacon.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "Mat_LP_RedBeacon";
            mat.SetColor("_BaseColor", new Color(1f, 0.1f, 0.1f, 1f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(2.5f, 0.1f, 0.1f, 1f));
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Material GetOrCreateHangarMaterial()
    {
        string path = $"{MAT_PATH}/Mat_LP_HangarMetal.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.name = "Mat_LP_HangarMetal";
            mat.SetColor("_BaseColor", new Color(0.80f, 0.82f, 0.85f, 1f));
            mat.SetFloat("_Metallic", 0.50f);
            mat.SetFloat("_Smoothness", 0.40f);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static Texture2D CreateProceduralWindowTexture(Color wallColor, Color windowColor)
    {
        string path = $"{TEX_PATH}/Tex_Facade_Windows.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];

        int cell = 16;
        int winSize = 10;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int modX = x % cell;
                int modY = y % cell;

                if (modX >= 3 && modX < 3 + winSize && modY >= 3 && modY < 3 + winSize)
                {
                    // Window pixel with subtle gradient
                    float glow = 0.85f + 0.15f * Mathf.Sin(x * 0.2f + y * 0.1f);
                    pixels[y * size + x] = windowColor * glow;
                }
                else
                {
                    // Wall / mullion frame
                    pixels[y * size + x] = wallColor;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ── PREFAB 1: MODERN GLASS TOWER ──
    private static void CreateBuildingPrefab_ModernTower(Material glass, Material concrete, Material steel, Material beacon)
    {
        string prefabPath = $"{PREFAB_PATH}/LP_ModernTower_01.prefab";
        GameObject root = new GameObject("LP_ModernTower_01");

        // Main glass body
        GameObject body = CreateBoxPart("TowerBody", root.transform, new Vector3(0, 25f, 0), new Vector3(14f, 50f, 14f), glass);
        
        // Crown / Rooftop mechanical room
        GameObject crown = CreateBoxPart("RoofCrown", root.transform, new Vector3(0, 52.5f, 0), new Vector3(10f, 5f, 10f), concrete);

        // Antenna Spire
        GameObject antenna = CreateBoxPart("Antenna", root.transform, new Vector3(0, 58f, 0), new Vector3(0.5f, 6f, 0.5f), steel);
        
        // Top Red Beacon
        CreateBoxPart("Beacon", antenna.transform, new Vector3(0, 3.2f, 0), new Vector3(0.7f, 0.7f, 0.7f), beacon);

        SaveAndCleanPrefab(root, prefabPath);
    }

    // ── PREFAB 2: STEPPED CORPORATE TOWER ──
    private static void CreateBuildingPrefab_SteppedTower(Material concrete, Material glass, Material steel, Material beacon)
    {
        string prefabPath = $"{PREFAB_PATH}/LP_SteppedTower_02.prefab";
        GameObject root = new GameObject("LP_SteppedTower_02");

        // Tier 1 Base
        CreateBoxPart("Tier1_Base", root.transform, new Vector3(0, 15f, 0), new Vector3(18f, 30f, 18f), glass);
        
        // Tier 2 Mid
        CreateBoxPart("Tier2_Mid", root.transform, new Vector3(0, 37.5f, 0), new Vector3(13f, 15f, 13f), concrete);

        // Tier 3 Top
        CreateBoxPart("Tier3_Top", root.transform, new Vector3(0, 48.5f, 0), new Vector3(8f, 7f, 8f), glass);

        // Rooftop Antenna
        GameObject ant = CreateBoxPart("Spire", root.transform, new Vector3(0, 55f, 0), new Vector3(0.4f, 6f, 0.4f), steel);
        CreateBoxPart("Beacon", ant.transform, new Vector3(0, 3.2f, 0), new Vector3(0.6f, 0.6f, 0.6f), beacon);

        SaveAndCleanPrefab(root, prefabPath);
    }

    // ── PREFAB 3: WIDE SKYLINE BLOCK ──
    private static void CreateBuildingPrefab_SkylineBlock(Material concrete, Material glass, Material roof)
    {
        string prefabPath = $"{PREFAB_PATH}/LP_SkylineBlock_03.prefab";
        GameObject root = new GameObject("LP_SkylineBlock_03");

        // Main Wide Block
        CreateBoxPart("MainBlock", root.transform, new Vector3(0, 16f, 0), new Vector3(28f, 32f, 16f), glass);

        // Concrete corner columns
        CreateBoxPart("Pillar_L", root.transform, new Vector3(-13.5f, 16f, 0), new Vector3(1.5f, 32.2f, 16.2f), concrete);
        CreateBoxPart("Pillar_R", root.transform, new Vector3(13.5f, 16f, 0), new Vector3(1.5f, 32.2f, 16.2f), concrete);

        // Rooftop HVAC units
        CreateBoxPart("HVAC_1", root.transform, new Vector3(-6f, 33.5f, 0), new Vector3(5f, 3f, 6f), roof);
        CreateBoxPart("HVAC_2", root.transform, new Vector3(6f, 33f, 2f), new Vector3(4f, 2f, 4f), roof);

        SaveAndCleanPrefab(root, prefabPath);
    }

    // ── PREFAB 4: AIRPORT LOGISTICS HANGAR ──
    private static void CreateBuildingPrefab_AviationHangar(Material hangarMetal, Material concrete, Material glass)
    {
        string prefabPath = $"{PREFAB_PATH}/LP_AviationHangar_04.prefab";
        GameObject root = new GameObject("LP_AviationHangar_04");

        // Main Hangar Hall
        CreateBoxPart("HangarHall", root.transform, new Vector3(0, 9f, 0), new Vector3(36f, 18f, 24f), hangarMetal);

        // Office wing
        CreateBoxPart("OfficeWing", root.transform, new Vector3(21f, 5f, 0), new Vector3(6f, 10f, 18f), concrete);

        // Front Gate / Door Frame
        CreateBoxPart("DoorFrame", root.transform, new Vector3(0, 7f, -12.1f), new Vector3(26f, 14f, 0.4f), hangarMetal);

        SaveAndCleanPrefab(root, prefabPath);
    }

    // ── PREFAB 5: MODERN HOTEL PLAZA ──
    private static void CreateBuildingPrefab_HotelPlaza(Material glass, Material concrete, Material steel)
    {
        string prefabPath = $"{PREFAB_PATH}/LP_HotelPlaza_05.prefab";
        GameObject root = new GameObject("LP_HotelPlaza_05");

        // Curved/Sleek Tall Body
        CreateBoxPart("MainTower", root.transform, new Vector3(0, 22f, 0), new Vector3(22f, 44f, 12f), glass);

        // Canopy Entrance
        CreateBoxPart("Canopy", root.transform, new Vector3(0, 2f, -7f), new Vector3(14f, 0.5f, 6f), steel);

        // Rooftop Skybar / Penthouse
        CreateBoxPart("Skybar", root.transform, new Vector3(0, 46f, 0), new Vector3(18f, 4f, 8f), concrete);

        SaveAndCleanPrefab(root, prefabPath);
    }

    // ── PREFAB 6: AIRPORT CONTROL TOWER ──
    private static void CreateBuildingPrefab_ControlTower(Material concrete, Material glass, Material steel, Material beacon)
    {
        string prefabPath = $"{PREFAB_PATH}/LP_ControlTower_06.prefab";
        GameObject root = new GameObject("LP_ControlTower_06");

        // Slender Concrete Shaft
        CreateBoxPart("Shaft", root.transform, new Vector3(0, 22f, 0), new Vector3(5f, 44f, 5f), concrete);

        // Cab Base
        CreateBoxPart("CabBase", root.transform, new Vector3(0, 46f, 0), new Vector3(10f, 4f, 10f), concrete);

        // Glass Observation Cab
        CreateBoxPart("GlassCab", root.transform, new Vector3(0, 50f, 0), new Vector3(12f, 4f, 12f), glass);

        // Roof Radar Dome / Antenna
        CreateBoxPart("RadarDome", root.transform, new Vector3(0, 53.5f, 0), new Vector3(4f, 3f, 4f), steel);
        GameObject ant = CreateBoxPart("RadarAntenna", root.transform, new Vector3(0, 57f, 0), new Vector3(0.3f, 4f, 0.3f), steel);
        CreateBoxPart("Beacon", ant.transform, new Vector3(0, 2.2f, 0), new Vector3(0.6f, 0.6f, 0.6f), beacon);

        SaveAndCleanPrefab(root, prefabPath);
    }

    private static GameObject CreateBoxPart(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        // Remove collider for optimal VR rendering performance on distant objects
        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            mr.receiveShadows = true;
        }

        return go;
    }

    private static void SaveAndCleanPrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        DestroyImmediate(root);
    }

    // ── SCENE SKYLINE POPULATION ──

    public void PlaceSkylineInScene()
    {
        ClearExistingSkyline();

        string[] prefabGuids = AssetDatabase.FindAssets("LP_ t:Prefab", new[] { PREFAB_PATH });
        if (prefabGuids == null || prefabGuids.Length == 0)
        {
            GenerateAllBuildingAssets();
            prefabGuids = AssetDatabase.FindAssets("LP_ t:Prefab", new[] { PREFAB_PATH });
        }

        List<GameObject> prefabs = new List<GameObject>();
        foreach (var guid in prefabGuids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (prefab != null) prefabs.Add(prefab);
        }

        if (prefabs.Count == 0)
        {
            Debug.LogError("[LowPolyBuildingGenerator] No building prefabs found in " + PREFAB_PATH);
            return;
        }

        GameObject skylineRoot = new GameObject("Background_Skyline_Horizon");
        skylineRoot.transform.position = skylineCenter;

        Random.InitState(42);

        for (int i = 0; i < buildingCount; i++)
        {
            GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab, skylineRoot.transform);

            Vector3 pos;
            Quaternion rot;

            if (arrangeInArc)
            {
                // Disperse in a wide panorama arc
                float angleSpan = 140f;
                float angle = -angleSpan / 2f + (angleSpan / (buildingCount - 1)) * i;
                angle += Random.Range(-2.5f, 2.5f);

                float rad = angle * Mathf.Deg2Rad;
                float dist = arcRadius + Random.Range(-30f, 35f);

                pos = new Vector3(Mathf.Sin(rad) * dist, groundY, Mathf.Cos(rad) * dist);
                // Face roughly towards the center/airport
                rot = Quaternion.Euler(0f, angle + 180f + Random.Range(-15f, 15f), 0f);
            }
            else
            {
                // Rectangular strip
                float x = -skylineWidth / 2f + (skylineWidth / (buildingCount - 1)) * i + Random.Range(-10f, 10f);
                float z = Random.Range(-skylineDepth / 2f, skylineDepth / 2f);
                pos = new Vector3(x, groundY, z);
                rot = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
            }

            instance.transform.localPosition = pos;
            instance.transform.localRotation = rot;

            // Height and scale variations
            float scaleY = Random.Range(minScale, maxScale);
            float scaleXZ = Random.Range(0.85f, 1.25f);
            instance.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

            // Mark as static for batching
            GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = skylineRoot;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log($"<color=cyan>[LowPolyBuildingGenerator]</color> Successfully placed {buildingCount} aesthetic low-poly buildings in 'Background_Skyline_Horizon'!");
    }

    public static void ClearExistingSkyline()
    {
        var existing = GameObject.Find("Background_Skyline_Horizon");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("[LowPolyBuildingGenerator] Removed existing 'Background_Skyline_Horizon'.");
        }
    }
}
