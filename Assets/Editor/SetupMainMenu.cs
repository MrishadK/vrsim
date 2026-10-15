using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

/// <summary>
/// AeroSim VR — Creates the Live 3D Rendered Main Menu Scene.
/// Redesigned for maximum visual clarity, zero blurriness, and hero 3D Boeing 737 framing.
/// Run via Tools > Setup Main Menu.
/// </summary>
public static class MainMenuSetup
{
    // ── Color palette (High contrast, modern glassmorphic aviation HUD) ──
    static readonly Color DarkPanel       = new Color(0.04f, 0.08f, 0.16f, 0.94f);
    static readonly Color CardBg          = new Color(0.03f, 0.07f, 0.14f, 0.88f);
    static readonly Color ModalBorder     = new Color(0.20f, 0.85f, 0.95f, 0.50f);
    static readonly Color CyanAccent      = new Color(0.18f, 0.88f, 0.98f, 1f);
    static readonly Color CyanGlow        = new Color(0.15f, 0.78f, 0.92f, 0.75f);
    static readonly Color ButtonPrimary   = new Color(0.08f, 0.68f, 0.82f, 1f);
    static readonly Color ButtonDark      = new Color(0.06f, 0.12f, 0.22f, 0.90f);
    static readonly Color TextWhite       = new Color(0.96f, 0.98f, 1f);
    static readonly Color TextMuted       = new Color(0.60f, 0.74f, 0.86f, 0.92f);
    static readonly Color TextDim         = new Color(0.45f, 0.58f, 0.72f, 0.75f);
    static readonly Color VersionColor    = new Color(0.55f, 0.68f, 0.82f, 0.85f);

    [MenuItem("Tools/Setup Main Menu")]
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[AeroSim VR] Stopping Play Mode before executing MainMenu setup...");
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += Execute;
            return;
        }

        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != scenePath)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            currentScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        // Clean up old root objects (protecting MCP runtime so connection stays alive)
        foreach (var r in currentScene.GetRootGameObjects())
        {
            if (!r.name.Contains("MCP") && !r.name.Contains("mcp") && !r.name.Contains("Transport"))
            {
                Object.DestroyImmediate(r);
            }
        }

        // ═══════════════════════════════════════════════════
        //  1. ENVIRONMENT LIGHTING & SKYBOX
        // ═══════════════════════════════════════════════════

        var skyboxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VRTemplateAssets/Materials/Skybox/Materials/skybox01_openGL.mat");
        if (skyboxMat != null)
        {
            RenderSettings.skybox = skyboxMat;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.72f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.68f, 0.74f, 0.82f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.20f, 0.24f); // Rich dark asphalt bounce

        // Linear Fog: Keeps the foreground tarmac and Boeing 737 100% crisp and clear,
        // while naturally fading the distant horizon into atmospheric perspective!
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 140f;
        RenderSettings.fogEndDistance = 900f;
        RenderSettings.fogColor = new Color(0.74f, 0.84f, 0.94f);

        // Directional Light (Sun) — Calibrated Key Light shining from pitch 32°, yaw 48°
        // directly illuminating the camera-facing side of the Boeing 737, the engine,
        // and the front glass facade of the airport terminal!
        var sunGo = new GameObject("Directional Light");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 1.0f; // 100% solid shadow blocking - prevents sunlight penetrating buildings
        sun.shadowBias = 0.015f;
        sun.shadowNormalBias = 0.15f;
        sun.intensity = 1.85f;
        sun.color = new Color(1.0f, 0.95f, 0.88f); // Warm natural daylight (5400K)
        sunGo.transform.rotation = Quaternion.Euler(32f, 48f, 0f);
        RenderSettings.sun = sun;

        // Dynamic Time of Day Controller
        var todGo = new GameObject("TimeOfDayController");
        var tod = todGo.AddComponent<TimeOfDayController>();
        tod.sunLight = sun;
        tod.skyboxMaterial = RenderSettings.skybox;

        // ═══════════════════════════════════════════════════
        //  2. LIVE 3D AIRPORT SCENERY & HERO AIRCRAFT
        // ═══════════════════════════════════════════════════

        var envRoot = new GameObject("LiveAirportScenery");

        // ── Tarmac / Apron Ground Plane ──
        var groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "ApronTarmac";
        groundPlane.transform.SetParent(envRoot.transform, false);
        groundPlane.transform.position = Vector3.zero;
        groundPlane.transform.localScale = new Vector3(40f, 1f, 40f); // 400m x 400m apron

        Material tarmacInstance = null;
        var runwayMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/pebbled-asphalt1-unity/runway.mat");
        if (runwayMat != null)
        {
            var groundRenderer = groundPlane.GetComponent<MeshRenderer>();
            tarmacInstance = new Material(runwayMat);
            tarmacInstance.mainTextureScale = new Vector2(40f, 40f);
            // Crucial fix: Enable ground shadow receiving so aircraft casts grounding contact shadows!
            tarmacInstance.DisableKeyword("_RECEIVE_SHADOWS_OFF");
            tarmacInstance.SetFloat("_ReceiveShadows", 1.0f);
            tarmacInstance.SetFloat("_Smoothness", 0.40f); // Subtle wet sheen reflection
            groundRenderer.sharedMaterial = tarmacInstance;
            groundRenderer.receiveShadows = true;
        }

        // ── Comprehensive ICAO Apron Markings (Centerlines, Stop Bars, Safety Envelope, Zipper Road) ──
        CreateRealisticApronMarkings(envRoot.transform);

        // ── Hero Aircraft: Ultra-Detailed Airbus A350-900 NEO Concept ──
        var a350Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/A350NeoConcept/A350-900_NEO_Concept.prefab");
        if (a350Prefab != null)
        {
            var planeGo = (GameObject)PrefabUtility.InstantiatePrefab(a350Prefab, envRoot.transform);
            planeGo.name = "A350_Showcase";
            // Parked at Gate 2 with Door 1L aligned to the Telescopic Jetway
            planeGo.transform.position = new Vector3(-8.0f, -0.23f, 68.0f);
            planeGo.transform.rotation = Quaternion.Euler(0f, 195.0f, 0f);

            // Add SimpleRotator to turbofan blades and spinner noses
            var transforms = planeGo.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t.name.ToLower().Contains("fan face") || t.name.ToLower().Contains("spinner"))
                {
                    var rot = t.gameObject.AddComponent<SimpleRotator>();
                    rot.rotationAxis = Vector3.forward;
                    rot.rotationSpeed = 360f;
                }
            }

            // Ensure all airframe meshes cast and receive soft shadows
            var planeRenderers = planeGo.GetComponentsInChildren<Renderer>(true);
            foreach (var pr in planeRenderers)
            {
                pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                pr.receiveShadows = true;
            }

            // Aircraft Belly Fill Light
            var bounceLightGo = new GameObject("Aircraft_BellyFillLight");
            bounceLightGo.transform.SetParent(planeGo.transform, false);
            bounceLightGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var bounceL = bounceLightGo.AddComponent<Light>();
            bounceL.type = LightType.Point;
            bounceL.color = new Color(0.82f, 0.85f, 0.95f);
            bounceL.range = 25f;
            bounceL.intensity = 1.2f;

            // Dedicated Gate Apron Servicing Floodlights
            var servLeftGo = new GameObject("GateServicing_LeftFlood");
            servLeftGo.transform.SetParent(planeGo.transform, false);
            servLeftGo.transform.localPosition = new Vector3(-16f, 18f, 12f);
            servLeftGo.transform.localRotation = Quaternion.Euler(45f, 50f, 0f);
            var servL = servLeftGo.AddComponent<Light>();
            servL.type = LightType.Spot;
            servL.color = new Color(1.0f, 0.96f, 0.90f);
            servL.range = 75f;
            servL.spotAngle = 80f;
            servL.intensity = 24f;
            servL.shadows = LightShadows.Soft;

            var servRightGo = new GameObject("GateServicing_RightFlood");
            servRightGo.transform.SetParent(planeGo.transform, false);
            servRightGo.transform.localPosition = new Vector3(16f, 18f, -8f);
            servRightGo.transform.localRotation = Quaternion.Euler(45f, -60f, 0f);
            var servR = servRightGo.AddComponent<Light>();
            servR.type = LightType.Spot;
            servR.color = new Color(0.95f, 0.98f, 1.0f);
            servR.range = 75f;
            servR.spotAngle = 80f;
            servR.intensity = 24f;
            servR.shadows = LightShadows.Soft;

            tod.aircraftShowcase = planeGo.transform;

            // Add Aviation Showcase FX (Pulsing beacons, strobes, nav lights)
            var showcaseFX = planeGo.AddComponent<AircraftShowcaseFX>();

            // Upper Red Anti-Collision Beacon
            var beaconTopGo = new GameObject("Beacon_Top");
            beaconTopGo.transform.SetParent(planeGo.transform, false);
            beaconTopGo.transform.localPosition = new Vector3(0f, 9.5f, -2.0f);
            var bTopLight = beaconTopGo.AddComponent<Light>();
            bTopLight.type = LightType.Point;
            bTopLight.color = Color.red;
            bTopLight.range = 25f;
            bTopLight.intensity = 0f;
            showcaseFX.beaconLights.Add(bTopLight);

            // Lower Belly Red Beacon
            var beaconBtmGo = new GameObject("Beacon_Bottom");
            beaconBtmGo.transform.SetParent(planeGo.transform, false);
            beaconBtmGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var bBtmLight = beaconBtmGo.AddComponent<Light>();
            bBtmLight.type = LightType.Point;
            bBtmLight.color = Color.red;
            bBtmLight.range = 25f;
            bBtmLight.intensity = 0f;
            showcaseFX.beaconLights.Add(bBtmLight);

            // Wingtip White Strobes
            var portStrobeGo = new GameObject("Strobe_Port");
            portStrobeGo.transform.SetParent(planeGo.transform, false);
            portStrobeGo.transform.localPosition = new Vector3(-32.0f, 6.5f, -12.0f);
            var pStrobe = portStrobeGo.AddComponent<Light>();
            pStrobe.type = LightType.Point;
            pStrobe.color = Color.white;
            pStrobe.range = 18f;
            pStrobe.intensity = 0f;
            showcaseFX.strobeLights.Add(pStrobe);

            var stbdStrobeGo = new GameObject("Strobe_Starboard");
            stbdStrobeGo.transform.SetParent(planeGo.transform, false);
            stbdStrobeGo.transform.localPosition = new Vector3(32.0f, 6.5f, -12.0f);
            var sStrobe = stbdStrobeGo.AddComponent<Light>();
            sStrobe.type = LightType.Point;
            sStrobe.color = Color.white;
            sStrobe.range = 18f;
            sStrobe.intensity = 0f;
            showcaseFX.strobeLights.Add(sStrobe);

            // Navigation Lights (Red left, Green right)
            var portNavGo = new GameObject("Nav_Port_Red");
            portNavGo.transform.SetParent(planeGo.transform, false);
            portNavGo.transform.localPosition = new Vector3(-32.2f, 6.5f, -11.0f);
            var pNav = portNavGo.AddComponent<Light>();
            pNav.type = LightType.Point;
            pNav.color = Color.red;
            pNav.range = 10f;
            pNav.intensity = 3.5f;
            showcaseFX.navLights.Add(pNav);

            var stbdNavGo = new GameObject("Nav_Starboard_Green");
            stbdNavGo.transform.SetParent(planeGo.transform, false);
            stbdNavGo.transform.localPosition = new Vector3(32.2f, 6.5f, -11.0f);
            var sNav = stbdNavGo.AddComponent<Light>();
            sNav.type = LightType.Point;
            sNav.color = Color.green;
            sNav.range = 10f;
            sNav.intensity = 3.5f;
            showcaseFX.navLights.Add(sNav);
        }

        // ── Real Modern International Airport Terminal (Full PBR Architecture) ──
        var modernTerminalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ModernAirport/Modern_Airport_Terminal.prefab");
        if (modernTerminalPrefab != null)
        {
            var termGo = (GameObject)PrefabUtility.InstantiatePrefab(modernTerminalPrefab, envRoot.transform);
            termGo.name = "Modern_Airport_Terminal";
            termGo.transform.position = Vector3.zero;
            termGo.transform.rotation = Quaternion.identity;
            termGo.transform.localScale = Vector3.one;
            PrefabUtility.UnpackPrefabInstance(termGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Eliminate duplicate tarmac ground from modern terminal prefab
            var dupGround = termGo.transform.Find("01_Exterior_Architecture/Apron_Tarmac_Ground");
            if (dupGround != null)
            {
                UnityEngine.Object.DestroyImmediate(dupGround.gameObject);
            }

            // Tune Glass Curtain Walls for crystal-clear panoramic visibility & realistic gloss
            var termRenderers = termGo.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var tr in termRenderers)
            {
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                tr.receiveShadows = true;

                var mats = tr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].name.Contains("CurtainGlass"))
                    {
                        var gMat = new Material(mats[i]);
                        gMat.color = new Color(0.12f, 0.25f, 0.38f, 0.18f);
                        gMat.SetFloat("_Smoothness", 0.98f);
                        gMat.SetFloat("_Metallic", 0.05f);
                        mats[i] = gMat;
                        changed = true;
                    }
                }
                if (changed) tr.sharedMaterials = mats;
            }

            // Spin Airport Surface Radar on rooftop
            var radarT = termGo.transform.Find("04_Rooftop_HVAC_Avionics/Airport_Surface_Radar");
            if (radarT != null)
            {
                var rRot = radarT.gameObject.AddComponent<SimpleRotator>();
                rRot.rotationAxis = Vector3.up;
                rRot.rotationSpeed = 120f;
            }

            var lSys = termGo.transform.Find("05_Lighting_System");
            if (lSys != null) tod.modernTerminalLighting = lSys;
        }

        // ── Air Traffic Control (ATC) Tower in the distance ──
        var atcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/atc.fbx");
        if (atcPrefab != null)
        {
            var atcGo = (GameObject)PrefabUtility.InstantiatePrefab(atcPrefab, envRoot.transform);
            atcGo.name = "AirTrafficControl_Tower";
            atcGo.transform.position = new Vector3(-65f, 0f, 130f);
            atcGo.transform.rotation = Quaternion.Euler(0f, 140f, 0f);
            atcGo.transform.localScale = Vector3.one * 1.5f;

            // Red hazard warning beacon on top of ATC cab
            var atcBeaconGo = new GameObject("ATC_TopBeacon");
            atcBeaconGo.transform.SetParent(atcGo.transform, false);
            atcBeaconGo.transform.localPosition = new Vector3(0f, 32f, 0f);
            var atcB = atcBeaconGo.AddComponent<Light>();
            atcB.type = LightType.Point;
            atcB.color = Color.red;
            atcB.range = 55f;
            atcB.intensity = 8.0f;

            // Rotating beacon
            var atcRot = atcBeaconGo.AddComponent<SimpleRotator>();
            atcRot.rotationAxis = Vector3.up;
            atcRot.rotationSpeed = 180f;
        }

        // ── Apron Passenger Shuttle Bus ──
        var busPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/bus.fbx");
        if (busPrefab != null)
        {
            var busGo = (GameObject)PrefabUtility.InstantiatePrefab(busPrefab, envRoot.transform);
            busGo.name = "Apron_Passenger_ShuttleBus";
            busGo.transform.position = new Vector3(12f, 0f, 56f);
            busGo.transform.rotation = Quaternion.Euler(0f, 310f, 0f);
            busGo.transform.localScale = Vector3.one * 0.95f;
        }

        // ── Apron Baggage Carts & Luggage Staging ──
        var luggagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Airport_Luggage_Baggage.obj");
        if (luggagePrefab != null)
        {
            var lugGo = (GameObject)PrefabUtility.InstantiatePrefab(luggagePrefab, envRoot.transform);
            lugGo.name = "Apron_Luggage_Staging";
            lugGo.transform.position = new Vector3(3f, 0f, 62f);
            lugGo.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
            lugGo.transform.localScale = Vector3.one * 1.2f;
        }

        var trolleyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/trolley/source/Airport_Trolley.fbx");
        if (trolleyPrefab != null)
        {
            var trGo = (GameObject)PrefabUtility.InstantiatePrefab(trolleyPrefab, envRoot.transform);
            trGo.name = "Apron_Trolley_Train";
            trGo.transform.position = new Vector3(5f, 0f, 64f);
            trGo.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
            trGo.transform.localScale = Vector3.one * 1.0f;
        }

        // ── High-Visibility Airport Safety Cones Around Engines ──
        CreateSafetyCone(envRoot.transform, new Vector3(-18f, 0f, 66f));
        CreateSafetyCone(envRoot.transform, new Vector3(-16f, 0f, 69f));
        CreateSafetyCone(envRoot.transform, new Vector3(2f, 0f, 66f));
        CreateSafetyCone(envRoot.transform, new Vector3(4f, 0f, 69f));
        CreateSafetyCone(envRoot.transform, new Vector3(6.0f, 0f, 4.5f));

        // ── Dynamic Falling Rain Particle System ──
        var rainGo = new GameObject("ApronRainSystem");
        rainGo.transform.SetParent(envRoot.transform, false);
        rainGo.transform.position = new Vector3(0f, 22f, 10f);

        var ps = rainGo.AddComponent<ParticleSystem>();
        var psMain = ps.main;
        psMain.maxParticles = 3000;
        psMain.startLifetime = 1.2f;
        psMain.startSpeed = 22f;
        psMain.startSize = 0.04f;
        psMain.startColor = new Color(0.85f, 0.92f, 1.0f, 0.45f);
        psMain.simulationSpace = ParticleSystemSimulationSpace.World;
        psMain.gravityModifier = 1.0f;

        var psEmission = ps.emission;
        psEmission.rateOverTime = 1200f;

        var psShape = ps.shape;
        psShape.shapeType = ParticleSystemShapeType.Box;
        psShape.scale = new Vector3(90f, 0.5f, 90f);

        var psVel = ps.velocityOverLifetime;
        psVel.enabled = true;
        psVel.x = -2.5f;
        psVel.y = -20f;
        psVel.z = 1.5f;

        var psr = rainGo.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.cameraVelocityScale = 0.05f;
        psr.velocityScale = 0.08f;
        psr.lengthScale = 2.0f;
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Standard"));
        psr.material.color = new Color(0.85f, 0.92f, 1.0f, 0.40f);
        ps.Stop(); // Stopped by default until Rainy weather preset is chosen

        // ── AIRPORT SFX AUDIO MANAGER ──
        var sfxGo = new GameObject("AirportSFXManager");
        var sfx = sfxGo.AddComponent<AirportSFXManager>();
        sfx.apronAmbienceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Airport_Apron_Loop.wav");
        sfx.atcChatterClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ATC_Chatter_Ambiance.wav");
        sfx.rainLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Rain_Apron_Loop.wav");
        sfx.uiHoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI_Hover.wav");
        sfx.uiClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI_Click.wav");

        // ── AIRPORT WEATHER CONTROLLER ──
        var weatherGo = new GameObject("AirportWeatherController");
        var weatherCtrl = weatherGo.AddComponent<AirportWeatherController>();
        weatherCtrl.rainParticleSystem = ps;
        weatherCtrl.tarmacMaterial = tarmacInstance;
        weatherCtrl.timeController = tod;

        // ── Rich Background Airport Infrastructure & City Skyline ──
        CreateBackgroundAirportEnvironment(envRoot.transform, tod);

        // ═══════════════════════════════════════════════════
        //  TERMINAL BUILDING ARCHITECTURAL LIGHTING RIG
        // ═══════════════════════════════════════════════════
        var bldgLightingRoot = new GameObject("TerminalBuildingLightingRig");
        bldgLightingRoot.transform.SetParent(envRoot.transform, false);
        tod.buildingLightingRig = bldgLightingRoot.transform;

        // 1. Facade Ground Wash Floodlights (Aiming up at the glass facade & canopy)
        (string WashName, Vector3 WashPos, Vector3 WashRot, float WashRange, float WashAngle, float WashInt)[] washLights = {
            ("FacadeWash_Left",   new Vector3(38f, 0.5f, 78f), new Vector3(-28f, 22f, 0f), 120f, 85f, 16f),
            ("FacadeWash_Center", new Vector3(55f, 0.5f, 75f), new Vector3(-30f, 15f, 0f), 125f, 90f, 22f),
            ("FacadeWash_Right",  new Vector3(72f, 0.5f, 78f), new Vector3(-28f, 5f, 0f),  120f, 85f, 16f)
        };

        foreach (var w in washLights)
        {
            var wGo = new GameObject(w.WashName);
            wGo.transform.SetParent(bldgLightingRoot.transform, false);
            wGo.transform.position = w.WashPos;
            wGo.transform.rotation = Quaternion.Euler(w.WashRot);
            var wl = wGo.AddComponent<Light>();
            wl.type = LightType.Spot;
            wl.color = new Color(1.0f, 0.95f, 0.88f);
            wl.range = w.WashRange;
            wl.spotAngle = w.WashAngle;
            wl.intensity = w.WashInt;
            wl.shadows = LightShadows.Soft;
        }

        // 2. Concourse Interior Warm Window Glows (Simulates vibrant passenger concourse through glass)
        Vector3[] windowGlowPositions = {
            new Vector3(40f, 6.5f, 88f),
            new Vector3(48f, 7.0f, 90f),
            new Vector3(56f, 8.0f, 92f),
            new Vector3(64f, 7.0f, 90f),
            new Vector3(72f, 6.5f, 88f)
        };

        for (int i = 0; i < windowGlowPositions.Length; i++)
        {
            var gGo = new GameObject($"WindowGlow_{i + 1}");
            gGo.transform.SetParent(bldgLightingRoot.transform, false);
            gGo.transform.position = windowGlowPositions[i];
            var gl = gGo.AddComponent<Light>();
            gl.type = LightType.Point;
            gl.color = new Color(1.0f, 0.84f, 0.56f); // Warm glowing concourse interior (3000K)
            gl.range = 75f;
            gl.intensity = (i == 2) ? 14.0f : 10.0f; // Main hall is brightest
        }

        // 3. Roofline Architectural Neon Cyan Accent
        var roofAccentGo = new GameObject("RoofContour_Cyan");
        roofAccentGo.transform.SetParent(bldgLightingRoot.transform, false);
        roofAccentGo.transform.position = new Vector3(55f, 22f, 90f);
        var ral = roofAccentGo.AddComponent<Light>();
        ral.type = LightType.Point;
        ral.color = new Color(0.35f, 0.85f, 1.0f);
        ral.range = 55f;
        ral.intensity = 10f;

        // 4. Entrance Canopy Downlights
        Vector3[] canopyPositions = { new Vector3(46f, 5.0f, 79f), new Vector3(64f, 5.0f, 79f) };
        for (int i = 0; i < canopyPositions.Length; i++)
        {
            var cdGo = new GameObject($"CanopyDownlight_{i + 1}");
            cdGo.transform.SetParent(bldgLightingRoot.transform, false);
            cdGo.transform.position = canopyPositions[i];
            cdGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var cdl = cdGo.AddComponent<Light>();
            cdl.type = LightType.Spot;
            cdl.color = new Color(1.0f, 0.92f, 0.80f);
            cdl.range = 35f;
            cdl.spotAngle = 75f;
            cdl.intensity = 12f;
        }

        // ═══════════════════════════════════════════════════
        //  APRON HIGH-MAST FLOODLIGHT TOWERS & TAXIWAY LIGHTS
        // ═══════════════════════════════════════════════════
        var apronLightingRoot = new GameObject("ApronLightingRig");
        apronLightingRoot.transform.SetParent(envRoot.transform, false);
        tod.airportLightingRig = apronLightingRoot.transform;

        // Gate 12 West High-Mast Stanchion Tower (aimed right at Boeing 737 cockpit & engine!)
        CreateApronFloodlightMast(apronLightingRoot.transform, "ApronMast_Gate12",
            new Vector3(-18f, 0f, 14f),
            new Vector3(32f, 75f, 0f), 28f, 120f, 75f,
            new Vector3(42f, 115f, 0f), 22f, 100f, 80f);

        // Gate 14 East High-Mast Stanchion Tower
        CreateApronFloodlightMast(apronLightingRoot.transform, "ApronMast_Gate14",
            new Vector3(26f, 0f, 32f),
            new Vector3(36f, -110f, 0f), 25f, 110f, 75f,
            new Vector3(45f, -70f, 0f),  20f, 100f, 80f);

        // Taxiway Edge Lights (Aviation Blue) & Centerline Lights (Amber)
        CreateTaxiwayGuidanceLights(apronLightingRoot.transform);

        // ═══════════════════════════════════════════════════
        //  POST-PROCESSING GLOBAL VOLUME (ACES Tonemapping & Bloom)
        // ═══════════════════════════════════════════════════
        var postVolGo = new GameObject("PostProcessing_GlobalVolume");
        var postVol = postVolGo.AddComponent<UnityEngine.Rendering.Volume>();
        postVol.isGlobal = true;
        var existingProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(
            "Assets/VRTemplateAssets/Graphics/Post Process Volume Profile.asset");
        if (existingProfile != null)
        {
            postVol.profile = existingProfile;
        }

        // ═══════════════════════════════════════════════════
        //  3. CINEMATIC CAMERA RIG (Framing the Boeing 737)
        // ═══════════════════════════════════════════════════

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1500f;
        cam.fieldOfView = 48f;
        camGo.AddComponent<AudioListener>();

        // High quality SMAA Anti-Aliasing & Post-Processing
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camData.antialiasingQuality = AntialiasingQuality.High;
        camData.renderPostProcessing = true;
        camData.renderShadows = true;

        var camCtrl = camGo.AddComponent<MenuCinematicCameraController>();
        camCtrl.shots = new MenuCinematicCameraController.CameraShot[]
        {
            new MenuCinematicCameraController.CameraShot
            {
                shotName = "GATE 2 PANORAMIC VISTA",
                position = new Vector3(20.0f, 3.8f, 30.0f),
                eulerRotation = new Vector3(3.5f, -36.8f, 0f),
                fieldOfView = 50f
            },
            new MenuCinematicCameraController.CameraShot
            {
                shotName = "HERO APRON TURBINE & NOSE",
                position = new Vector3(-6.0f, 3.2f, 48.0f),
                eulerRotation = new Vector3(4.5f, 42.0f, 0f),
                fieldOfView = 48f
            },
            new MenuCinematicCameraController.CameraShot
            {
                shotName = "TERMINAL & APRON OVERVIEW",
                position = new Vector3(32.0f, 8.5f, 18.0f),
                eulerRotation = new Vector3(12.0f, -45.0f, 0f),
                fieldOfView = 55f
            }
        };
        camGo.transform.position = new Vector3(20.0f, 3.8f, 30.0f);
        camGo.transform.rotation = Quaternion.Euler(3.5f, -36.8f, 0f);

        // ═══════════════════════════════════════════════════
        //  4. UI CANVAS — HIGH-TECH CRISP AVIATION HUD
        // ═══════════════════════════════════════════════════

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 3.0f; // 3x Supersampling for razor-sharp text
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event System
        var eventSys = new GameObject("EventSystem");
        eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // Controllers
        var menuController = canvasGo.AddComponent<MainMenuController>();
        var settingsController = canvasGo.AddComponent<SettingsMenuController>();

        // ═══════════════════════════════════════════
        //  MAIN MENU PANEL (Left Glassmorphic Card HUD)
        // ═══════════════════════════════════════════

        var mainMenuPanel = MakeRect("MainMenuPanel", canvasGo.transform);
        StretchFill(mainMenuPanel);

        // ── Left Card Container (Clean glass card, NO screen-splitting hard cuts!) ──
        float leftMargin = 60f;
        float cardWidth = 520f;

        var hudCard = MakeRect("HudCard", mainMenuPanel.transform);
        SetAnchored(hudCard, new Vector2(0f, 0.5f), new Vector2(leftMargin + cardWidth * 0.5f, 0f), new Vector2(cardWidth, 880f));
        var cardImg = hudCard.AddComponent<Image>();
        cardImg.color = new Color(0.02f, 0.06f, 0.12f, 0.78f);

        // Card Border Line (Cyan glowing edge)
        var cardBorder = MakeRect("CardBorder", hudCard.transform);
        StretchFill(cardBorder);
        var cbImg = cardBorder.AddComponent<Image>();
        cbImg.color = new Color(0.18f, 0.88f, 0.98f, 0.25f);
        cbImg.raycastTarget = false;

        // ── Brand Title Block inside Card ──
        float contentY = 390f;

        // Live Apron Status Pill
        var liveBadge = MakeRect("LiveBadge", hudCard.transform);
        SetAnchored(liveBadge, new Vector2(0.5f, 0.5f), new Vector2(0f, contentY), new Vector2(280f, 32f));
        var lbImg = liveBadge.AddComponent<Image>();
        lbImg.color = new Color(0.04f, 0.14f, 0.26f, 0.90f);

        MakeTMP("LiveText", liveBadge.transform,
            "●  LIVE APRON  •  STAND 12  ACTIVE", 13f, FontStyles.Bold,
            new Color(0.25f, 1.0f, 0.50f), TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(270f, 28f), new Vector2(0.5f, 0.5f));

        contentY -= 55f;

        // Big Title: AEROSIM VR
        var titleTMP = MakeTMP("GameTitle", hudCard.transform,
            "AEROSIM VR", 48f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.Center,
            new Vector2(0f, contentY), new Vector2(480f, 58f), new Vector2(0.5f, 0.5f));
        titleTMP.characterSpacing = 3f;

        contentY -= 38f;

        // Subtitle
        MakeTMP("Tagline", hudCard.transform,
            "REAL-TIME AIRPORT & PASSENGER SIMULATION", 13f, FontStyles.Normal,
            new Color(0.65f, 0.82f, 0.95f, 0.95f), TextAlignmentOptions.Center,
            new Vector2(0f, contentY), new Vector2(480f, 24f), new Vector2(0.5f, 0.5f));

        contentY -= 25f;

        // Glowing Cyan Divider Line
        var hudLine = MakeRect("HudLine", hudCard.transform);
        hudLine.AddComponent<Image>().color = CyanGlow;
        SetAnchored(hudLine, new Vector2(0.5f, 0.5f), new Vector2(0f, contentY), new Vector2(440f, 1.5f));

        contentY -= 20f;

        // ── Departures Board Widget ──
        var deptWidget = MakeRect("DeparturesWidget", hudCard.transform);
        SetAnchored(deptWidget, new Vector2(0.5f, 0.5f), new Vector2(0f, contentY - 65f), new Vector2(460f, 130f));
        var dwImg = deptWidget.AddComponent<Image>();
        dwImg.color = new Color(0.02f, 0.05f, 0.10f, 0.90f);

        MakeTMP("DeptHeader", deptWidget.transform, "FLIGHT DEPARTURES  •  TERMINAL 1", 13f, FontStyles.Bold,
            CyanAccent, TextAlignmentOptions.TopLeft,
            new Vector2(16f, -12f), new Vector2(280f, 22f), new Vector2(0f, 1f));

        float savedHour = TimeOfDayController.GetSavedHour();
        MakeTMP("TimeDisplay", deptWidget.transform,
            TimeOfDayController.Format24HourClock(savedHour), 26f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.TopRight,
            new Vector2(-16f, -10f), new Vector2(110f, 32f), new Vector2(1f, 1f));

        string[] miniFlights = {
            "08:30   AI 101    New York (JFK)       GATE A12   ON TIME",
            "09:15   BA 217   London (LHR)         GATE B08   BOARDING",
            "10:05   EK 523   Dubai (DXB)            GATE A12   ON TIME"
        };
        for (int i = 0; i < miniFlights.Length; i++)
        {
            Color flightColor = miniFlights[i].Contains("BOARDING")
                ? new Color(0.25f, 1.0f, 0.50f, 1f)
                : new Color(0.80f, 0.90f, 1.0f, 0.95f);
            MakeTMP($"Flight{i}", deptWidget.transform,
                miniFlights[i], 12f, FontStyles.Normal, flightColor,
                TextAlignmentOptions.TopLeft,
                new Vector2(16f, -42f - i * 26f), new Vector2(430f, 22f),
                new Vector2(0f, 1f));
        }

        contentY -= 160f;

        // ── Main Action Buttons ──

        // START GAME button (Primary Cyan Glow)
        CreateMenuButton(hudCard.transform,
            "START SIMULATION", "Board aircraft & commence departure", ">",
            ButtonPrimary, new Vector2(0f, contentY), new Vector2(460f, 78f),
            true, menuController.StartGame);

        contentY -= 92f;

        // SETTINGS button (Dark Translucent)
        CreateMenuButton(hudCard.transform,
            "SETTINGS", "Flight time, 3D sun, audio & VR comfort", "#",
            ButtonDark, new Vector2(0f, contentY), new Vector2(460f, 72f),
            false, settingsController.OpenSettings);

        contentY -= 86f;

        // QUIT button
        CreateMenuButton(hudCard.transform,
            "QUIT SIMULATOR", "Exit application safely to desktop", "X",
            ButtonDark, new Vector2(0f, contentY), new Vector2(460f, 72f),
            false, menuController.QuitGame);

        // ═══════════════════════════════════════════
        //  BOTTOM HUD BAR
        // ═══════════════════════════════════════════

        var bottomBar = MakeRect("BottomBar", canvasGo.transform);
        bottomBar.AddComponent<Image>().color = new Color(0.02f, 0.05f, 0.10f, 0.85f);
        var bbRT = bottomBar.GetComponent<RectTransform>();
        bbRT.anchorMin = new Vector2(0f, 0f); bbRT.anchorMax = new Vector2(1f, 0f);
        bbRT.pivot = new Vector2(0.5f, 0f);
        bbRT.sizeDelta = new Vector2(0f, 48f);

        // Logo Pill (Bottom-Left)
        var logoPanel = MakeRect("LogoPanel", bottomBar.transform);
        var logoImg = logoPanel.AddComponent<Image>();
        logoImg.color = ButtonPrimary;
        var logoRT = logoPanel.GetComponent<RectTransform>();
        logoRT.anchorMin = new Vector2(0f, 0.5f); logoRT.anchorMax = new Vector2(0f, 0.5f);
        logoRT.pivot = new Vector2(0f, 0.5f);
        logoRT.anchoredPosition = new Vector2(25f, 0f);
        logoRT.sizeDelta = new Vector2(140f, 32f);

        MakeTMP("LogoText", logoPanel.transform, "AEROSIM VR", 14f, FontStyles.Bold,
            new Color(0.02f, 0.06f, 0.10f), TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(130f, 28f), new Vector2(0.5f, 0.5f));

        // Engine Status (Bottom-Center)
        MakeTMP("VersionInfo", bottomBar.transform,
            "v1.4.0  •  REAL-TIME 3D APRON  •  BOEING 737-800 SHOWCASE ACTIVE", 13f, FontStyles.Normal,
            VersionColor, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(650f, 28f), new Vector2(0.5f, 0.5f));

        // Camera Switcher Button (Bottom-Right)
        var camBtnGo = MakeRect("CameraSwitchBtn", bottomBar.transform);
        SetAnchored(camBtnGo, new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(240f, 34f));
        var camBtnImg = camBtnGo.AddComponent<Image>();
        camBtnImg.color = new Color(0.08f, 0.18f, 0.32f, 0.95f);
        var camBtn = camBtnGo.AddComponent<Button>();
        camBtn.targetGraphic = camBtnImg;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(camBtn.onClick, camCtrl.CycleCameraView);
        camBtnGo.AddComponent<UIButtonAudio>();

        MakeTMP("CamBtnText", camBtnGo.transform, "SWITCH VIEW  [ C ]", 12f, FontStyles.Bold,
            CyanAccent, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(240f, 34f), new Vector2(0.5f, 0.5f));

        // ═══════════════════════════════════════════
        //  SETTINGS MODAL PANEL (Floating Semi-Transparent)
        // ═══════════════════════════════════════════

        var settingsPanel = MakeRect("SettingsPanel", canvasGo.transform);
        StretchFill(settingsPanel);

        // Backdrop dimmer
        var settingsDimmer = MakeRect("BackdropDimmer", settingsPanel.transform);
        StretchFill(settingsDimmer);
        var dimImg = settingsDimmer.AddComponent<Image>();
        dimImg.color = new Color(0.01f, 0.02f, 0.05f, 0.65f);

        // Modal Box
        var modalBox = MakeRect("ModalBox", settingsPanel.transform);
        SetAnchored(modalBox, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 720f));
        var modalImg = modalBox.AddComponent<Image>();
        modalImg.color = DarkPanel;

        // Modal Border Outline
        var borderBox = MakeRect("ModalBorder", modalBox.transform);
        StretchFill(borderBox);
        var borderImg = borderBox.AddComponent<Image>();
        borderImg.color = ModalBorder;
        borderImg.raycastTarget = false;

        // Modal Header
        var modalHeader = MakeRect("ModalHeader", modalBox.transform);
        SetAnchored(modalHeader, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(720f, 60f));

        MakeTMP("ModalTitle", modalHeader.transform,
            "SIMULATION SETTINGS", 24f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 0f), new Vector2(500f, 34f), new Vector2(0f, 1f));

        MakeTMP("ModalSubtitle", modalHeader.transform,
            "Customize flight time, live 3D lighting, audio, and VR comfort", 14f, FontStyles.Normal,
            TextMuted, TextAlignmentOptions.TopLeft,
            new Vector2(0f, -34f), new Vector2(500f, 22f), new Vector2(0f, 1f));

        // Close 'X' Button
        CreateIconButton(modalBox.transform, "X",
            new Vector2(-15f, -15f), new Vector2(36f, 36f),
            settingsController.CloseSettings);

        // Content Area inside Modal
        float mY = -120f;

        // ── 1. TIME OF DAY CONTROLLER ──
        MakeTMP("TodLabel", modalBox.transform,
            "FLIGHT TIME / TIME OF DAY", 16f, FontStyles.Bold,
            CyanAccent, TextAlignmentOptions.TopLeft,
            new Vector2(-360f, mY), new Vector2(400f, 24f), new Vector2(0.5f, 1f));

        MakeTMP("TodSub", modalBox.transform,
            "Watch the live 3D sun, shadows, and airport lighting shift in real time", 13f, FontStyles.Normal,
            TextMuted, TextAlignmentOptions.TopLeft,
            new Vector2(-360f, mY - 24f), new Vector2(480f, 20f), new Vector2(0.5f, 1f));

        // Time Badge Display
        var timeBadgeGo = MakeRect("TimeBadge", modalBox.transform);
        SetAnchored(timeBadgeGo, new Vector2(0.5f, 1f), new Vector2(250f, mY - 6f), new Vector2(200f, 34f));
        var tbImg = timeBadgeGo.AddComponent<Image>();
        tbImg.color = new Color(0.08f, 0.20f, 0.36f, 0.90f);

        var timeBadgeTMP = MakeTMP("TimeBadgeText", timeBadgeGo.transform,
            TimeOfDayController.FormatTimeString(savedHour), 14f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(190f, 30f), new Vector2(0.5f, 0.5f));

        mY -= 65f;

        // Time Slider
        var timeSlider = CreateModernSlider(modalBox.transform, "TimeSlider",
            new Vector2(0f, mY), new Vector2(700f, 32f),
            6.0f, 20.5f, savedHour);

        mY -= 36f;

        // Quick Preset Buttons
        float presetBtnWidth = 165f;
        float presetGap = 12f;
        float totalPresetWidth = 4 * presetBtnWidth + 3 * presetGap;
        float presetStartX = -totalPresetWidth * 0.5f + presetBtnWidth * 0.5f;

        CreatePresetButton(modalBox.transform,
            "MORNING (08:30)",
            new Vector2(presetStartX + 0 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetMorningPreset);

        CreatePresetButton(modalBox.transform,
            "MIDDAY (12:00)",
            new Vector2(presetStartX + 1 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetNoonPreset);

        CreatePresetButton(modalBox.transform,
            "SUNSET (18:15)",
            new Vector2(presetStartX + 2 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetSunsetPreset);

        CreatePresetButton(modalBox.transform,
            "EVENING (20:00)",
            new Vector2(presetStartX + 3 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetEveningPreset);

        mY -= 55f;

        // ── 2. ATMOSPHERIC WEATHER SYSTEM ──
        MakeTMP("WeatherLabel", modalBox.transform,
            "ATMOSPHERIC WEATHER & ENVIRONMENT", 16f, FontStyles.Bold,
            CyanAccent, TextAlignmentOptions.TopLeft,
            new Vector2(-360f, mY), new Vector2(400f, 24f), new Vector2(0.5f, 1f));

        MakeTMP("WeatherSub", modalBox.transform,
            "Experience clear skies, golden-hour sunset, nocturnal apron lights, or stormy wet tarmac", 13f, FontStyles.Normal,
            TextMuted, TextAlignmentOptions.TopLeft,
            new Vector2(-360f, mY - 24f), new Vector2(550f, 20f), new Vector2(0.5f, 1f));

        mY -= 48f;

        CreatePresetButton(modalBox.transform,
            "CLEAR SKY ☀️",
            new Vector2(presetStartX + 0 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetWeatherClear);

        CreatePresetButton(modalBox.transform,
            "SUNSET 🌅",
            new Vector2(presetStartX + 1 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetWeatherSunset);

        CreatePresetButton(modalBox.transform,
            "NIGHT 🌙",
            new Vector2(presetStartX + 2 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetWeatherNight);

        CreatePresetButton(modalBox.transform,
            "RAINY STORM 🌧️",
            new Vector2(presetStartX + 3 * (presetBtnWidth + presetGap), mY),
            new Vector2(presetBtnWidth, 34f),
            settingsController.SetWeatherRain);

        mY -= 55f;

        // Divider Line
        var modalLine = MakeRect("ModalLine1", modalBox.transform);
        modalLine.AddComponent<Image>().color = new Color(0.20f, 0.85f, 0.95f, 0.25f);
        SetAnchored(modalLine, new Vector2(0.5f, 1f), new Vector2(0f, mY), new Vector2(700f, 1f));

        mY -= 25f;

        // ── 3. MASTER AUDIO VOLUME ──
        MakeTMP("VolLabel", modalBox.transform,
            "MASTER VOLUME", 16f, FontStyles.Bold,
            CyanAccent, TextAlignmentOptions.TopLeft,
            new Vector2(-360f, mY), new Vector2(300f, 24f), new Vector2(0.5f, 1f));

        float savedVol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        var volValTMP = MakeTMP("VolValue", modalBox.transform,
            $"{Mathf.RoundToInt(savedVol * 100f)}%", 16f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.TopRight,
            new Vector2(350f, mY), new Vector2(100f, 24f), new Vector2(0.5f, 1f));

        mY -= 38f;

        var volSlider = CreateModernSlider(modalBox.transform, "VolumeSlider",
            new Vector2(0f, mY), new Vector2(700f, 30f),
            0f, 1f, savedVol);

        mY -= 55f;

        // Divider Line
        var modalLine2 = MakeRect("ModalLine2", modalBox.transform);
        modalLine2.AddComponent<Image>().color = new Color(0.20f, 0.85f, 0.95f, 0.25f);
        SetAnchored(modalLine2, new Vector2(0.5f, 1f), new Vector2(0f, mY), new Vector2(700f, 1f));

        mY -= 25f;

        // ── 3. VR COMFORT TURNING ──
        MakeTMP("ComfortLabel", modalBox.transform,
            "VR COMFORT TURNING", 16f, FontStyles.Bold,
            CyanAccent, TextAlignmentOptions.TopLeft,
            new Vector2(-360f, mY), new Vector2(300f, 24f), new Vector2(0.5f, 1f));

        bool savedComfort = PlayerPrefs.GetInt("VR_Comfort", 1) == 1;
        var comfortValTMP = MakeTMP("ComfortStatus", modalBox.transform,
            savedComfort ? "ENABLED (Snap Turning)" : "DISABLED (Smooth Turning)", 13f, FontStyles.Normal,
            TextMuted, TextAlignmentOptions.TopLeft,
            new Vector2(-360f, mY - 24f), new Vector2(400f, 20f), new Vector2(0.5f, 1f));

        var toggleGo = MakeRect("ComfortToggle", modalBox.transform);
        SetAnchored(toggleGo, new Vector2(0.5f, 1f), new Vector2(335f, mY - 6f), new Vector2(50f, 26f));
        var toggleBg = toggleGo.AddComponent<Image>();
        toggleBg.color = savedComfort ? ButtonPrimary : new Color(0.15f, 0.25f, 0.35f, 1f);
        var toggle = toggleGo.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBg;
        toggle.isOn = savedComfort;

        // Apply & Close Button (Centered at bottom of modal)
        CreateActionButton(modalBox.transform,
            "APPLY & CLOSE",
            new Vector2(0f, 35f), new Vector2(260f, 50f),
            ButtonPrimary, settingsController.CloseSettings);

        // Connect references to SettingsMenuController
        settingsController.mainMenuPanel = mainMenuPanel;
        settingsController.settingsPanel = settingsPanel;
        settingsController.timeOfDaySlider = timeSlider;
        settingsController.timeOfDayText = timeBadgeTMP;
        settingsController.volumeSlider = volSlider;
        settingsController.volumeText = volValTMP;
        settingsController.comfortToggle = toggle;
        settingsController.comfortStatusText = comfortValTMP;

        settingsPanel.SetActive(false);

        // ═══════════════════════════════════════════
        //  LOADING OVERLAY
        // ═══════════════════════════════════════════

        var loadingOverlay = MakeRect("LoadingOverlay", canvasGo.transform);
        StretchFill(loadingOverlay);
        var loImg = loadingOverlay.AddComponent<Image>();
        loImg.color = new Color(0.01f, 0.03f, 0.08f, 0.98f);

        // Spinner / Icon
        var spinnerGo = MakeRect("SpinnerIcon", loadingOverlay.transform);
        SetAnchored(spinnerGo, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(70f, 70f));
        var spinImg = spinnerGo.AddComponent<Image>();
        spinImg.color = CyanAccent;
        var spinnerRT = spinnerGo.GetComponent<RectTransform>();

        // Loading Text
        var loadTxtTMP = MakeTMP("LoadingText", loadingOverlay.transform,
            "COMMENCING DEPARTURE SIMULATION...", 18f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.Center,
            new Vector2(0f, -30f), new Vector2(600f, 30f), new Vector2(0.5f, 0.5f));

        // Progress Bar
        var progressBg = MakeRect("ProgressBarBg", loadingOverlay.transform);
        SetAnchored(progressBg, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(500f, 8f));
        progressBg.AddComponent<Image>().color = new Color(0.10f, 0.18f, 0.28f, 1f);

        var progressFillGo = MakeRect("ProgressBarFill", progressBg.transform);
        var fillImg = progressFillGo.AddComponent<Image>();
        fillImg.color = CyanAccent;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;
        StretchFill(progressFillGo);

        // Connect references to MainMenuController
        menuController.loadingPanel = loadingOverlay;
        menuController.progressBarFill = fillImg;
        menuController.loadingText = loadTxtTMP;
        menuController.spinnerIcon = spinnerRT;

        loadingOverlay.SetActive(false);

        // Save Scene
        if (!System.IO.Directory.Exists("Assets/Scenes"))
            System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(currentScene);
        bool saved = EditorSceneManager.SaveScene(currentScene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[AeroSim VR] Live Rendered 3D Main Menu created at " + scenePath + " (Saved: " + saved + ")");
    }

    // ═══════════════════════════════════════════════════
    //  UI BUILDER HELPERS
    // ═══════════════════════════════════════════════════

    static void CreateMenuButton(Transform parent,
        string title, string subtitle, string iconChar,
        Color bgColor, Vector2 centerPos, Vector2 size,
        bool isPrimary, UnityEngine.Events.UnityAction action)
    {
        string goName = title.Replace(" ", "") + "Btn";
        var btnGo = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);

        var img = btnGo.GetComponent<Image>();
        img.color = bgColor;

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = isPrimary
            ? new Color(1.15f, 1.15f, 1.15f)
            : new Color(1.30f, 1.35f, 1.45f);
        cb.pressedColor = new Color(0.7f, 0.85f, 1f);
        btn.colors = cb;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
        btnGo.AddComponent<UIButtonAudio>();

        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = centerPos;
        rt.sizeDelta = size;

        // Icon Box
        var iconBg = MakeRect("IconBg", btnGo.transform);
        var iconBgImg = iconBg.AddComponent<Image>();
        iconBgImg.color = isPrimary
            ? new Color(0.04f, 0.50f, 0.60f, 0.65f)
            : new Color(0.12f, 0.22f, 0.35f, 0.90f);
        iconBgImg.raycastTarget = false;
        var iconBgRT = iconBg.GetComponent<RectTransform>();
        iconBgRT.anchorMin = new Vector2(0f, 0.5f); iconBgRT.anchorMax = new Vector2(0f, 0.5f);
        iconBgRT.pivot = new Vector2(0f, 0.5f);
        iconBgRT.anchoredPosition = new Vector2(18f, 0f);
        iconBgRT.sizeDelta = new Vector2(46f, 46f);

        MakeTMP("Icon", iconBg.transform, iconChar, 20f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(44f, 44f), new Vector2(0.5f, 0.5f));

        // Title text
        MakeTMP("Title", btnGo.transform, title, 20f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.TopLeft,
            new Vector2(80f, 10f), new Vector2(320f, 28f), new Vector2(0f, 0.5f));

        // Subtitle text
        MakeTMP("Subtitle", btnGo.transform, subtitle, 12f, FontStyles.Normal,
            TextMuted, TextAlignmentOptions.TopLeft,
            new Vector2(80f, -14f), new Vector2(320f, 20f), new Vector2(0f, 0.5f));

        // Chevron >
        var chev = MakeTMP("Chevron", btnGo.transform, ">", 24f, FontStyles.Bold,
            isPrimary ? TextWhite : TextMuted, TextAlignmentOptions.Center,
            new Vector2(-18f, 0f), new Vector2(24f, 32f), new Vector2(1f, 0.5f));
        var chevRT = chev.GetComponent<RectTransform>();
        chevRT.anchorMin = chevRT.anchorMax = new Vector2(1f, 0.5f);
        chevRT.pivot = new Vector2(1f, 0.5f);
        chevRT.anchoredPosition = new Vector2(-18f, 0f);
    }

    static Slider CreateModernSlider(Transform parent, string name, Vector2 pos, Vector2 size, float minVal, float maxVal, float defaultVal)
    {
        var sliderGo = MakeRect(name, parent);
        SetAnchored(sliderGo, new Vector2(0.5f, 1f), pos, size);

        // Background Rail
        var bgGo = MakeRect("Background", sliderGo.transform);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.16f, 0.28f, 1f);
        var bgRT = bgGo.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.35f); bgRT.anchorMax = new Vector2(1f, 0.65f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Fill Area
        var fillAreaGo = MakeRect("Fill Area", sliderGo.transform);
        var faRT = fillAreaGo.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.35f); faRT.anchorMax = new Vector2(1f, 0.65f);
        faRT.offsetMin = new Vector2(5f, 0f); faRT.offsetMax = new Vector2(-5f, 0f);

        var fillGo = MakeRect("Fill", fillAreaGo.transform);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = CyanAccent;
        var fillRT = fillGo.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Handle Area
        var handleAreaGo = MakeRect("Handle Slide Area", sliderGo.transform);
        var haRT = handleAreaGo.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10f, 0f); haRT.offsetMax = new Vector2(-10f, 0f);

        var handleGo = MakeRect("Handle", handleAreaGo.transform);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = TextWhite;
        var handleRT = handleGo.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(24f, 24f);

        var slider = sliderGo.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = minVal;
        slider.maxValue = maxVal;
        slider.value = defaultVal;

        return slider;
    }

    static Button CreatePresetButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var btnGo = MakeRect("PresetBtn", parent);
        SetAnchored(btnGo, new Vector2(0.5f, 1f), pos, size);
        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.06f, 0.14f, 0.24f, 0.90f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.12f, 0.35f, 0.55f, 1f);
        cb.pressedColor = CyanAccent;
        btn.colors = cb;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
        btnGo.AddComponent<UIButtonAudio>();

        MakeTMP("PresetTxt", btnGo.transform, label, 12f, FontStyles.Bold,
            CyanAccent, TextAlignmentOptions.Center,
            Vector2.zero, size, new Vector2(0.5f, 0.5f));

        return btn;
    }

    static Button CreateActionButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction action)
    {
        var btnGo = MakeRect("ActionBtn", parent);
        SetAnchored(btnGo, new Vector2(0.5f, 0f), pos, size);
        var img = btnGo.AddComponent<Image>();
        img.color = bgColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.2f);
        cb.pressedColor = new Color(0.7f, 0.9f, 1f);
        btn.colors = cb;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
        btnGo.AddComponent<UIButtonAudio>();

        MakeTMP("BtnLabel", btnGo.transform, label, 16f, FontStyles.Bold,
            new Color(0.02f, 0.06f, 0.10f), TextAlignmentOptions.Center,
            Vector2.zero, size, new Vector2(0.5f, 0.5f));

        return btn;
    }

    static Button CreateIconButton(Transform parent, string icon, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var btnGo = MakeRect("IconBtn", parent);
        SetAnchored(btnGo, new Vector2(1f, 1f), pos, size);
        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.12f, 0.22f, 0.35f, 0.85f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.3f, 1.4f);
        cb.pressedColor = CyanAccent;
        btn.colors = cb;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
        btnGo.AddComponent<UIButtonAudio>();

        MakeTMP("IconTxt", btnGo.transform, icon, 18f, FontStyles.Bold,
            TextWhite, TextAlignmentOptions.Center,
            Vector2.zero, size, new Vector2(0.5f, 0.5f));

        return btn;
    }

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFill(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchored(GameObject go, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent,
        string text, float fontSize, FontStyles style,
        Color color, TextAlignmentOptions alignment,
        Vector2 pos, Vector2 size, Vector2 anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return tmp;
    }

    static void CreateApronFloodlightMast(Transform parent, string mastName, Vector3 basePos,
        Vector3 rotA, float intA, float rangeA, float angleA,
        Vector3 rotB, float intB, float rangeB, float angleB)
    {
        var mastGo = new GameObject(mastName);
        mastGo.transform.SetParent(parent, false);
        mastGo.transform.position = basePos;

        // Physical Pole (Cylinder)
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "StanchionPole";
        pole.transform.SetParent(mastGo.transform, false);
        pole.transform.localPosition = new Vector3(0f, 8f, 0f);
        pole.transform.localScale = new Vector3(0.5f, 8f, 0.5f);
        var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        poleMat.color = new Color(0.18f, 0.22f, 0.28f);
        poleMat.SetFloat("_Smoothness", 0.35f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = poleMat;
        Object.DestroyImmediate(pole.GetComponent<Collider>());

        // Top Crossbar (Cube)
        var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossbar.name = "CrossbarBracket";
        crossbar.transform.SetParent(mastGo.transform, false);
        crossbar.transform.localPosition = new Vector3(0f, 16f, 0f);
        crossbar.transform.localScale = new Vector3(2.8f, 0.4f, 0.6f);
        crossbar.GetComponent<MeshRenderer>().sharedMaterial = poleMat;
        Object.DestroyImmediate(crossbar.GetComponent<Collider>());

        // Fixture A
        var fixAGo = new GameObject("Floodlight_A");
        fixAGo.transform.SetParent(mastGo.transform, false);
        fixAGo.transform.localPosition = new Vector3(-0.9f, 15.8f, 0f);
        fixAGo.transform.rotation = Quaternion.Euler(rotA);
        var lightA = fixAGo.AddComponent<Light>();
        lightA.type = LightType.Spot;
        lightA.color = new Color(1.0f, 0.95f, 0.88f);
        lightA.range = rangeA;
        lightA.spotAngle = angleA;
        lightA.intensity = intA;
        lightA.shadows = LightShadows.Soft;

        // Fixture B
        var fixBGo = new GameObject("Floodlight_B");
        fixBGo.transform.SetParent(mastGo.transform, false);
        fixBGo.transform.localPosition = new Vector3(0.9f, 15.8f, 0f);
        fixBGo.transform.rotation = Quaternion.Euler(rotB);
        var lightB = fixBGo.AddComponent<Light>();
        lightB.type = LightType.Spot;
        lightB.color = new Color(0.95f, 0.98f, 1.0f);
        lightB.range = rangeB;
        lightB.spotAngle = angleB;
        lightB.intensity = intB;
        lightB.shadows = LightShadows.Soft;
    }

    static void CreateTaxiwayGuidanceLights(Transform parent)
    {
        var guidanceRoot = new GameObject("TaxiwayGuidanceLights");
        guidanceRoot.transform.SetParent(parent, false);

        var blueMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        blueMat.color = new Color(0.15f, 0.55f, 1.0f);
        blueMat.EnableKeyword("_EMISSION");
        blueMat.SetColor("_EmissionColor", new Color(0.15f, 0.55f, 1.0f) * 2.5f);

        var amberMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        amberMat.color = new Color(1.0f, 0.75f, 0.15f);
        amberMat.EnableKeyword("_EMISSION");
        amberMat.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.15f) * 2.0f);

        // 1. Aviation Blue Edge Lights along Apron Boundary
        Vector3[] bluePositions = {
            new Vector3(-20f, 0.1f, -12f),
            new Vector3(-14f, 0.1f, -4f),
            new Vector3(-8f, 0.1f, 4f),
            new Vector3(-2f, 0.1f, 12f),
            new Vector3(4f, 0.1f, 20f),
            new Vector3(10f, 0.1f, 28f)
        };

        for (int i = 0; i < bluePositions.Length; i++)
        {
            var pGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pGo.name = $"TaxiEdge_Blue_{i + 1}";
            pGo.transform.SetParent(guidanceRoot.transform, false);
            pGo.transform.position = bluePositions[i];
            pGo.transform.localScale = new Vector3(0.25f, 0.15f, 0.25f);
            pGo.GetComponent<MeshRenderer>().sharedMaterial = blueMat;
            Object.DestroyImmediate(pGo.GetComponent<Collider>());

            var l = pGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.18f, 0.55f, 1.0f);
            l.range = 7.0f;
            l.intensity = 3.5f;
        }

        // 2. Amber Centerline Lights embedded along Yellow Lead-In Stripe
        Vector3[] amberPositions = {
            new Vector3(-2.0f, 0.05f, -6f),
            new Vector3(-2.0f, 0.05f, 0f),
            new Vector3(-2.0f, 0.05f, 6f),
            new Vector3(-2.0f, 0.05f, 12f)
        };

        for (int i = 0; i < amberPositions.Length; i++)
        {
            var aGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aGo.name = $"TaxiCenter_Amber_{i + 1}";
            aGo.transform.SetParent(guidanceRoot.transform, false);
            aGo.transform.position = amberPositions[i];
            aGo.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f);
            aGo.GetComponent<MeshRenderer>().sharedMaterial = amberMat;
            Object.DestroyImmediate(aGo.GetComponent<Collider>());

            var l = aGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.75f, 0.18f);
            l.range = 5.0f;
            l.intensity = 3.0f;
        }
    }

    private static void CreateSafetyCone(Transform parent, Vector3 pos)
    {
        var cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = "Apron_SafetyCone";
        cone.transform.SetParent(parent, false);
        cone.transform.position = pos + new Vector3(0f, 0.40f, 0f);
        cone.transform.localScale = new Vector3(0.32f, 0.40f, 0.32f);

        var coneMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        coneMat.color = new Color(1.0f, 0.35f, 0.05f); // High-viz airport orange
        coneMat.SetFloat("_Smoothness", 0.60f);
        cone.GetComponent<MeshRenderer>().sharedMaterial = coneMat;
        Object.DestroyImmediate(cone.GetComponent<Collider>());

        var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Reflective_Band";
        band.transform.SetParent(cone.transform, false);
        band.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        band.transform.localScale = new Vector3(1.06f, 0.25f, 1.06f);
        var bandMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        bandMat.color = Color.white;
        bandMat.SetFloat("_Smoothness", 0.90f);
        band.GetComponent<MeshRenderer>().sharedMaterial = bandMat;
        Object.DestroyImmediate(band.GetComponent<Collider>());
    }

    private static void UpgradeAircraftMaterials(GameObject planeGo)
    {
        var liveryTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/textures/Boeing737_UltraReal_Livery.png");
        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        var fuselageMat = new Material(litShader);
        fuselageMat.name = "Mat_Boeing737_UltraReal_Fuselage";
        if (liveryTex != null)
        {
            fuselageMat.mainTexture = liveryTex;
            fuselageMat.SetTexture("_BaseMap", liveryTex);
        }
        fuselageMat.SetColor("_BaseColor", Color.white);
        fuselageMat.SetFloat("_Metallic", 0.22f);
        fuselageMat.SetFloat("_Smoothness", 0.88f); // Gloss aerospace clearcoat reflection

        var cockpitGlassMat = new Material(litShader);
        cockpitGlassMat.name = "Mat_Boeing737_CockpitGlass";
        cockpitGlassMat.SetColor("_BaseColor", new Color(0.06f, 0.08f, 0.12f, 1.0f));
        cockpitGlassMat.SetFloat("_Metallic", 0.40f);
        cockpitGlassMat.SetFloat("_Smoothness", 0.96f);

        var chromeLipMat = new Material(litShader);
        chromeLipMat.name = "Mat_Boeing737_ChromeEngineLip";
        chromeLipMat.SetColor("_BaseColor", new Color(0.92f, 0.94f, 0.97f, 1.0f));
        chromeLipMat.SetFloat("_Metallic", 0.95f);
        chromeLipMat.SetFloat("_Smoothness", 0.88f);

        var planeRenderers = planeGo.GetComponentsInChildren<Renderer>(true);
        foreach (var pr in planeRenderers)
        {
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            pr.receiveShadows = true;

            var mats = pr.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;
                string mName = mats[m].name.ToLower();

                if (mName.Contains("material.003") || pr.name.ToLower().Contains("door") || pr.name.Contains("Boeing 737"))
                {
                    mats[m] = fuselageMat;
                }
                else if (mName.Contains("material.001") || mName.Contains("material.002"))
                {
                    mats[m] = cockpitGlassMat;
                }
                else if (mName.Contains("material.017") || mName.Contains("chrome"))
                {
                    mats[m] = chromeLipMat;
                }
                else if (mats[m].shader != null)
                {
                    var upgradedMat = new Material(mats[m]);
                    upgradedMat.shader = litShader;
                    upgradedMat.SetFloat("_Smoothness", 0.80f);
                    mats[m] = upgradedMat;
                }
            }
            pr.sharedMaterials = mats;
        }
    }

    // ═══════════════════════════════════════════════════
    //  REALISTIC ICAO APRON TARMAC MARKINGS
    // ═══════════════════════════════════════════════════
    private static void CreateRealisticApronMarkings(Transform root)
    {
        var markingsGo = new GameObject("Apron_ICAO_Markings");
        markingsGo.transform.SetParent(root, false);

        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // High-contrast Aviation Standard PBR Markings
        var yellowMat = new Material(litShader) { name = "Mat_ICAO_AviationYellow" };
        yellowMat.color = new Color(0.98f, 0.78f, 0.08f, 1f); // FAA/ICAO Chrome Yellow
        yellowMat.SetFloat("_Smoothness", 0.35f);

        var blackBorderMat = new Material(litShader) { name = "Mat_ICAO_BlackBorder" };
        blackBorderMat.color = new Color(0.10f, 0.10f, 0.12f, 1f); // Contrast outline
        blackBorderMat.SetFloat("_Smoothness", 0.25f);

        var whiteMat = new Material(litShader) { name = "Mat_ICAO_HighwayWhite" };
        whiteMat.color = new Color(0.94f, 0.96f, 0.98f, 1f);
        whiteMat.SetFloat("_Smoothness", 0.35f);

        var redHazardMat = new Material(litShader) { name = "Mat_ICAO_SafetyRed" };
        redHazardMat.color = new Color(0.88f, 0.12f, 0.10f, 1f); // Safety Red
        redHazardMat.SetFloat("_Smoothness", 0.35f);

        var darkPadMat = new Material(litShader) { name = "Mat_Tarmac_GroundPad" };
        darkPadMat.color = new Color(0.08f, 0.08f, 0.10f, 0.98f);
        darkPadMat.SetFloat("_Smoothness", 0.20f);

        // ── 1. Curved Taxiway Lead-In Centerline with Black Contrast Edges ──
        Vector3[] curvePoints = new Vector3[]
        {
            new Vector3(22.0f, 0.025f, 22.0f),
            new Vector3(12.0f, 0.025f, 26.5f),
            new Vector3(2.0f, 0.025f, 31.0f),
            new Vector3(-5.0f, 0.025f, 36.0f),
            new Vector3(-9.5f, 0.025f, 41.5f),
            new Vector3(-11.5f, 0.025f, 46.5f),
            new Vector3(-12.8f, 0.025f, 50.0f) // Nosewheel stop bar
        };

        for (int i = 0; i < curvePoints.Length - 1; i++)
        {
            Vector3 pA = curvePoints[i];
            Vector3 pB = curvePoints[i + 1];
            Vector3 mid = (pA + pB) * 0.5f;
            Vector3 dir = pB - pA;
            float segLen = dir.magnitude;
            Quaternion rot = Quaternion.LookRotation(dir.normalized);

            // Black contrast border
            CreateTarmacStripe(markingsGo.transform, "LeadIn_BlackBorder_" + i,
                mid + Vector3.down * 0.005f, rot, new Vector3(0.60f, 0.01f, segLen), blackBorderMat);

            // Yellow aviation lead-in line
            CreateTarmacStripe(markingsGo.transform, "LeadIn_YellowLine_" + i,
                mid, rot, new Vector3(0.30f, 0.015f, segLen), yellowMat);
        }

        // Straight stand centerline running beneath the fuselage
        Vector3 cA = new Vector3(-12.8f, 0.025f, 50.0f);
        Vector3 cB = new Vector3(-6.2f, 0.025f, 75.0f);
        Vector3 cMid = (cA + cB) * 0.5f;
        Vector3 cDir = cB - cA;
        Quaternion cRot = Quaternion.LookRotation(cDir.normalized);

        CreateTarmacStripe(markingsGo.transform, "Stand_Centerline_Border",
            cMid + Vector3.down * 0.005f, cRot, new Vector3(0.60f, 0.01f, cDir.magnitude), blackBorderMat);
        CreateTarmacStripe(markingsGo.transform, "Stand_Centerline_Yellow",
            cMid, cRot, new Vector3(0.30f, 0.015f, cDir.magnitude), yellowMat);

        // ── 2. Aircraft Nosewheel Stop Bars (ICAO Pitch Marks) ──
        Vector3 stopA350Pos = new Vector3(-12.8f, 0.035f, 50.0f);
        Quaternion stopRot = Quaternion.Euler(0f, 105f, 0f); // Perpendicular to heading 195°
        CreateTarmacStripe(markingsGo.transform, "StopBar_A350_Border",
            stopA350Pos + Vector3.down * 0.005f, stopRot, new Vector3(0.70f, 0.015f, 5.0f), blackBorderMat);
        CreateTarmacStripe(markingsGo.transform, "StopBar_A350_Yellow",
            stopA350Pos, stopRot, new Vector3(0.40f, 0.02f, 4.8f), yellowMat);

        // Stop Bar Label text on the ground
        CreateTarmacText(markingsGo.transform, "Label_Stop_A350",
            stopA350Pos + new Vector3(-2.8f, 0.005f, -0.6f), stopRot,
            "A350 / B787 / B777", 10f, yellowMat.color);

        // Secondary Stop Bar for A320 / B737
        Vector3 stopA320Pos = new Vector3(-12.0f, 0.035f, 53.0f);
        CreateTarmacStripe(markingsGo.transform, "StopBar_A320_Border",
            stopA320Pos + Vector3.down * 0.005f, stopRot, new Vector3(0.55f, 0.015f, 4.0f), blackBorderMat);
        CreateTarmacStripe(markingsGo.transform, "StopBar_A320_Yellow",
            stopA320Pos, stopRot, new Vector3(0.32f, 0.02f, 3.8f), yellowMat);

        CreateTarmacText(markingsGo.transform, "Label_Stop_A320",
            stopA320Pos + new Vector3(-2.4f, 0.005f, -0.5f), stopRot,
            "A320 / B737", 8.5f, yellowMat.color);

        // Stop command
        CreateTarmacText(markingsGo.transform, "Label_STOP_Command",
            stopA350Pos + new Vector3(0f, 0.005f, -1.8f), stopRot,
            "STOP", 11f, Color.white);

        // ── 3. Stand Identification Ground Marking (Yellow on Black Pad) ──
        Vector3 standPadPos = new Vector3(-10.2f, 0.028f, 42.0f);
        CreateTarmacStripe(markingsGo.transform, "Stand_Pad_Backing",
            standPadPos, Quaternion.Euler(0f, 15f, 0f), new Vector3(4.5f, 0.01f, 2.5f), darkPadMat);

        CreateBoxOutline(markingsGo.transform, "Stand_Pad_Border",
            standPadPos, Quaternion.Euler(0f, 15f, 0f), 4.5f, 2.5f, 0.15f, yellowMat);

        CreateTarmacText(markingsGo.transform, "Stand_ID_Text",
            standPadPos + Vector3.up * 0.005f, Quaternion.Euler(90f, 15f, 0f),
            "STAND 02", 14f, yellowMat.color);

        // ── 4. Aircraft Safety Clearance Envelope (The "Red Box") ──
        Vector3 safetyBoxCenter = new Vector3(-9.0f, 0.028f, 69.5f);
        float boxWidth = 46.0f;
        float boxLength = 50.0f;
        Quaternion boxRot = Quaternion.Euler(0f, 15f, 0f);

        CreateBoxOutline(markingsGo.transform, "Aircraft_Safety_RedBox",
            safetyBoxCenter, boxRot, boxWidth, boxLength, 0.28f, redHazardMat);

        // Corner safety marks
        Vector3[] corners = new Vector3[]
        {
            safetyBoxCenter + boxRot * new Vector3(-boxWidth*0.5f, 0f, -boxLength*0.5f),
            safetyBoxCenter + boxRot * new Vector3(boxWidth*0.5f, 0f, -boxLength*0.5f),
            safetyBoxCenter + boxRot * new Vector3(-boxWidth*0.5f, 0f, boxLength*0.5f),
            safetyBoxCenter + boxRot * new Vector3(boxWidth*0.5f, 0f, boxLength*0.5f)
        };
        for (int i = 0; i < corners.Length; i++)
        {
            CreateTarmacStripe(markingsGo.transform, "Safety_CornerMarker_" + i,
                corners[i], boxRot, new Vector3(2.5f, 0.012f, 2.5f), redHazardMat);
        }

        CreateTarmacText(markingsGo.transform, "Label_SafetyZone",
            safetyBoxCenter + boxRot * new Vector3(0f, 0.005f, -boxLength*0.5f + 1.2f),
            Quaternion.Euler(90f, 15f, 0f), "AIRCRAFT STAND SAFETY ZONE - NO GSE PARKING", 8.0f, whiteMat.color);

        // ── 5. Apron Ground Service Vehicle Road (White Zipper Markings) ──
        Vector3 roadCenter = new Vector3(-5.0f, 0.026f, 35.5f);
        float roadLen = 80.0f;
        float roadWidth = 6.8f;
        Quaternion roadRot = Quaternion.Euler(0f, 102f, 0f);

        float dashSpacing = 1.6f;
        int dashCount = Mathf.RoundToInt(roadLen / dashSpacing);
        for (int i = -dashCount / 2; i <= dashCount / 2; i++)
        {
            float offsetAlong = i * dashSpacing;

            float leftOffsetZ = (i % 2 == 0) ? -roadWidth * 0.5f : (-roadWidth * 0.5f + 0.35f);
            Vector3 leftDashPos = roadCenter + roadRot * new Vector3(leftOffsetZ, 0f, offsetAlong);
            CreateTarmacStripe(markingsGo.transform, "Zipper_L_" + i,
                leftDashPos, roadRot, new Vector3(0.32f, 0.01f, 0.85f), whiteMat);

            float rightOffsetZ = (i % 2 == 0) ? roadWidth * 0.5f : (roadWidth * 0.5f - 0.35f);
            Vector3 rightDashPos = roadCenter + roadRot * new Vector3(rightOffsetZ, 0f, offsetAlong);
            CreateTarmacStripe(markingsGo.transform, "Zipper_R_" + i,
                rightDashPos, roadRot, new Vector3(0.32f, 0.01f, 0.85f), whiteMat);

            if (i % 2 == 0)
            {
                Vector3 centerDashPos = roadCenter + roadRot * new Vector3(0f, 0f, offsetAlong);
                CreateTarmacStripe(markingsGo.transform, "Road_CenterDash_" + i,
                    centerDashPos, roadRot, new Vector3(0.18f, 0.01f, 1.0f), whiteMat);
            }
        }

        // Roadway Speed Limit "20"
        Vector3 speedSignPos = roadCenter + roadRot * new Vector3(1.6f, 0.005f, 4.0f);
        CreateTarmacStripe(markingsGo.transform, "Speed_BackingCircle",
            speedSignPos, roadRot, new Vector3(2.6f, 0.01f, 2.6f), darkPadMat);
        CreateBoxOutline(markingsGo.transform, "Speed_CircleBorder",
            speedSignPos, roadRot, 2.6f, 2.6f, 0.16f, whiteMat);
        CreateTarmacText(markingsGo.transform, "Speed_Limit_Text",
            speedSignPos + Vector3.up * 0.005f, Quaternion.Euler(90f, 12f, 0f),
            "20", 16f, whiteMat.color);

        CreateTarmacText(markingsGo.transform, "Road_Arrow_East",
            roadCenter + roadRot * new Vector3(-1.6f, 0.005f, -8.0f), Quaternion.Euler(90f, 102f, 0f),
            ">>", 12f, whiteMat.color);
        CreateTarmacText(markingsGo.transform, "Road_Arrow_West",
            roadCenter + roadRot * new Vector3(1.6f, 0.005f, 16.0f), Quaternion.Euler(90f, 282f, 0f),
            ">>", 12f, whiteMat.color);

        // ── 6. GSE Equipment Staging Bays ──
        Vector3 gseBayPos = new Vector3(8.5f, 0.026f, 60.0f);
        CreateBoxOutline(markingsGo.transform, "GSE_StagingBay_01",
            gseBayPos, Quaternion.Euler(0f, 15f, 0f), 10.0f, 16.0f, 0.20f, whiteMat);
        CreateTarmacText(markingsGo.transform, "GSE_Label_01",
            gseBayPos + new Vector3(0f, 0.005f, -7.0f), Quaternion.Euler(90f, 15f, 0f),
            "GSE STAGING / PASSENGER SHUTTLE", 7.5f, whiteMat.color);

        Vector3 gpuBayPos = new Vector3(-18.5f, 0.026f, 51.5f);
        CreateBoxOutline(markingsGo.transform, "GSE_StagingBay_GPU",
            gpuBayPos, Quaternion.Euler(0f, 15f, 0f), 5.5f, 5.5f, 0.18f, whiteMat);
        CreateTarmacText(markingsGo.transform, "GPU_Label",
            gpuBayPos + new Vector3(0f, 0.005f, -2.0f), Quaternion.Euler(90f, 15f, 0f),
            "GPU ONLY", 8.0f, yellowMat.color);

        // ── 7. Jetway Movement Hazard Exclusion Zone ──
        Vector3 jwHazardPos = new Vector3(-14.8f, 0.026f, 62.5f);
        CreateBoxOutline(markingsGo.transform, "Jetway_Hazard_Box",
            jwHazardPos, Quaternion.Euler(0f, 15f, 0f), 7.0f, 7.0f, 0.22f, redHazardMat);
        for (int d = -2; d <= 2; d++)
        {
            Vector3 dPos = jwHazardPos + Quaternion.Euler(0f, 15f, 0f) * new Vector3(d * 1.2f, 0f, 0f);
            CreateTarmacStripe(markingsGo.transform, "JW_HazardStripe_" + d,
                dPos, Quaternion.Euler(0f, 60f, 0f), new Vector3(0.20f, 0.01f, 5.5f), redHazardMat);
        }

        // ── 8. High-Visibility Rubber Nosewheel Chocks ──
        Vector3 noseWheelPos = new Vector3(-12.8f, 0f, 50.0f);
        CreateWheelChock(markingsGo.transform, noseWheelPos + new Vector3(-0.15f, 0f, -0.65f), Quaternion.Euler(0f, 105f, 0f));
        CreateWheelChock(markingsGo.transform, noseWheelPos + new Vector3(0.15f, 0f, 0.65f), Quaternion.Euler(0f, 105f, 0f));
    }

    // ═══════════════════════════════════════════════════
    //  RICH BACKGROUND AIRPORT ENVIRONMENT & SKYLINE
    // ═══════════════════════════════════════════════════
    private static void CreateBackgroundAirportEnvironment(Transform root, TimeOfDayController tod)
    {
        var bgRoot = new GameObject("Background_Airport_Environment");
        bgRoot.transform.SetParent(root, false);
        tod.backgroundEnvironment = bgRoot.transform;

        // ── 1. Distant Aircraft Maintenance Hangars ──
        var hangarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LowPolyBuildings/LP_AviationHangar_04.prefab");
        if (hangarPrefab != null)
        {
            var h1 = (GameObject)PrefabUtility.InstantiatePrefab(hangarPrefab, bgRoot.transform);
            h1.name = "Aviation_Hangar_BayA";
            h1.transform.position = new Vector3(-105f, 0f, 175f);
            h1.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
            h1.transform.localScale = Vector3.one * 1.35f;

            CreateHangarFloodlight(h1.transform, new Vector3(0f, 16f, 14f), tod);

            var h2 = (GameObject)PrefabUtility.InstantiatePrefab(hangarPrefab, bgRoot.transform);
            h2.name = "Aviation_Hangar_BayB";
            h2.transform.position = new Vector3(-162f, 0f, 160f);
            h2.transform.rotation = Quaternion.Euler(0f, 155f, 0f);
            h2.transform.localScale = Vector3.one * 1.45f;

            CreateHangarFloodlight(h2.transform, new Vector3(0f, 16f, 14f), tod);
        }

        // ── 2. Remote Stand 4 with Parked Airliner in the Distance ──
        var a350Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/A350NeoConcept/A350-900_NEO_Concept.prefab");
        if (a350Prefab != null)
        {
            var remotePlane = (GameObject)PrefabUtility.InstantiatePrefab(a350Prefab, bgRoot.transform);
            remotePlane.name = "RemoteStand4_A350";
            remotePlane.transform.position = new Vector3(-68.0f, -0.23f, 96.0f);
            remotePlane.transform.rotation = Quaternion.Euler(0f, 185f, 0f);
            remotePlane.transform.localScale = Vector3.one * 0.95f;

            var rStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rStripe.name = "RemoteStand_LeadIn";
            rStripe.transform.SetParent(bgRoot.transform, false);
            rStripe.transform.position = new Vector3(-68.0f, 0.025f, 90.0f);
            rStripe.transform.rotation = Quaternion.Euler(0f, 5f, 0f);
            rStripe.transform.localScale = new Vector3(0.35f, 0.015f, 35f);
            var yMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            yMat.color = new Color(0.98f, 0.78f, 0.08f, 1f);
            rStripe.GetComponent<MeshRenderer>().sharedMaterial = yMat;
            UnityEngine.Object.DestroyImmediate(rStripe.GetComponent<Collider>());
        }

        // ── 3. Modern Airport Skyline & Terminal Concourse Towers ──
        var modernTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LowPolyBuildings/LP_ModernTower_01.prefab");
        if (modernTowerPrefab != null)
        {
            var tow1 = (GameObject)PrefabUtility.InstantiatePrefab(modernTowerPrefab, bgRoot.transform);
            tow1.name = "Skyline_OperationsTower";
            tow1.transform.position = new Vector3(-45f, 0f, 225f);
            tow1.transform.rotation = Quaternion.Euler(0f, 175f, 0f);
            tow1.transform.localScale = Vector3.one * 1.6f;
        }

        var hotelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LowPolyBuildings/LP_HotelPlaza_05.prefab");
        if (hotelPrefab != null)
        {
            var hotel = (GameObject)PrefabUtility.InstantiatePrefab(hotelPrefab, bgRoot.transform);
            hotel.name = "Skyline_AirportHotel";
            hotel.transform.position = new Vector3(45f, 0f, 235f);
            hotel.transform.rotation = Quaternion.Euler(0f, 190f, 0f);
            hotel.transform.localScale = Vector3.one * 1.8f;
        }

        var steppedTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LowPolyBuildings/LP_SteppedTower_02.prefab");
        if (steppedTowerPrefab != null)
        {
            var step1 = (GameObject)PrefabUtility.InstantiatePrefab(steppedTowerPrefab, bgRoot.transform);
            step1.name = "Skyline_ConcourseTower";
            step1.transform.position = new Vector3(-5f, 0f, 250f);
            step1.transform.rotation = Quaternion.Euler(0f, 170f, 0f);
            step1.transform.localScale = Vector3.one * 1.7f;
        }

        var cargoBlockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LowPolyBuildings/LP_SkylineBlock_03.prefab");
        if (cargoBlockPrefab != null)
        {
            var cargo = (GameObject)PrefabUtility.InstantiatePrefab(cargoBlockPrefab, bgRoot.transform);
            cargo.name = "Skyline_CargoLogisticsCenter";
            cargo.transform.position = new Vector3(-135f, 0f, 235f);
            cargo.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
            cargo.transform.localScale = Vector3.one * 1.65f;
        }

        // ── 4. Airport Perimeter Security Fences ──
        var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/uploads_files_6360127_cgtrader_optimized_Fence_Pack_001.fbx");
        if (fencePrefab != null)
        {
            var fenceRoot = new GameObject("Perimeter_Security_Fences");
            fenceRoot.transform.SetParent(bgRoot.transform, false);

            for (int i = 0; i < 7; i++)
            {
                var fenceSeg = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, fenceRoot.transform);
                fenceSeg.name = "PerimeterFence_" + i;
                fenceSeg.transform.position = new Vector3(-150f + i * 22f, 0f, 140f + (i % 2) * 3f);
                fenceSeg.transform.rotation = Quaternion.Euler(0f, 85f, 0f);
                fenceSeg.transform.localScale = Vector3.one * 1.2f;
            }
        }

        // ── 5. High-Mast Apron Floodlight Towers (Ramp Lighting Masts) ──
        CreateHighMastFloodlight(bgRoot.transform, new Vector3(18.0f, 0f, 44.0f), 26f, 320f, tod, "HighMast_Gate2");
        CreateHighMastFloodlight(bgRoot.transform, new Vector3(-52.0f, 0f, 78.0f), 26f, 120f, tod, "HighMast_RemoteStand4");
        CreateHighMastFloodlight(bgRoot.transform, new Vector3(-88.0f, 0f, 145.0f), 28f, 150f, tod, "HighMast_Hangars");
    }

    // ── Ground Marking Mesh Helpers ──
    private static void CreateTarmacStripe(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
    {
        var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = name;
        stripe.transform.SetParent(parent, false);
        stripe.transform.position = pos;
        stripe.transform.rotation = rot;
        stripe.transform.localScale = scale;
        stripe.GetComponent<MeshRenderer>().sharedMaterial = mat;
        stripe.GetComponent<MeshRenderer>().receiveShadows = true;
        UnityEngine.Object.DestroyImmediate(stripe.GetComponent<Collider>());
    }

    private static void CreateBoxOutline(Transform parent, string name, Vector3 center, Quaternion rot, float width, float length, float thickness, Material mat)
    {
        var boxGo = new GameObject(name);
        boxGo.transform.SetParent(parent, false);

        // Top line
        CreateTarmacStripe(boxGo.transform, "Outline_Top",
            center + rot * new Vector3(0f, 0f, length * 0.5f), rot,
            new Vector3(width, 0.015f, thickness), mat);

        // Bottom line
        CreateTarmacStripe(boxGo.transform, "Outline_Bottom",
            center + rot * new Vector3(0f, 0f, -length * 0.5f), rot,
            new Vector3(width, 0.015f, thickness), mat);

        // Left line
        CreateTarmacStripe(boxGo.transform, "Outline_Left",
            center + rot * new Vector3(-width * 0.5f, 0f, 0f), rot,
            new Vector3(thickness, 0.015f, length), mat);

        // Right line
        CreateTarmacStripe(boxGo.transform, "Outline_Right",
            center + rot * new Vector3(width * 0.5f, 0f, 0f), rot,
            new Vector3(thickness, 0.015f, length), mat);
    }

    private static void CreateTarmacText(Transform parent, string name, Vector3 pos, Quaternion rot, string text, float fontSize, Color color)
    {
        var textGo = new GameObject(name);
        textGo.transform.SetParent(parent, false);
        textGo.transform.position = pos;
        textGo.transform.rotation = rot;
        var tmp = textGo.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        var rt = textGo.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(30f, 6f);
        }
    }

    private static void CreateWheelChock(Transform parent, Vector3 pos, Quaternion rot)
    {
        var chockGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chockGo.name = "NoseWheel_Chock";
        chockGo.transform.SetParent(parent, false);
        chockGo.transform.position = pos + Vector3.up * 0.12f;
        chockGo.transform.rotation = rot;
        chockGo.transform.localScale = new Vector3(0.35f, 0.22f, 1.4f);
        var chockMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        chockMat.color = new Color(0.95f, 0.75f, 0.05f, 1f); // Aviation Yellow rubber
        chockMat.SetFloat("_Smoothness", 0.15f);
        chockGo.GetComponent<MeshRenderer>().sharedMaterial = chockMat;
        UnityEngine.Object.DestroyImmediate(chockGo.GetComponent<Collider>());
    }

    private static void CreateHighMastFloodlight(Transform parent, Vector3 pos, float height, float rotY, TimeOfDayController tod, string name)
    {
        var mastGo = new GameObject(name);
        mastGo.transform.SetParent(parent, false);
        mastGo.transform.position = pos;
        mastGo.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        var steelMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        steelMat.color = new Color(0.72f, 0.76f, 0.80f, 1f);
        steelMat.SetFloat("_Metallic", 0.85f);
        steelMat.SetFloat("_Smoothness", 0.45f);

        // Mast pole
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "MastPole";
        pole.transform.SetParent(mastGo.transform, false);
        pole.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        pole.transform.localScale = new Vector3(0.75f, height * 0.5f, 0.75f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = steelMat;
        UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());

        // Crossbar
        var crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossbar.name = "Headframe";
        crossbar.transform.SetParent(mastGo.transform, false);
        crossbar.transform.localPosition = new Vector3(0f, height, 0f);
        crossbar.transform.localScale = new Vector3(5.5f, 0.45f, 0.85f);
        crossbar.GetComponent<MeshRenderer>().sharedMaterial = steelMat;
        UnityEngine.Object.DestroyImmediate(crossbar.GetComponent<Collider>());

        // Spotlights
        for (int i = -1; i <= 1; i++)
        {
            var lightGo = new GameObject("FloodLED_" + (i + 1));
            lightGo.transform.SetParent(mastGo.transform, false);
            lightGo.transform.localPosition = new Vector3(i * 1.8f, height - 0.2f, 0.4f);
            lightGo.transform.localRotation = Quaternion.Euler(48f, 0f, 0f);

            var sp = lightGo.AddComponent<Light>();
            sp.type = LightType.Spot;
            sp.color = new Color(0.96f, 0.98f, 1.0f);
            sp.range = 85f;
            sp.spotAngle = 75f;
            sp.intensity = 22f;
            sp.shadows = LightShadows.Soft;
        }

        // Obstruction beacon
        var beaconGo = new GameObject("ObstructionBeacon");
        beaconGo.transform.SetParent(mastGo.transform, false);
        beaconGo.transform.localPosition = new Vector3(0f, height + 0.8f, 0f);
        var bLight = beaconGo.AddComponent<Light>();
        bLight.type = LightType.Point;
        bLight.color = Color.red;
        bLight.range = 35f;
        bLight.intensity = 4.5f;

        var rotBeacon = beaconGo.AddComponent<SimpleRotator>();
        rotBeacon.rotationAxis = Vector3.up;
        rotBeacon.rotationSpeed = 150f;
    }

    private static void CreateHangarFloodlight(Transform hangarT, Vector3 localPos, TimeOfDayController tod)
    {
        var fGo = new GameObject("Hangar_ExteriorFlood");
        fGo.transform.SetParent(hangarT, false);
        fGo.transform.localPosition = localPos;
        fGo.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

        var sp = fGo.AddComponent<Light>();
        sp.type = LightType.Spot;
        sp.color = new Color(1.0f, 0.94f, 0.85f);
        sp.range = 45f;
        sp.spotAngle = 80f;
        sp.intensity = 16f;
        sp.shadows = LightShadows.Soft;
    }
}
