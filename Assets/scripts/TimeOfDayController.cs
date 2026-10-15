using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AeroSim VR — Dynamic Time of Day & Lighting Controller.
/// Adjusts the sun (Directional Light), ambient lighting, skybox exposure,
/// atmospheric fog, and airport architectural lights based on simulation time (Morning to Evening).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class TimeOfDayController : MonoBehaviour
{
    public const string PREF_TIMEOFDAY = "GameTimeOfDay";
    public const float DEFAULT_HOUR = 8.5f; // 08:30 AM Morning default

    [Header("Current Time")]
    [Tooltip("Time in decimal hours (6.0 = 06:00 AM, 12.0 = 12:00 PM, 18.0 = 06:00 PM, 20.0 = 08:00 PM)")]
    [Range(6f, 20.5f)]
    public float currentHour = DEFAULT_HOUR;

    [Header("Lighting Targets")]
    public Light sunLight;
    public Material skyboxMaterial;

    [Header("Airport Light Groups")]
    [Tooltip("Optional parent containing apron floodlight masts and taxiway edge lights.")]
    public Transform airportLightingRig;
    [Tooltip("Optional parent containing terminal building wash lights, window glows, and canopy downlights.")]
    public Transform buildingLightingRig;
    [Tooltip("Optional parent containing the Boeing 737 showcase aircraft.")]
    public Transform aircraftShowcase;
    [Tooltip("Optional secondary building lighting system (e.g. Modern Airport Terminal interior ceiling downlights).")]
    public Transform modernTerminalLighting;
    [Tooltip("Optional background airport environment lighting (e.g. hangars, background towers).")]
    public Transform backgroundEnvironment;
    [Tooltip("Material used for terminal curtain glass to control nocturnal window emission.")]
    public Material curtainGlassMaterial;

    private static TimeOfDayController instance;
    public static TimeOfDayController Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<TimeOfDayController>() == null)
        {
            var go = new GameObject("TimeOfDayController");
            go.AddComponent<TimeOfDayController>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Load saved time preference from PlayerPrefs
        currentHour = PlayerPrefs.GetFloat(PREF_TIMEOFDAY, DEFAULT_HOUR);
    }

    private void Start()
    {
        ResolveReferences();
        ApplyTimeOfDay(currentHour);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.leftBracketKey.wasPressedThisFrame)
            {
                SetTime(currentHour - 0.5f);
                Debug.Log($"[AeroSim VR] Time adjusted: {FormatTimeString(currentHour)}");
            }
            else if (kb.rightBracketKey.wasPressedThisFrame)
            {
                SetTime(currentHour + 0.5f);
                Debug.Log($"[AeroSim VR] Time adjusted: {FormatTimeString(currentHour)}");
            }
        }
#endif
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
            ApplyTimeOfDay(currentHour);
        }
    }

    public void ResolveReferences()
    {
        if (sunLight == null)
        {
            if (RenderSettings.sun != null)
            {
                sunLight = RenderSettings.sun;
            }
            else
            {
                var dirLights = FindObjectsOfType<Light>();
                foreach (var l in dirLights)
                {
                    if (l.type == LightType.Directional)
                    {
                        sunLight = l;
                        break;
                    }
                }
            }
        }

        // Configure Sun to cast 100% solid, opaque shadows so sunlight CANNOT penetrate or pass through buildings
        if (sunLight != null)
        {
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 1.0f; // 100% opaque solid shadows - prevents sunlight going through buildings
            sunLight.shadowBias = 0.015f;
            sunLight.shadowNormalBias = 0.15f;
        }

        var scenery = GameObject.Find("LiveAirportScenery");

        // 1. Apron Stadium High-Masts & Taxiway Lights
        if (airportLightingRig == null)
        {
            if (scenery != null)
            {
                var t = scenery.transform.Find("ApronLightingRig");
                if (t != null) airportLightingRig = t;
            }
            if (airportLightingRig == null)
            {
                var rigObj = GameObject.Find("ApronLightingRig");
                if (rigObj != null) airportLightingRig = rigObj.transform;
            }
        }

        // 2. Terminal Building Architectural & Interior Lighting Rig
        if (buildingLightingRig == null)
        {
            if (scenery != null)
            {
                var t = scenery.transform.Find("TerminalBuildingLightingRig");
                if (t != null) buildingLightingRig = t;
            }
            if (buildingLightingRig == null)
            {
                var bRigObj = GameObject.Find("TerminalBuildingLightingRig") ?? GameObject.Find("Architectural_Lighting_Rig");
                if (bRigObj != null) buildingLightingRig = bRigObj.transform;
            }
        }

        // 3. Modern Terminal 05_Lighting_System
        if (modernTerminalLighting == null)
        {
            var termObj = GameObject.Find("Modern_Airport_Terminal") ?? GameObject.Find("Modern_International_Airport_Terminal");
            if (termObj != null)
            {
                var lSys = termObj.transform.Find("05_Lighting_System");
                if (lSys != null) modernTerminalLighting = lSys;
            }
            if (modernTerminalLighting == null)
            {
                var lSys = GameObject.Find("05_Lighting_System");
                if (lSys != null) modernTerminalLighting = lSys.transform;
            }
        }

        // 4. Background Airport Environment
        if (backgroundEnvironment == null)
        {
            if (scenery != null)
            {
                var bg = scenery.transform.Find("Background_Airport_Environment");
                if (bg != null) backgroundEnvironment = bg;
            }
            if (backgroundEnvironment == null)
            {
                var bgObj = GameObject.Find("Background_Airport_Environment");
                if (bgObj != null) backgroundEnvironment = bgObj.transform;
            }
        }

        // 5. Aircraft Showcase
        if (aircraftShowcase == null)
        {
            if (scenery != null)
            {
                var t = scenery.transform.Find("A350_Showcase") ?? scenery.transform.Find("Boeing 737-800 Showcase");
                if (t != null) aircraftShowcase = t;
            }
            if (aircraftShowcase == null)
            {
                var pObj = GameObject.Find("A350_Showcase") ?? GameObject.Find("Boeing 737-800 Showcase");
                if (pObj != null) aircraftShowcase = pObj.transform;
            }
        }

        // 6. Skybox Material
        if (skyboxMaterial == null && RenderSettings.skybox != null)
        {
            if (Application.isPlaying)
            {
                skyboxMaterial = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyboxMaterial;
            }
            else
            {
                skyboxMaterial = RenderSettings.skybox;
            }
        }

        // 7. Curtain Glass Material for Nocturnal Window Glow
        if (curtainGlassMaterial == null)
        {
            var allMats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var m in allMats)
            {
                if (m != null && m.name.Contains("CurtainGlass"))
                {
                    curtainGlassMaterial = m;
                    break;
                }
            }
        }

        // 8. Enforce Two-Sided Shadow Casting on all Terminal / Building renderers
        EnforceBuildingTwoSidedShadows();
    }

    /// <summary>
    /// Enforces two-sided shadow casting on all airport buildings so that sunlight cannot pass through single-sided polygons or backfaces.
    /// </summary>
    private void EnforceBuildingTwoSidedShadows()
    {
        var allRenderers = FindObjectsOfType<MeshRenderer>();
        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            string objName = r.gameObject.name;
            string parentName = (r.transform.parent != null) ? r.transform.parent.name : "";

            if (objName.Contains("Terminal") || objName.Contains("Roof") || objName.Contains("Wall") ||
                objName.Contains("Facade") || objName.Contains("Hangar") || objName.Contains("Tower") ||
                objName.Contains("Building") || objName.Contains("Airport") ||
                parentName.Contains("Terminal") || parentName.Contains("Architecture") || parentName.Contains("Building"))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                r.receiveShadows = true;
            }
        }
    }

    /// <summary>
    /// Sets the time of day and immediately applies lighting calculations.
    /// </summary>
    public void SetTime(float hour)
    {
        currentHour = Mathf.Clamp(hour, 6f, 20.5f);
        PlayerPrefs.SetFloat(PREF_TIMEOFDAY, currentHour);
        ApplyTimeOfDay(currentHour);
    }

    /// <summary>
    /// Applies lighting parameters for the specified hour.
    /// </summary>
    public void ApplyTimeOfDay(float hour)
    {
        if (sunLight == null || (airportLightingRig == null && buildingLightingRig == null))
            ResolveReferences();

        // ── 1. Sun & Moon Key Light Rotation ──
        float pitch;
        float yaw;

        if (hour <= 12.0f)
        {
            // Morning: 06:00 (pitch 16°, yaw 28°) -> 12:00 (pitch 46°, yaw 58°)
            float m = Mathf.InverseLerp(6.0f, 12.0f, hour);
            pitch = Mathf.Lerp(16f, 46f, Mathf.SmoothStep(0f, 1f, m));
            yaw = Mathf.LerpAngle(28f, 58f, m);
        }
        else if (hour <= 16.5f)
        {
            // Afternoon: 12:00 (pitch 46°, yaw 58°) -> 16:30 (pitch 30°, yaw 95°)
            float a = Mathf.InverseLerp(12.0f, 16.5f, hour);
            pitch = Mathf.Lerp(46f, 30f, Mathf.SmoothStep(0f, 1f, a));
            yaw = Mathf.LerpAngle(58f, 95f, a);
        }
        else if (hour <= 18.5f)
        {
            // Late Afternoon / Sunset Golden Hour: 16:30 (pitch 30°, yaw 95°) -> 18:30 (pitch 6°, yaw 245°)
            float s = Mathf.InverseLerp(16.5f, 18.5f, hour);
            pitch = Mathf.Lerp(30f, 6f, Mathf.SmoothStep(0f, 1f, s));
            yaw = Mathf.LerpAngle(95f, 245f, Mathf.SmoothStep(0f, 1f, s));
        }
        else if (hour <= 19.3f)
        {
            // Twilight Dusk / Sun dips below horizon: 18:30 (pitch 6°, yaw 245°) -> 19:18 (pitch -4°, yaw 265°)
            float d = Mathf.InverseLerp(18.5f, 19.3f, hour);
            pitch = Mathf.Lerp(6f, -4f, Mathf.SmoothStep(0f, 1f, d));
            yaw = Mathf.LerpAngle(245f, 265f, Mathf.SmoothStep(0f, 1f, d));
        }
        else
        {
            // Evening & Night (19:18 -> 20:30): Gentle Nocturnal Moonlight Key Light shining from steep overhead angle!
            // Pitch 48°, Yaw 48° - steep downward angle eliminates horizontal rays cutting through building openings!
            float n = Mathf.InverseLerp(19.3f, 20.5f, hour);
            pitch = Mathf.Lerp(45f, 50f, Mathf.SmoothStep(0f, 1f, n));
            yaw = 48f;
        }

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            sunLight.color = EvaluateSunColor(hour);
            sunLight.intensity = EvaluateSunIntensity(hour);
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 1.0f; // 100% opaque solid shadows - completely blocks sunlight through buildings!
            sunLight.shadowBias = 0.015f;
            sunLight.shadowNormalBias = 0.15f;
        }

        // ── 2. Ambient Lighting & Fog ──
        ApplyAmbientAndFog(hour);

        // ── 3. Skybox Exposure & Tint ──
        ApplySkybox(hour);

        // ── 4. Terminal & Tarmac Artificial Lights ──
        ApplyAirportArtificialLights(hour);
    }

    private Color EvaluateSunColor(float hour)
    {
        if (hour < 7.5f)
        {
            // Early Dawn (warm rose-gold)
            float t = Mathf.InverseLerp(6f, 7.5f, hour);
            return Color.Lerp(new Color(1f, 0.70f, 0.42f), new Color(1f, 0.88f, 0.72f), t);
        }
        else if (hour < 11.5f)
        {
            // Morning (crisp radiant golden sunlight)
            float t = Mathf.InverseLerp(7.5f, 11.5f, hour);
            return Color.Lerp(new Color(1f, 0.88f, 0.72f), new Color(1f, 0.96f, 0.90f), t);
        }
        else if (hour < 15.5f)
        {
            // Midday (brilliant aeronautical daylight)
            return new Color(0.99f, 0.98f, 1.0f);
        }
        else if (hour < 17.0f)
        {
            // Late Afternoon (soft warm amber)
            float t = Mathf.InverseLerp(15.5f, 17.0f, hour);
            return Color.Lerp(new Color(0.99f, 0.98f, 1.0f), new Color(1f, 0.85f, 0.62f), t);
        }
        else if (hour < 18.5f)
        {
            // Sunset / Golden Hour (fiery rich amber-gold)
            float t = Mathf.InverseLerp(17.0f, 18.5f, hour);
            return Color.Lerp(new Color(1f, 0.85f, 0.62f), new Color(1f, 0.48f, 0.20f), t);
        }
        else if (hour < 19.3f)
        {
            // Twilight Dusk (deep violet-crimson twilight)
            float t = Mathf.InverseLerp(18.5f, 19.3f, hour);
            return Color.Lerp(new Color(1f, 0.48f, 0.20f), new Color(0.45f, 0.42f, 0.75f), t);
        }
        else
        {
            // Evening / Night (Celestial Moonlight cool steel-blue)
            float t = Mathf.InverseLerp(19.3f, 20.5f, hour);
            return Color.Lerp(new Color(0.45f, 0.42f, 0.75f), new Color(0.55f, 0.72f, 0.98f), t);
        }
    }

    private float EvaluateSunIntensity(float hour)
    {
        if (hour < 8f)
            return Mathf.Lerp(1.0f, 1.75f, Mathf.InverseLerp(6f, 8f, hour));
        else if (hour < 15.5f)
            return 1.85f;
        else if (hour < 17.0f)
            return Mathf.Lerp(1.85f, 1.45f, Mathf.InverseLerp(15.5f, 17.0f, hour));
        else if (hour < 18.5f)
            // Sunset golden hour: fades smoothly from 1.45f down to 0.45f
            return Mathf.Lerp(1.45f, 0.45f, Mathf.InverseLerp(17.0f, 18.5f, hour));
        else if (hour < 19.2f)
            // Dusk / Twilight: sun fades out as it dips below horizon
            return Mathf.Lerp(0.45f, 0.08f, Mathf.InverseLerp(18.5f, 19.2f, hour));
        else
            // Night Moonlight Key: Holds at a subtle, soft 0.08f - 0.12f
            // Soft celestial moonlight that does NOT overpower artificial airport lighting or shine through buildings!
            return Mathf.Lerp(0.08f, 0.12f, Mathf.InverseLerp(19.2f, 20.5f, hour));
    }

    private void ApplyAmbientAndFog(float hour)
    {
        Color skyColor;
        Color equatorColor;
        Color groundColor;
        Color fogColor;

        if (hour < 9f)
        {
            // Morning: crisp blue sky, warm horizon, rich dark asphalt bounce
            float t = Mathf.InverseLerp(6f, 9f, hour);
            skyColor = Color.Lerp(new Color(0.48f, 0.60f, 0.85f), new Color(0.55f, 0.72f, 0.95f), t);
            equatorColor = Color.Lerp(new Color(0.85f, 0.70f, 0.52f), new Color(0.68f, 0.74f, 0.82f), t);
            groundColor = new Color(0.18f, 0.20f, 0.24f);
            fogColor = Color.Lerp(new Color(0.85f, 0.78f, 0.72f), new Color(0.74f, 0.84f, 0.94f), t);
        }
        else if (hour < 16.5f)
        {
            // Midday: radiant azure sky, clean horizon, dark slate ground
            skyColor = new Color(0.58f, 0.76f, 0.98f);
            equatorColor = new Color(0.65f, 0.74f, 0.84f);
            groundColor = new Color(0.20f, 0.22f, 0.26f);
            fogColor = new Color(0.75f, 0.86f, 0.96f);
        }
        else if (hour < 19.0f)
        {
            // Sunset / Golden Hour: fiery dusk horizon
            float t = Mathf.InverseLerp(16.5f, 19.0f, hour);
            skyColor = Color.Lerp(new Color(0.58f, 0.76f, 0.98f), new Color(0.52f, 0.35f, 0.62f), t);
            equatorColor = Color.Lerp(new Color(0.65f, 0.74f, 0.84f), new Color(0.96f, 0.58f, 0.28f), t);
            groundColor = new Color(0.18f, 0.15f, 0.14f);
            fogColor = Color.Lerp(new Color(0.75f, 0.86f, 0.96f), new Color(0.92f, 0.55f, 0.32f), t);
        }
        else
        {
            // Evening / Night: rich nocturnal twilight with urban airport ambient glow
            float t = Mathf.InverseLerp(19.0f, 20.5f, hour);
            skyColor = Color.Lerp(new Color(0.52f, 0.35f, 0.62f), new Color(0.32f, 0.44f, 0.70f), t);
            equatorColor = Color.Lerp(new Color(0.96f, 0.58f, 0.28f), new Color(0.38f, 0.44f, 0.60f), t);
            groundColor = new Color(0.20f, 0.24f, 0.32f); // Luminous asphalt night bounce
            fogColor = Color.Lerp(new Color(0.92f, 0.55f, 0.32f), new Color(0.14f, 0.18f, 0.30f), t);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = skyColor;
        RenderSettings.ambientEquatorColor = equatorColor;
        RenderSettings.ambientGroundColor = groundColor;

        // Linear Fog: Keeps the foreground tarmac and Boeing 737 100% sharp and clear
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 140f;
        RenderSettings.fogEndDistance = 900f;
        RenderSettings.fogColor = fogColor;
    }

    private void ApplySkybox(float hour)
    {
        if (skyboxMaterial == null) return;

        if (skyboxMaterial.HasProperty("_Exposure"))
        {
            float exposure;
            if (hour < 8f)
                exposure = Mathf.Lerp(0.90f, 1.15f, Mathf.InverseLerp(6f, 8f, hour));
            else if (hour < 16f)
                exposure = 1.25f;
            else if (hour < 18.5f)
                exposure = Mathf.Lerp(1.25f, 0.98f, Mathf.InverseLerp(16f, 18.5f, hour));
            else
                exposure = Mathf.Lerp(0.98f, 0.85f, Mathf.InverseLerp(18.5f, 20.5f, hour));

            skyboxMaterial.SetFloat("_Exposure", exposure);
        }

        if (skyboxMaterial.HasProperty("_Tint"))
        {
            Color tint;
            if (hour < 8.5f)
                tint = new Color(0.58f, 0.54f, 0.50f, 0.5f);
            else if (hour < 16f)
                tint = new Color(0.52f, 0.52f, 0.52f, 0.5f);
            else if (hour < 18.5f)
                tint = new Color(0.64f, 0.50f, 0.42f, 0.5f);
            else
                tint = new Color(0.45f, 0.52f, 0.75f, 0.5f);

            skyboxMaterial.SetColor("_Tint", tint);
        }
    }

    private void ApplyAirportArtificialLights(float hour)
    {
        // Smooth transition factors
        // Evening transition begins smoothly at 16.8f, reaching full brilliance by 18.2f - 19.0f
        float eveningFactor = Mathf.Clamp01(Mathf.InverseLerp(16.8f, 18.5f, hour));
        float dawnFactor = (hour <= 8.0f) ? Mathf.Clamp01(Mathf.InverseLerp(8.0f, 6.5f, hour)) : 0f;
        float nightFactor = Mathf.Max(eveningFactor, dawnFactor);
        bool needsFloodlights = (nightFactor > 0.05f);

        // ── 1. Apron Stadium High-Mast Floodlights & Taxiway Guidance Lights ──
        if (airportLightingRig != null)
        {
            float mastIntensity = Mathf.Lerp(3.0f, 32.0f, nightFactor);
            var lights = airportLightingRig.GetComponentsInChildren<Light>(true);
            foreach (var l in lights)
            {
                if (l.type == LightType.Spot)
                {
                    l.enabled = needsFloodlights;
                    l.intensity = mastIntensity;
                    if (l.range < 90f) l.range = 120f;
                }
                else if (l.type == LightType.Point)
                {
                    // Taxiway edge lights (blue) and centerline markers (amber)
                    l.enabled = needsFloodlights;
                    l.intensity = Mathf.Lerp(1.2f, 4.0f, nightFactor);
                    if (l.range < 6f) l.range = 7.5f;
                }
            }
        }

        // ── 2. Terminal Building Architectural, Facade, Window Glow & Interior Lights ──
        ApplyBuildingLightsGroup(buildingLightingRig, nightFactor, needsFloodlights);
        ApplyBuildingLightsGroup(modernTerminalLighting, nightFactor, needsFloodlights);

        // ── 3. Background Airport Hangars & Remote Masts ──
        if (backgroundEnvironment != null)
        {
            var bgLights = backgroundEnvironment.GetComponentsInChildren<Light>(true);
            foreach (var l in bgLights)
            {
                if (l.type == LightType.Spot)
                {
                    l.enabled = needsFloodlights;
                    l.intensity = Mathf.Lerp(2.0f, 25.0f, nightFactor);
                }
                else if (l.type == LightType.Point)
                {
                    l.enabled = true;
                    if (l.name.Contains("Beacon"))
                    {
                        l.intensity = Mathf.Lerp(2.5f, 6.5f, nightFactor);
                    }
                    else
                    {
                        l.enabled = needsFloodlights;
                        l.intensity = Mathf.Lerp(2.0f, 15.0f, nightFactor);
                    }
                }
            }
        }

        // ── 4. Terminal Window Glass Material Glow (Emission) ──
        if (curtainGlassMaterial != null)
        {
            curtainGlassMaterial.EnableKeyword("_EMISSION");
            Color warmInteriorGlow = new Color(1.0f, 0.84f, 0.58f); // 3200K warm interior light
            curtainGlassMaterial.SetColor("_EmissionColor", warmInteriorGlow * (nightFactor * 1.8f));
        }

        // ── 5. Aircraft Showcase Night Servicing & Belly Lights ──
        if (aircraftShowcase != null)
        {
            var aLights = aircraftShowcase.GetComponentsInChildren<Light>(true);
            foreach (var l in aLights)
            {
                if (l.name.Contains("BellyFillLight"))
                {
                    l.intensity = Mathf.Lerp(0.85f, 2.8f, nightFactor);
                }
                else if (l.name.Contains("GateServicing"))
                {
                    l.enabled = needsFloodlights;
                    l.intensity = Mathf.Lerp(0f, 22.0f, nightFactor);
                }
            }
        }

        // ── 6. Fallback for standalone scene lights ──
        if (airportLightingRig == null && buildingLightingRig == null)
        {
            var sceneLights = FindObjectsOfType<Light>();
            foreach (var l in sceneLights)
            {
                if (l == sunLight || l.type == LightType.Directional) continue;

                if (l.name.Contains("StreetLight") || l.name.Contains("Spotlight") || l.name.Contains("Point Light") || l.name.Contains("VRLight") || l.name.Contains("LED"))
                {
                    l.enabled = true;
                    l.intensity = Mathf.Lerp(1.2f, 4.0f, nightFactor);
                }
            }
        }
    }

    private void ApplyBuildingLightsGroup(Transform rig, float nightFactor, bool needsFloodlights)
    {
        if (rig == null) return;

        var bLights = rig.GetComponentsInChildren<Light>(true);
        foreach (var l in bLights)
        {
            if (l.name.Contains("LED_Panel") || l.name.Contains("Ceiling_Light") || l.name.Contains("Interior") || (l.transform.parent != null && (l.transform.parent.name.Contains("Interior") || l.transform.parent.name.Contains("Lighting_System"))))
            {
                // Interior concourse and terminal ceiling panels: always on 24/7 like a real airport, boosted in evening!
                l.enabled = true;
                l.intensity = Mathf.Lerp(1.4f, 3.8f, nightFactor);
                if (l.range < 16f) l.range = 22f;
                l.shadows = LightShadows.None; // Optimal performance for interior downlights
            }
            else if (l.name.Contains("WindowGlow"))
            {
                // Terminal concourse interior warm light radiating through curtain glass
                l.enabled = true;
                l.intensity = Mathf.Lerp(5.0f, 35.0f, nightFactor);
                if (l.range < 60f) l.range = 80f;
            }
            else if (l.name.Contains("FacadeWash") || l.name.Contains("Front_Floodlight") || l.name.Contains("Rear_Floodlight") || l.name.Contains("Exterior_Flood") || l.name.Contains("Hangar_ExteriorFlood") || l.name.Contains("Apron_Floodlight"))
            {
                // Exterior architectural floodlights washing the facade and apron
                l.enabled = needsFloodlights;
                l.intensity = Mathf.Lerp(2.0f, 38.0f, nightFactor);
                if (l.range < 60f) l.range = 95f;
                l.shadows = LightShadows.Soft;
            }
            else if (l.name.Contains("CanopyDownlight") || l.name.Contains("Portal"))
            {
                // Canopy entrance and portal downlights
                l.enabled = true;
                l.intensity = Mathf.Lerp(3.0f, 20.0f, nightFactor);
                if (l.range < 25f) l.range = 35f;
            }
            else if (l.name.Contains("RoofContour") || l.name.Contains("Neon"))
            {
                // Rooftop architectural contour neon
                l.enabled = needsFloodlights;
                l.intensity = Mathf.Lerp(1.0f, 16.0f, nightFactor);
                if (l.range < 40f) l.range = 55f;
            }
            else
            {
                // Generic building fixtures
                l.enabled = true;
                l.intensity = Mathf.Lerp(1.2f, 4.0f, nightFactor);
            }
        }
    }

    /// <summary>
    /// Helper to format any hour value into a rich display string with icons.
    /// E.g. "08:30 AM  •  MORNING ☀️"
    /// </summary>
    public static string FormatTimeString(float hour)
    {
        int totalMinutes = Mathf.RoundToInt(hour * 60f);
        int h = (totalMinutes / 60) % 24;
        int m = totalMinutes % 60;

        string ampm = (h >= 12) ? "PM" : "AM";
        int displayH = h % 12;
        if (displayH == 0) displayH = 12;

        string phaseLabel;
        if (hour < 7.5f)
            phaseLabel = "DAWN 🌅";
        else if (hour < 11.5f)
            phaseLabel = "MORNING ☀️";
        else if (hour < 14.5f)
            phaseLabel = "MIDDAY / NOON ☀️";
        else if (hour < 17.0f)
            phaseLabel = "AFTERNOON 🌤️";
        else if (hour < 18.5f)
            phaseLabel = "GOLDEN HOUR / SUNSET 🌇";
        else if (hour < 19.3f)
            phaseLabel = "TWILIGHT DUSK 🌆";
        else
            phaseLabel = "EVENING / NIGHT 🌙";

        return $"{displayH:D2}:{m:D2} {ampm}   •   {phaseLabel}";
    }

    /// <summary>
    /// Formats a 24-hour clock string (e.g. "08:30" or "18:45") for boards.
    /// </summary>
    public static string Format24HourClock(float hour)
    {
        int totalMinutes = Mathf.RoundToInt(hour * 60f);
        int h = (totalMinutes / 60) % 24;
        int m = totalMinutes % 60;
        return $"{h:D2}:{m:D2}";
    }

    public static float GetSavedHour()
    {
        return PlayerPrefs.GetFloat(PREF_TIMEOFDAY, DEFAULT_HOUR);
    }

    public static void SaveHour(float hour)
    {
        PlayerPrefs.SetFloat(PREF_TIMEOFDAY, Mathf.Clamp(hour, 6f, 20.5f));
        PlayerPrefs.Save();
    }
}
