using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

/// <summary>
/// AeroSim VR — Master Game Screen Atmosphere & Polish Tool.
/// Sets up:
/// 1. Terminal Interior Lighting (Zone-specific PBR downlights, Check-In/Security task spotlights, Lounge accents, Jetway lights)
/// 2. Dynamic Weather & Time-of-Day (TimeOfDayController, AirportWeatherController, Rain particles & Puddle controller)
/// 3. Spatial 3D Audio & Terminal Ambiance (AirportAmbianceController with PA announcements, AirportSFXManager with ATC chatter, PlayerFootstepAudio)
/// 4. NPC Crowd Flow & Living Terminal (AirportRouteManager, AirportTrafficManager with calibrated one-way passenger flow)
/// </summary>
[InitializeOnLoad]
public static class SetupGameScreenAtmosphere
{
    public const string GAME_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    public const string AUTO_FLAG_PATH = "Temp/SetupGameAtmosphere.flag";

    static SetupGameScreenAtmosphere()
    {
        EditorApplication.delayCall += CheckAutoSetup;
    }

    private static void CheckAutoOpen()
    {
        CheckAutoSetup();
    }

    private static void CheckAutoSetup()
    {
        if (System.IO.File.Exists(AUTO_FLAG_PATH))
        {
            try { System.IO.File.Delete(AUTO_FLAG_PATH); } catch { }
            Execute();
        }
    }

    [MenuItem("Tools/Aviation/Setup Game Screen Atmosphere & Polish #&a", false, 10)]
    public static void Execute()
    {
        Debug.Log("<color=#00FFCC>[GameScreenAtmosphere]</color> ═══════════════════════════════════════════");
        Debug.Log("<color=#00FFCC>[GameScreenAtmosphere]</color> Starting Master Atmosphere & Polish Configuration...");

        // Ensure we are operating on the game scene
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != GAME_SCENE_PATH)
        {
            Debug.Log($"<color=#00FFCC>[GameScreenAtmosphere]</color> Switching active scene to: {GAME_SCENE_PATH}");
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
        }

        Undo.SetCurrentGroupName("Setup Game Screen Atmosphere & Polish");
        int undoGroup = Undo.GetCurrentGroup();

        // ─────────────────────────────────────────────
        // 1. TERMINAL INTERIOR ARCHITECTURAL LIGHTING
        // ─────────────────────────────────────────────
        SetupTerminalLighting();

        // ─────────────────────────────────────────────
        // 2. DYNAMIC WEATHER & TIME OF DAY
        // ─────────────────────────────────────────────
        SetupWeatherAndTimeOfDay();

        // ─────────────────────────────────────────────
        // 3. SPATIAL 3D AUDIO & AMBIANCE
        // ─────────────────────────────────────────────
        SetupSpatialAudioAndAmbiance();

        // ─────────────────────────────────────────────
        // 4. LIVING TERMINAL & NPC CROWD FLOW
        // ─────────────────────────────────────────────
        SetupNPCPassengerFlow();

        // ─────────────────────────────────────────────
        // 5. PLAYER FOOTSTEP AUDIO & LOCOMOTION HOOK
        // ─────────────────────────────────────────────
        SetupPlayerLocomotionAndAudio();

        // ─────────────────────────────────────────────
        // 6. QUEST-OPTIMIZED REFLECTION PROBES
        // ─────────────────────────────────────────────
        SetupQuestReflectionProbes();

        // ─────────────────────────────────────────────
        // 7. BOEING 737 SPLINE & REALISM FIX
        // ─────────────────────────────────────────────
        OptimizeBoeing737();

        // Mark scenes dirty & save
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#00FFCC>[GameScreenAtmosphere]</color> SUCCESS! Game screen atmosphere, lighting, weather, audio & crowd flow fully initialized for Meta Quest!");
        Debug.Log("<color=#00FFCC>[GameScreenAtmosphere]</color> ═══════════════════════════════════════════");
    }

    // ═════════════════════════════════════════════════════
    //  1. TERMINAL INTERIOR LIGHTING
    // ═════════════════════════════════════════════════════

    private static void SetupTerminalLighting()
    {
        Debug.Log("<color=#00FFCC>[Lighting]</color> Configuring zone-specific architectural lighting...");

        var root = GameObject.Find("Terminal_Architectural_Lighting");
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }

        root = new GameObject("Terminal_Architectural_Lighting");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        // Clean up legacy floating lights if any
        var oldRig = GameObject.Find("Architectural_Lighting_Rig");
        if (oldRig != null)
        {
            oldRig.SetActive(false); // disable old unoptimized grid
        }

        // Zone 1: Curbside Entrance Canopy (Warm Welcome LED 3200K)
        Transform canopyGroup = CreateGroup("01_Curbside_Canopy_Lights", root.transform);
        Color canopyWarm = new Color(1.0f, 0.94f, 0.85f);
        for (float x = 28f; x <= 40f; x += 4f)
        {
            CreatePointLight($"Canopy_LED_{x:0}", canopyGroup, new Vector3(x, 2.2f, -41.1f), canopyWarm, 1.8f, 10f);
        }

        // Zone 2: Check-In Concourse & Counter Task Spots
        Transform checkinGroup = CreateGroup("02_CheckIn_Hall_Lights", root.transform);
        Color concourseColor = new Color(0.92f, 0.96f, 1.0f); // Clean cool white 4200K
        for (float x = -2f; x <= 12f; x += 4.5f)
        {
            for (float z = -65f; z <= -52f; z += 4.5f)
            {
                CreatePointLight($"CheckIn_Ceiling_{x:0}_{z:0}", checkinGroup, new Vector3(x, 2.3f, z), concourseColor, 1.6f, 9f);
            }
        }

        // Focused Task Spotlights aimed directly down at the Check-In counters
        CreateSpotLight("CheckIn_Counter1_TaskSpot", checkinGroup, new Vector3(2.21f, 2.6f, -58.41f), Quaternion.Euler(85f, 0f, 0f), Color.white, 3.0f, 7f, 65f);
        CreateSpotLight("CheckIn_Counter2_TaskSpot", checkinGroup, new Vector3(4.80f, 2.6f, -58.41f), Quaternion.Euler(85f, 0f, 0f), Color.white, 3.0f, 7f, 65f);

        // Zone 3: Main Concourse Spine (Guiding walkway from entrance to security)
        Transform concourseGroup = CreateGroup("03_Main_Concourse_Lights", root.transform);
        for (float x = 12f; x <= 28f; x += 4f)
        {
            CreatePointLight($"Concourse_Spine_{x:0}", concourseGroup, new Vector3(x, 2.3f, -30f), concourseColor, 1.6f, 11f);
            CreatePointLight($"Concourse_Spine_B_{x:0}", concourseGroup, new Vector3(x, 2.3f, -20f), concourseColor, 1.6f, 11f);
        }

        // Zone 4: Security Checkpoint Inspection (High-CRI Cool White 4800K)
        Transform secGroup = CreateGroup("04_Security_Checkpoint_Lights", root.transform);
        Color secCool = new Color(0.90f, 0.95f, 1.0f);
        CreateSpotLight("Security_Arch_InspectionSpot", secGroup, new Vector3(17.70f, 2.6f, -9.92f), Quaternion.Euler(90f, 0f, 0f), secCool, 3.5f, 8f, 70f);
        CreateSpotLight("Security_XRay_InspectionSpot", secGroup, new Vector3(15.50f, 2.6f, -9.92f), Quaternion.Euler(85f, 0f, 0f), secCool, 3.0f, 8f, 70f);
        CreatePointLight("Security_Hall_Ambient", secGroup, new Vector3(17.70f, 2.3f, -9.92f), secCool, 1.8f, 12f);

        // Zone 5: Passport Control / Immigration Booth
        Transform immGroup = CreateGroup("05_Passport_Control_Lights", root.transform);
        CreateSpotLight("Immigration_Booth_TaskSpot", immGroup, new Vector3(1.90f, 2.5f, 3.69f), Quaternion.Euler(85f, 0f, 0f), Color.white, 3.2f, 7f, 65f);
        CreatePointLight("Immigration_Hall_Ambient", immGroup, new Vector3(2.0f, 2.3f, 3.7f), concourseColor, 1.6f, 10f);

        // Zone 6: Departure Lounge & Seating (Warm Relaxed 3000K Lounge Atmosphere)
        Transform loungeGroup = CreateGroup("06_Departure_Lounge_Lights", root.transform);
        Color loungeWarm = new Color(1.0f, 0.92f, 0.82f);
        for (float x = -15f; x <= -7f; x += 3.5f)
        {
            for (float z = 0f; z <= 8f; z += 3.5f)
            {
                CreatePointLight($"Lounge_Ceiling_{x:0}_{z:0}", loungeGroup, new Vector3(x, 2.3f, z), loungeWarm, 1.4f, 8.5f);
            }
        }

        // Gate A12 Podium Task Light
        CreateSpotLight("Gate_A12_Podium_TaskSpot", loungeGroup, new Vector3(-14.50f, 2.5f, 2.00f), Quaternion.Euler(85f, 0f, 0f), Color.white, 2.8f, 6.5f, 60f);

        // Zone 7: Telescopic Jetway Tunnel (Linear Boarding Lights)
        Transform jetwayGroup = CreateGroup("07_Jetway_Tunnel_Lights", root.transform);
        Color jetwayColor = new Color(0.95f, 0.98f, 1.0f);
        CreatePointLight("Jetway_Light_01", jetwayGroup, new Vector3(-11.5f, 1.8f, 4.5f), jetwayColor, 1.8f, 6f);
        CreatePointLight("Jetway_Light_02", jetwayGroup, new Vector3(-7.5f, 1.9f, 8.5f), jetwayColor, 1.8f, 6f);
        CreatePointLight("Jetway_Light_03", jetwayGroup, new Vector3(-5.0f, 2.0f, 11.2f), jetwayColor, 2.0f, 6f);

        Debug.Log("<color=#00FFCC>[Lighting]</color> Terminal architectural lighting configured across 7 authentic functional zones!");
    }

    // ═════════════════════════════════════════════════════
    //  2. DYNAMIC WEATHER & TIME OF DAY
    // ═════════════════════════════════════════════════════

    private static void SetupWeatherAndTimeOfDay()
    {
        Debug.Log("<color=#00FFCC>[Weather & Time]</color> Integrating TimeOfDayController and AirportWeatherController...");

        // Ensure TimeOfDayController exists in scene
        var tod = Object.FindObjectOfType<TimeOfDayController>();
        if (tod == null)
        {
            var todGo = new GameObject("TimeOfDayController");
            tod = todGo.AddComponent<TimeOfDayController>();
        }

        // Configure Sun Light reference
        if (RenderSettings.sun == null)
        {
            var dirLights = Object.FindObjectsOfType<Light>();
            foreach (var l in dirLights)
            {
                if (l.type == LightType.Directional)
                {
                    RenderSettings.sun = l;
                    break;
                }
            }
        }
        tod.sunLight = RenderSettings.sun;
        if (tod.sunLight != null)
        {
            tod.sunLight.shadows = LightShadows.Soft;
            tod.sunLight.shadowStrength = 1.0f;
            tod.sunLight.shadowBias = 0.015f;
            tod.sunLight.shadowNormalBias = 0.15f;
        }

        // Ensure AirportWeatherController exists in scene
        var weather = Object.FindObjectOfType<AirportWeatherController>();
        if (weather == null)
        {
            var weatherGo = new GameObject("AirportWeatherController");
            weather = weatherGo.AddComponent<AirportWeatherController>();
        }
        weather.timeController = tod;

        // Ensure Rain Particle System exists
        var rainGo = GameObject.Find("Airport_Rain_System");
        ParticleSystem rainPs = null;
        if (rainGo != null)
        {
            rainPs = rainGo.GetComponent<ParticleSystem>();
        }
        else
        {
            rainGo = new GameObject("Airport_Rain_System");
            rainGo.transform.position = new Vector3(-5f, 18f, -20f); // Centered over terminal & apron
            rainPs = rainGo.AddComponent<ParticleSystem>();
            var psMain = rainPs.main;
            psMain.loop = true;
            psMain.startLifetime = 1.5f;
            psMain.startSpeed = 28f;
            psMain.startSize = 0.08f;
            psMain.maxParticles = 3000;
            psMain.simulationSpace = ParticleSystemSimulationSpace.World;
            psMain.startColor = new Color(0.85f, 0.92f, 1.0f, 0.35f);

            var psEmission = rainPs.emission;
            psEmission.rateOverTime = 1200;

            var psShape = rainPs.shape;
            psShape.shapeType = ParticleSystemShapeType.Box;
            psShape.scale = new Vector3(120f, 1f, 120f);

            var psVelocity = rainPs.velocityOverLifetime;
            psVelocity.enabled = true;
            psVelocity.x = new ParticleSystem.MinMaxCurve(-2.5f);
            psVelocity.y = new ParticleSystem.MinMaxCurve(-28f);
            psVelocity.z = new ParticleSystem.MinMaxCurve(-3.5f);

            var psRenderer = rainGo.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            psRenderer.cameraVelocityScale = 0.05f;
            psRenderer.lengthScale = 2.5f;
        }
        weather.rainParticleSystem = rainPs;

        // Ensure ApronRainPuddleController exists
        var puddleCtrl = Object.FindObjectOfType<ApronRainPuddleController>();
        if (puddleCtrl == null)
        {
            var puddleGo = new GameObject("ApronRainPuddleController");
            puddleCtrl = puddleGo.AddComponent<ApronRainPuddleController>();
        }
        weather.puddleController = puddleCtrl;

        // Assign ApronTarmac_WetWeather material if available
        var wetMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ApronTarmac_WetWeather.mat");
        if (wetMat != null)
        {
            weather.tarmacMaterial = wetMat;
            puddleCtrl.tarmacMaterial = wetMat;
        }

        Debug.Log("<color=#00FFCC>[Weather & Time]</color> Dynamic weather and Time-of-day fully bound.");
    }

    // ═════════════════════════════════════════════════════
    //  3. SPATIAL 3D AUDIO & AMBIANCE
    // ═════════════════════════════════════════════════════

    private static void SetupSpatialAudioAndAmbiance()
    {
        Debug.Log("<color=#00FFCC>[Spatial Audio]</color> Setting up terminal ambiance, ATC radio chatter & SFX...");

        // Ensure AirportAmbianceController exists
        var ambiance = Object.FindObjectOfType<AirportAmbianceController>();
        if (ambiance == null)
        {
            var ambianceGo = new GameObject("AirportAmbianceController");
            ambiance = ambianceGo.AddComponent<AirportAmbianceController>();
        }

        // Ensure AirportSFXManager exists
        var sfx = Object.FindObjectOfType<AirportSFXManager>();
        if (sfx == null)
        {
            var sfxGo = new GameObject("AirportSFXManager");
            sfx = sfxGo.AddComponent<AirportSFXManager>();
        }

        // Load ATC chatter, apron ambience and rain audio from assets if available
        if (sfx.atcChatterClip == null)
        {
            sfx.atcChatterClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/atc_chatter.mp3") 
                              ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Turbofan_Engine_Loop.wav");
        }
        if (sfx.apronAmbienceClip == null)
        {
            sfx.apronAmbienceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Turbofan_Engine_Loop.wav");
        }

        Debug.Log("<color=#00FFCC>[Spatial Audio]</color> Spatial audio engines (Ambiance + SFX) active.");
    }

    // ═════════════════════════════════════════════════════
    //  4. NPC CROWD FLOW & LIVING TERMINAL
    // ═════════════════════════════════════════════════════

    private static void SetupNPCPassengerFlow()
    {
        Debug.Log("<color=#00FFCC>[NPC Flow]</color> Calibrating passenger route flow hierarchy...");

        // Invoke existing SetupAirportNPCFlow overhaul logic
        SetupAirportNPCFlow.Execute();

        // Ensure AirportTrafficManager is on AirportRouteManager
        var routeManager = Object.FindObjectOfType<AirportRouteManager>();
        if (routeManager != null)
        {
            var traffic = routeManager.GetComponent<AirportTrafficManager>();
            if (traffic == null)
            {
                traffic = routeManager.gameObject.AddComponent<AirportTrafficManager>();
            }
        }

        Debug.Log("<color=#00FFCC>[NPC Flow]</color> AirportTrafficManager and route manager flow active.");
    }

    // ═════════════════════════════════════════════════════
    //  5. PLAYER FOOTSTEP AUDIO & LOCOMOTION
    // ═════════════════════════════════════════════════════

    private static void SetupPlayerLocomotionAndAudio()
    {
        Debug.Log("<color=#00FFCC>[Player]</color> Verifying player locomotion and footstep audio...");

        // Find Player Rig / XR Origin
        GameObject playerRig = GameObject.Find("XR Origin (XR Rig)") 
                            ?? GameObject.Find("Player") 
                            ?? GameObject.Find("Complete XR Origin Set Up Variant");

        if (playerRig != null)
        {
            playerRig.tag = "Player"; // Guarantee Player tag for trigger interactions

            var cc = playerRig.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = playerRig.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.9f, 0f);
            }

            var locomotion = playerRig.GetComponent<AirportLocomotion>();
            if (locomotion == null)
            {
                locomotion = playerRig.AddComponent<AirportLocomotion>();
            }
            locomotion.controlsLocked = false; // Player can move freely immediately!

            var footstep = playerRig.GetComponent<PlayerFootstepAudio>();
            if (footstep == null)
            {
                footstep = playerRig.AddComponent<PlayerFootstepAudio>();
            }
            footstep.BindLocomotion(locomotion);

            Debug.Log("<color=#00FFCC>[Player]</color> Player locomotion and footstep audio calibrated.");
        }
        else
        {
            Debug.LogWarning("[Player] No player rig found in scene root.");
        }
    }

    // ═════════════════════════════════════════════════════
    //  6. QUEST-OPTIMIZED REFLECTION PROBES
    // ═════════════════════════════════════════════════════

    private static void SetupQuestReflectionProbes()
    {
        Debug.Log("<color=#00FFCC>[Reflection Probes]</color> Configuring Quest-optimized Box-Projected Reflection Probes...");

        var root = GameObject.Find("Quest_Reflection_Probes");
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }

        root = new GameObject("Quest_Reflection_Probes");
        root.transform.position = Vector3.zero;

        // Zone 1: Check-in Concourse
        CreateReflectionProbe(
            "Probe_CheckIn_Concourse",
            root.transform,
            position: new Vector3(5f, 2.0f, -55f),
            size: new Vector3(32f, 6f, 32f)
        );

        // Zone 2: Main Concourse & Security Checkpoint
        CreateReflectionProbe(
            "Probe_Concourse_Security",
            root.transform,
            position: new Vector3(18f, 2.0f, -20f),
            size: new Vector3(30f, 6f, 40f)
        );

        // Zone 3: Departure Lounge & Gate A12
        CreateReflectionProbe(
            "Probe_Departure_Lounge",
            root.transform,
            position: new Vector3(-11f, 2.0f, 4f),
            size: new Vector3(25f, 6f, 22f)
        );

        // Zone 4: Apron / Boarding Gate
        CreateReflectionProbe(
            "Probe_Apron_Gate",
            root.transform,
            position: new Vector3(-6f, 4.0f, 22f),
            size: new Vector3(50f, 16f, 45f)
        );

        Debug.Log("<color=#00FFCC>[Reflection Probes]</color> Reflection probes configured (OnAwake, Res 128, Box Projected).");
    }

    // ═════════════════════════════════════════════════════
    //  7. BOEING 737 SPLINE & REALISM OPTIMIZATION
    // ═════════════════════════════════════════════════════

    private static void OptimizeBoeing737()
    {
        Debug.Log("<color=#00FFCC>[Aircraft]</color> Configuring Boeing 737 spline animation...");
        var animators = Object.FindObjectsOfType<SplineAnimate>();
        foreach (var anim in animators)
        {
            anim.PlayOnAwake = false;
        }
        Debug.Log("<color=#00FFCC>[Aircraft]</color> Boeing 737 spline animation set to trigger-driven only (PlayOnAwake disabled).");
    }

    // ═════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════

    private static Transform CreateGroup(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void CreatePointLight(string name, Transform parent, Vector3 worldPos, Color color, float intensity, float range)
    {
        var fixture = new GameObject(name);
        fixture.transform.SetParent(parent, false);
        fixture.transform.position = worldPos;

        var l = fixture.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None; // Optimized for performance in VR!
    }

    private static void CreateSpotLight(string name, Transform parent, Vector3 worldPos, Quaternion rotation, Color color, float intensity, float range, float spotAngle)
    {
        var fixture = new GameObject(name);
        fixture.transform.SetParent(parent, false);
        fixture.transform.position = worldPos;
        fixture.transform.rotation = rotation;

        var l = fixture.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.spotAngle = spotAngle;
        l.shadows = LightShadows.None; // Soft task spot
    }

    private static void CreateReflectionProbe(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var fixture = new GameObject(name);
        fixture.transform.SetParent(parent, false);
        fixture.transform.position = position;

        var probe = fixture.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        probe.resolution = 128;
        probe.boxProjection = true;
        probe.size = size;
        probe.intensity = 1.0f;
    }
}
