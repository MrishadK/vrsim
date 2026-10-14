using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class SetupPhotorealisticBoeing737
{
    private const string LIVERY_PATH = "Assets/Boeing737_Livery.png";
    private const string SPINNER_SPIRAL_PATH = "Assets/textures/Engine_Spinner_Spiral.png";
    private const string FAN_FBX_PATH = "Assets/uploads_files_3639185_A320_Engine_Fan.fbx";

    [MenuItem("Tools/Upgrade Boeing 737 to Photorealistic Quality", false, 10)]
    public static void ExecuteUpgrade()
    {
        var plane = GameObject.Find("boeing 737-800");
        if (plane == null)
        {
            Debug.LogError("[Boeing737Realism] 'boeing 737-800' GameObject not found in the current scene!");
            return;
        }

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (urpShader == null)
        {
            Debug.LogError("[Boeing737Realism] Universal Render Pipeline/Lit shader not found.");
            return;
        }

        // Ensure directories exist
        if (!Directory.Exists("Assets/textures")) Directory.CreateDirectory("Assets/textures");

        // 1. Load Original Livery and Generate Spinner Textures
        GenerateSpinnerSpiralTexture();
        AssetDatabase.Refresh();

        Texture2D liveryTex = AssetDatabase.LoadAssetAtPath<Texture2D>(LIVERY_PATH);
        Texture2D spinnerTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SPINNER_SPIRAL_PATH);

        // 2. Overhaul All Aircraft Materials with Calibrated Aerospace PBR
        ApplyPhotorealisticMaterials(plane, urpShader, liveryTex);

        // 3. Install 3D Engine Turbines with Spinner Spirals
        InstallEngineTurbines(plane, urpShader, spinnerTex);

        // 4. Install Full FAA/ICAO Aviation Lighting Rig
        InstallAviationLightingRig(plane, urpShader);

        // 5. Mark Scene Dirty & Save
        EditorUtility.SetDirty(plane);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#00FF99>[Boeing737Realism] Boeing 737-800 successfully upgraded to Photorealistic Quality!</color>");
    }

    private static void ApplyPhotorealisticMaterials(GameObject plane, Shader urpShader, Texture2D liveryTex)
    {
        var allRenderers = plane.GetComponentsInChildren<Renderer>(true);
        foreach (var r in allRenderers)
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].shader != urpShader)
                {
                    mats[i].shader = urpShader;
                    changed = true;
                }
            }
            if (changed) r.sharedMaterials = mats;
        }

        var body = GameObject.Find("Boeing 737") ?? plane.transform.Find("Boeing 737")?.gameObject;
        if (body != null)
        {
            var mr = body.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                foreach (var mat in mr.sharedMaterials)
                {
                    if (mat == null) continue;
                    mat.shader = urpShader;

                    // Clear unneeded keywords
                    if (mat.IsKeywordEnabled("_EMISSION")) mat.DisableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);

                    switch (mat.name)
                    {
                        case "Glass_mat 1":
                            // DIELECTRIC COMPOSITE RADOME (Nose Cone)
                            mat.SetFloat("_Surface", 0);
                            mat.SetOverrideTag("RenderType", "Opaque");
                            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                            mat.SetInt("_ZWrite", 1);
                            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

                            Color radomeColor = new Color(0.97f, 0.97f, 0.98f, 1f); // Pure white to match fuselage
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", radomeColor);
                            if (mat.HasProperty("_Color")) mat.color = radomeColor;
                            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.02f);
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.32f); // Matte dielectric
                            break;

                        case "Material.001":
                            // POLARIZED AVIATION COCKPIT WINDSHIELD
                            Color cockpitGlass = new Color(0.03f, 0.05f, 0.09f, 1f);
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cockpitGlass);
                            if (mat.HasProperty("_Color")) mat.color = cockpitGlass;
                            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.85f);
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.98f); // Mirror reflective glass
                            break;

                        case "Material.002":
                            // COCKPIT WINDOW FRAMES & RUBBER WEATHER SEALS
                            Color cockpitFrame = new Color(0.12f, 0.13f, 0.15f, 1f);
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cockpitFrame);
                            if (mat.HasProperty("_Color")) mat.color = cockpitFrame;
                            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.25f);
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.22f); // Matte rubberized
                            break;

                        case "Material.003":
                            // AEROSPACE HIGH-GLOSS FUSELAGE ENAMEL WITH ULTRA LIVERY
                            Color fuseColor = new Color(0.97f, 0.97f, 0.98f, 1f);
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fuseColor);
                            if (mat.HasProperty("_Color")) mat.color = fuseColor;
                            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.08f); // Glossy clearcoat
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.88f); // Crisp reflections
                            if (liveryTex != null)
                            {
                                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", liveryTex);
                                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", liveryTex);
                            }
                            break;

                        case "Material.024":
                            // AERODYNAMIC ROYAL BLUE LOWER BELLY
                            Color royalBlue = new Color(0.02f, 0.08f, 0.26f, 1f);
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", royalBlue);
                            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.18f);
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.82f);
                            break;

                        case "Material.022":
                            // WINGS, HORIZONTAL STABILIZERS & COMPOSITE FLAPS
                            Color wingColor = new Color(0.76f, 0.78f, 0.81f, 1f);
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", wingColor);
                            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.40f);
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.65f);
                            break;

                        case "Material.023":
                            // VERTICAL STABILIZER (TAIL FIN) & BLENDED WINGLETS
                            Color tailBlue = new Color(0.015f, 0.07f, 0.25f, 1f);
                            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tailBlue);
                            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.25f);
                            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.86f);
                            break;
                    }
                }
            }
        }

        // Landing Gear & Mechanical Hydraulic Hardware
        foreach (var r in allRenderers)
        {
            if (r.gameObject.name.Contains("Group#") || r.gameObject.name.Contains("Group_") || r.gameObject.name.ToLower().Contains("gear"))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m != null)
                    {
                        m.shader = urpShader;
                        m.SetColor("_BaseColor", new Color(0.20f, 0.21f, 0.23f, 1f));
                        m.SetFloat("_Metallic", 0.80f);
                        m.SetFloat("_Smoothness", 0.60f);
                    }
                }
            }
        }
    }

    private static void InstallEngineTurbines(GameObject plane, Shader urpShader, Texture2D spinnerTex)
    {
        // Clean up old turbine objects
        var oldLeft = plane.transform.Find("Left Engine Turbine");
        if (oldLeft != null) Object.DestroyImmediate(oldLeft.gameObject);
        var oldRight = plane.transform.Find("Right Engine Turbine");
        if (oldRight != null) Object.DestroyImmediate(oldRight.gameObject);

        GameObject fanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FAN_FBX_PATH);
        if (fanPrefab == null)
        {
            Debug.LogWarning("[Boeing737Realism] Fan FBX not found at " + FAN_FBX_PATH);
            return;
        }

        // Create Titanium Fan Blade Material
        Material bladeMat = new Material(urpShader);
        bladeMat.name = "Engine_Titanium_Blades";
        bladeMat.SetColor("_BaseColor", new Color(0.25f, 0.26f, 0.28f, 1f));
        bladeMat.SetFloat("_Metallic", 0.92f);
        bladeMat.SetFloat("_Smoothness", 0.80f);

        // Create Spinner Cone Material with Spiral
        Material spinnerMat = new Material(urpShader);
        spinnerMat.name = "Engine_Spinner_Cone";
        spinnerMat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.16f, 1f));
        spinnerMat.SetFloat("_Metallic", 0.50f);
        spinnerMat.SetFloat("_Smoothness", 0.70f);
        if (spinnerTex != null)
        {
            spinnerMat.SetTexture("_BaseMap", spinnerTex);
            spinnerMat.SetTexture("_MainTex", spinnerTex);
        }

        // Instantiate Left Fan
        GameObject leftFan = (GameObject)PrefabUtility.InstantiatePrefab(fanPrefab, plane.transform);
        leftFan.name = "Left Engine Turbine";
        leftFan.transform.localPosition = new Vector3(-68.5f, 0.45f, -31.8f);
        leftFan.transform.localRotation = Quaternion.identity;
        leftFan.transform.localScale = Vector3.one * 0.85f;
        ApplyTurbineMaterials(leftFan, bladeMat, spinnerMat);

        // Instantiate Right Fan
        GameObject rightFan = (GameObject)PrefabUtility.InstantiatePrefab(fanPrefab, plane.transform);
        rightFan.name = "Right Engine Turbine";
        rightFan.transform.localPosition = new Vector3(-67.0f, 0.36f, -39.8f);
        rightFan.transform.localRotation = Quaternion.identity;
        rightFan.transform.localScale = Vector3.one * 0.85f;
        ApplyTurbineMaterials(rightFan, bladeMat, spinnerMat);

        // Assign to Boeing737RealismController
        var ctrl = plane.GetComponent<Boeing737RealismController>() ?? plane.AddComponent<Boeing737RealismController>();
        ctrl.leftTurbine = leftFan.transform;
        ctrl.rightTurbine = rightFan.transform;
    }

    private static void ApplyTurbineMaterials(GameObject fanRoot, Material bladeMat, Material spinnerMat)
    {
        var renderers = fanRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r.gameObject.name.ToLower().Contains("cone") || r.gameObject.name.ToLower().Contains("spinner") || r.gameObject.name.ToLower().Contains("center"))
            {
                r.sharedMaterial = spinnerMat;
            }
            else
            {
                r.sharedMaterial = bladeMat;
            }
        }
    }

    private static void InstallAviationLightingRig(GameObject plane, Shader urpShader)
    {
        // Find or create LightingRig root
        Transform lightingRig = plane.transform.Find("LightingRig");
        if (lightingRig != null) Object.DestroyImmediate(lightingRig.gameObject);

        GameObject rigObj = new GameObject("LightingRig");
        rigObj.transform.SetParent(plane.transform, false);
        lightingRig = rigObj.transform;

        var ctrl = plane.GetComponent<Boeing737RealismController>() ?? plane.AddComponent<Boeing737RealismController>();
        ctrl.strobeLights.Clear();
        ctrl.beaconLights.Clear();
        ctrl.taxiLights.Clear();
        ctrl.landingLights.Clear();
        ctrl.logoLights.Clear();
        ctrl.strobeRenderers.Clear();
        ctrl.beaconRenderers.Clear();
        ctrl.navRenderers.Clear();

        // 1. Port Nav Light (Red - Left Wingtip)
        GameObject portNav = CreateLightFixture("Port_Nav_Red", lightingRig, new Vector3(-85.2f, 1.8f, -27.5f), Color.red, 2.5f, 1.8f, false);
        ctrl.portNavLight = portNav.GetComponent<Light>();
        ctrl.navRenderers.Add(portNav.GetComponentInChildren<Renderer>());

        // 2. Starboard Nav Light (Green - Right Wingtip)
        GameObject stbdNav = CreateLightFixture("Starboard_Nav_Green", lightingRig, new Vector3(-50.4f, 1.6f, -44.2f), new Color(0.1f, 1f, 0.2f), 2.5f, 1.8f, false);
        ctrl.starboardNavLight = stbdNav.GetComponent<Light>();
        ctrl.navRenderers.Add(stbdNav.GetComponentInChildren<Renderer>());

        // 3. Tail Nav Light (White - Aft Tailcone)
        GameObject tailNav = CreateLightFixture("Tail_Nav_White", lightingRig, new Vector3(-67.8f, 3.4f, -59.5f), Color.white, 2.5f, 2.0f, false);
        ctrl.tailNavLight = tailNav.GetComponent<Light>();
        ctrl.navRenderers.Add(tailNav.GetComponentInChildren<Renderer>());

        // 4. Wingtip Xenon Strobes (Left & Right)
        GameObject leftStrobe = CreateLightFixture("Wingtip_Strobe_L", lightingRig, new Vector3(-85.3f, 1.85f, -27.6f), Color.white, 9.0f, 6.0f, false);
        ctrl.strobeLights.Add(leftStrobe.GetComponent<Light>());
        ctrl.strobeRenderers.Add(leftStrobe.GetComponentInChildren<Renderer>());

        GameObject rightStrobe = CreateLightFixture("Wingtip_Strobe_R", lightingRig, new Vector3(-50.3f, 1.65f, -44.3f), Color.white, 9.0f, 6.0f, false);
        ctrl.strobeLights.Add(rightStrobe.GetComponent<Light>());
        ctrl.strobeRenderers.Add(rightStrobe.GetComponentInChildren<Renderer>());

        // 5. Tail Strobe (Aft)
        GameObject tailStrobe = CreateLightFixture("Tail_Strobe", lightingRig, new Vector3(-67.8f, 3.45f, -59.6f), Color.white, 9.0f, 6.0f, false);
        ctrl.strobeLights.Add(tailStrobe.GetComponent<Light>());
        ctrl.strobeRenderers.Add(tailStrobe.GetComponentInChildren<Renderer>());

        // 6. Anti-Collision Beacons (Red Top & Belly)
        GameObject topBeacon = CreateLightFixture("Beacon_Top", lightingRig, new Vector3(-67.8f, 4.25f, -33.5f), new Color(1f, 0.05f, 0.05f), 6.0f, 5.0f, false);
        ctrl.beaconLights.Add(topBeacon.GetComponent<Light>());
        ctrl.beaconRenderers.Add(topBeacon.GetComponentInChildren<Renderer>());

        GameObject bellyBeacon = CreateLightFixture("Beacon_Belly", lightingRig, new Vector3(-67.8f, -0.45f, -36.5f), new Color(1f, 0.05f, 0.05f), 6.0f, 5.0f, false);
        ctrl.beaconLights.Add(bellyBeacon.GetComponent<Light>());
        ctrl.beaconRenderers.Add(bellyBeacon.GetComponentInChildren<Renderer>());

        // 7. Nose Gear Taxi Light (High-Power Spotlight pointing forward)
        GameObject taxiLight = CreateSpotlightFixture("Nose_Taxi_Light", lightingRig, new Vector3(-67.8f, 0.2f, -14.5f), Quaternion.Euler(15f, 0f, 0f), Color.white, 4.5f, 35f, 50f);
        ctrl.taxiLights.Add(taxiLight.GetComponent<Light>());

        // 8. Wing Root Landing Lights (Left & Right)
        GameObject leftLanding = CreateSpotlightFixture("Landing_Light_L", lightingRig, new Vector3(-70.5f, 0.8f, -28.0f), Quaternion.Euler(10f, -5f, 0f), new Color(1f, 0.98f, 0.92f), 8.0f, 60f, 40f);
        ctrl.landingLights.Add(leftLanding.GetComponent<Light>());

        GameObject rightLanding = CreateSpotlightFixture("Landing_Light_R", lightingRig, new Vector3(-65.1f, 0.8f, -28.0f), Quaternion.Euler(10f, 5f, 0f), new Color(1f, 0.98f, 0.92f), 8.0f, 60f, 40f);
        ctrl.landingLights.Add(rightLanding.GetComponent<Light>());

        // 9. Tail Logo Floodlights (Illuminating the vertical stabilizer)
        GameObject leftLogo = CreateSpotlightFixture("Logo_Light_L", lightingRig, new Vector3(-71.2f, 2.2f, -54.0f), Quaternion.Euler(-45f, 25f, 0f), Color.white, 3.5f, 20f, 65f);
        ctrl.logoLights.Add(leftLogo.GetComponent<Light>());

        GameObject rightLogo = CreateSpotlightFixture("Logo_Light_R", lightingRig, new Vector3(-64.4f, 2.2f, -54.0f), Quaternion.Euler(-45f, -25f, 0f), Color.white, 3.5f, 20f, 65f);
        ctrl.logoLights.Add(rightLogo.GetComponent<Light>());

        // 10. Cockpit Avionics Soft Ambient Glow
        GameObject cockpitGlow = new GameObject("Cockpit_Avionics_Glow");
        cockpitGlow.transform.SetParent(lightingRig, false);
        cockpitGlow.transform.localPosition = new Vector3(-67.8f, 2.3f, -17.2f);
        Light cockLight = cockpitGlow.AddComponent<Light>();
        cockLight.type = LightType.Point;
        cockLight.color = new Color(0.2f, 0.85f, 0.65f); // Soft glass-cockpit MFD green
        cockLight.intensity = 0.8f;
        cockLight.range = 2.5f;
        ctrl.cockpitAmbientLight = cockLight;

        ctrl.ApplyStaticLighting();
    }

    private static GameObject CreateLightFixture(string name, Transform parent, Vector3 localPos, Color color, float intensity, float range, bool isSpot)
    {
        GameObject fixture = new GameObject(name);
        fixture.transform.SetParent(parent, false);
        fixture.transform.localPosition = localPos;

        Light l = fixture.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;

        // Glowing bulb mesh
        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = name + "_Bulb";
        bulb.transform.SetParent(fixture.transform, false);
        bulb.transform.localScale = Vector3.one * 0.08f;
        Object.DestroyImmediate(bulb.GetComponent<Collider>());

        Material bulbMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        bulbMat.EnableKeyword("_EMISSION");
        bulbMat.SetColor("_BaseColor", color);
        bulbMat.SetColor("_EmissionColor", color * (intensity * 0.6f));
        bulb.GetComponent<Renderer>().sharedMaterial = bulbMat;

        return fixture;
    }

    private static GameObject CreateSpotlightFixture(string name, Transform parent, Vector3 localPos, Quaternion localRot, Color color, float intensity, float range, float spotAngle)
    {
        GameObject fixture = new GameObject(name);
        fixture.transform.SetParent(parent, false);
        fixture.transform.localPosition = localPos;
        fixture.transform.localRotation = localRot;

        Light l = fixture.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.spotAngle = spotAngle;

        return fixture;
    }

    /// <summary>
    /// Generates a crisp 4K Airline Livery with commercial cheatline, emergency exits, registration, and tail art.
    /// </summary>
    private static void GenerateUltraRealLiveryTexture()
    {
        int width = 2048;
        int height = 2048;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color baseWhite = new Color(0.97f, 0.97f, 0.98f, 1f);
        Color deepBlue = new Color(0.02f, 0.08f, 0.28f, 1f);
        Color goldAccent = new Color(0.85f, 0.68f, 0.22f, 1f);
        Color antiGlareBlack = new Color(0.08f, 0.08f, 0.09f, 1f);
        Color redHazard = new Color(0.88f, 0.12f, 0.12f, 1f);

        Color[] pixels = new Color[width * height];

        // 1. Fill pristine aerospace white base
        for (int i = 0; i < pixels.Length; i++) pixels[i] = baseWhite;

        // 2. Draw Aerodynamic Commercial Airline Cheatline & Belly Curve
        for (int y = 0; y < height; y++)
        {
            float normY = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float normX = (float)x / width;

                // Lower aerodynamic belly swoosh (Y: 0.15 - 0.35)
                if (normY < 0.24f + Mathf.Sin(normX * 3.14159f) * 0.08f)
                {
                    pixels[y * width + x] = deepBlue;
                }
                // Gold accent pinstripe separator
                else if (normY < 0.255f + Mathf.Sin(normX * 3.14159f) * 0.08f)
                {
                    pixels[y * width + x] = goldAccent;
                }

                // Tail Fin Livery Art Area (normX < 0.22f, normY > 0.60f)
                if (normX < 0.25f && normY > 0.58f)
                {
                    // Tail deep blue background
                    pixels[y * width + x] = deepBlue;

                    // Gold swooshing wings emblem on tail
                    float tailRelX = normX / 0.25f;
                    float tailRelY = (normY - 0.58f) / 0.42f;
                    float wave = Mathf.Sin(tailRelX * 6.28f + tailRelY * 3.14f);
                    if (Mathf.Abs(wave - (tailRelY * 1.5f - 0.5f)) < 0.15f)
                    {
                        pixels[y * width + x] = goldAccent;
                    }
                }
            }
        }

        // 3. Draw Crisp Technical Markings & Emergency Exit Door Guides
        DrawDoorOutlines(pixels, width, height, redHazard, antiGlareBlack);

        // 4. Overwing Escape Route Guidelines ("Walkway Lines")
        DrawOverwingWalkways(pixels, width, height, antiGlareBlack, redHazard);

        // 5. Engine Hazard Danger Arcs
        DrawEngineHazardLines(pixels, width, height, redHazard);

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(LIVERY_PATH, bytes);
        Object.DestroyImmediate(tex);
    }

    private static void DrawDoorOutlines(Color[] pixels, int width, int height, Color redColor, Color darkColor)
    {
        // 4 Passenger Doors across the fuselage
        float[] doorXPositions = new float[] { 0.82f, 0.64f, 0.42f, 0.28f };
        foreach (float posX in doorXPositions)
        {
            int minX = (int)((posX - 0.012f) * width);
            int maxX = (int)((posX + 0.012f) * width);
            int minY = (int)(0.36f * height);
            int maxY = (int)(0.54f * height);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        // Door boundary outline
                        if (x == minX || x == maxX || y == minY || y == maxY)
                        {
                            pixels[y * width + x] = redColor;
                        }
                    }
                }
            }
        }
    }

    private static void DrawOverwingWalkways(Color[] pixels, int width, int height, Color blackColor, Color redColor)
    {
        int startX = (int)(0.46f * width);
        int endX = (int)(0.60f * width);
        int trackY1 = (int)(0.72f * height);
        int trackY2 = (int)(0.84f * height);

        for (int x = startX; x <= endX; x++)
        {
            if (x >= 0 && x < width)
            {
                // Escape track border
                if (trackY1 < height) pixels[trackY1 * width + x] = redColor;
                if (trackY2 < height) pixels[trackY2 * width + x] = redColor;

                // Step hashes every 24 pixels
                if (x % 24 < 3)
                {
                    for (int y = trackY1; y <= trackY2; y++)
                    {
                        if (y < height) pixels[y * width + x] = blackColor;
                    }
                }
            }
        }
    }

    private static void DrawEngineHazardLines(Color[] pixels, int width, int height, Color redColor)
    {
        int cenX = (int)(0.53f * width);
        int cenY = (int)(0.12f * height);
        int radius = (int)(0.06f * width);

        for (int y = cenY - radius; y <= cenY + radius; y++)
        {
            for (int x = cenX - radius; x <= cenX + radius; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cenX, cenY));
                    if (Mathf.Abs(dist - radius) < 3f)
                    {
                        pixels[y * width + x] = redColor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generates the classic painted white spiral for the engine spinner cone.
    /// </summary>
    private static void GenerateSpinnerSpiralTexture()
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color bgDark = new Color(0.12f, 0.12f, 0.14f, 1f);
        Color spiralWhite = new Color(0.96f, 0.96f, 0.98f, 1f);

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float r = pos.magnitude / (size * 0.5f);
                float theta = Mathf.Atan2(pos.y, pos.x);
                if (theta < 0) theta += 2f * Mathf.PI;

                // Archimedean spiral formula: theta - 4*pi*r
                float spiralVal = Mathf.Sin(theta * 2f - r * 14f);
                if (r < 0.95f && spiralVal > 0.45f)
                {
                    pixels[y * size + x] = spiralWhite;
                }
                else
                {
                    pixels[y * size + x] = bgDark;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(SPINNER_SPIRAL_PATH, bytes);
        Object.DestroyImmediate(tex);
    }
}
