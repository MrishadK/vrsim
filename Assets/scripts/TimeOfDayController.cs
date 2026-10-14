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

        var scenery = GameObject.Find("LiveAirportScenery");

        if (airportLightingRig == null)
        {
            if (scenery != null)
            {
                var t = scenery.transform.Find("ApronLightingRig");
                if (t != null) airportLightingRig = t;
            }
            if (airportLightingRig == null)
            {
                var rigObj = GameObject.Find("ApronLightingRig") ?? GameObject.Find("Architectural_Lighting_Rig");
                if (rigObj != null) airportLightingRig = rigObj.transform;
            }
        }

        if (buildingLightingRig == null)
        {
            if (scenery != null)
            {
                var t = scenery.transform.Find("TerminalBuildingLightingRig");
                if (t != null) buildingLightingRig = t;
            }
            if (buildingLightingRig == null)
            {
                var bRigObj = GameObject.Find("TerminalBuildingLightingRig");
                if (bRigObj != null) buildingLightingRig = bRigObj.transform;
            }
        }

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
        if (sunLight == null || airportLightingRig == null || buildingLightingRig == null)
            ResolveReferences();

        // ── 1. Sun & Moon Key Light Rotation ──
        // Morning & Midday: Sun shines from yaw 28°-58° as a glorious Key Light onto the aircraft & terminal facade.
        // Sunset: Moves to yaw 265° for golden-hour backlight rim lighting.
        // Evening & Night: Seamlessly transitions into a high-angle Nocturnal Moonlight Key Light (pitch 38°, yaw 48°),
        // preventing the scene from ever going dark and keeping all aircraft & terminal geometry clearly illuminated!
        float pitch;
        float yaw;

        if (hour <= 12.0f)
        {
            // Morning: 06:00 (pitch 16°, yaw 28°) -> 12:00 (pitch 46°, yaw 58°)
            float m = Mathf.InverseLerp(6.0f, 12.0f, hour);
            pitch = Mathf.Lerp(16f, 46f, Mathf.SmoothStep(0f, 1f, m));
            yaw = Mathf.LerpAngle(28f, 58f, m);
        }
        else if (hour <= 17.5f)
        {
            // Afternoon: 12:00 (pitch 46°, yaw 58°) -> 17:30 (pitch 24°, yaw 110°)
            float a = Mathf.InverseLerp(12.0f, 17.5f, hour);
            pitch = Mathf.Lerp(46f, 24f, Mathf.SmoothStep(0f, 1f, a));
            yaw = Mathf.LerpAngle(58f, 110f, a);
        }
        else if (hour <= 19.0f)
        {
            // Golden Hour / Sunset: 17:30 (pitch 24°, yaw 110°) -> 19:00 (pitch 6°, yaw 265°)
            float s = Mathf.InverseLerp(17.5f, 19.0f, hour);
            pitch = Mathf.Lerp(24f, 6f, Mathf.SmoothStep(0f, 1f, s));
            yaw = Mathf.LerpAngle(110f, 265f, Mathf.SmoothStep(0f, 1f, s));
        }
        else
        {
            // Evening & Night (19:00 -> 20:30): Transitions smoothly from sunset into
            // a powerful Nocturnal Moonlight Key at Pitch 38°, Yaw 48°!
            float e = Mathf.InverseLerp(19.0f, 20.5f, hour);
            pitch = Mathf.Lerp(6f, 38f, Mathf.SmoothStep(0f, 1f, e));
            yaw = Mathf.LerpAngle(265f, 48f, Mathf.SmoothStep(0f, 1f, e));
        }

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            sunLight.color = EvaluateSunColor(hour);
            sunLight.intensity = EvaluateSunIntensity(hour);
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = (hour >= 19.0f) ? 0.65f : 0.86f;
            sunLight.shadowBias = 0.02f;
            sunLight.shadowNormalBias = 0.12f;
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
        else if (hour < 17.5f)
        {
            // Late Afternoon (soft warm amber)
            float t = Mathf.InverseLerp(15.5f, 17.5f, hour);
            return Color.Lerp(new Color(0.99f, 0.98f, 1.0f), new Color(1f, 0.85f, 0.62f), t);
        }
        else if (hour < 19.0f)
        {
            // Sunset / Golden Hour (fiery rich amber-gold)
            float t = Mathf.InverseLerp(17.5f, 19.0f, hour);
            return Color.Lerp(new Color(1f, 0.85f, 0.62f), new Color(1f, 0.52f, 0.22f), t);
        }
        else
        {
            // Evening / Night (Luminous Celestial Moonlight steel-blue)
            float t = Mathf.InverseLerp(19.0f, 20.5f, hour);
            return Color.Lerp(new Color(1f, 0.52f, 0.22f), new Color(0.55f, 0.72f, 0.98f), t);
        }
    }

    private float EvaluateSunIntensity(float hour)
    {
        if (hour < 8f)
            return Mathf.Lerp(1.2f, 1.75f, Mathf.InverseLerp(6f, 8f, hour));
        else if (hour < 15.5f)
            return 1.85f;
        else if (hour < 17.5f)
            return Mathf.Lerp(1.85f, 1.45f, Mathf.InverseLerp(15.5f, 17.5f, hour));
        else if (hour < 19.0f)
            return Mathf.Lerp(1.45f, 0.85f, Mathf.InverseLerp(17.5f, 19.0f, hour));
        else
            // Night Moonlight Key: Holds steady at 0.95f - 1.10f so the scene is beautifully illuminated!
            return Mathf.Lerp(0.85f, 1.05f, Mathf.InverseLerp(19.0f, 20.5f, hour));
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
        bool isEvening = (hour >= 17.5f);
        bool isDawn = (hour <= 7.5f);
        bool needsFloodlights = (isEvening || isDawn);

        // 1. Apron High-Mast Stadium Floodlights (Towers Gate 12 & Gate 14)
        if (airportLightingRig != null)
        {
            float mastIntensity = isEvening ? Mathf.Lerp(4.0f, 32.0f, Mathf.InverseLerp(17.5f, 19.5f, hour)) :
                                  isDawn ? Mathf.Lerp(32.0f, 4.0f, Mathf.InverseLerp(6.0f, 7.5f, hour)) : 0.5f;

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
                    l.enabled = (hour >= 17.0f || hour <= 8.0f);
                    l.intensity = (hour >= 17.0f || hour <= 8.0f) ? 3.8f : 1.4f;
                    if (l.range < 6f) l.range = 7.0f;
                }
            }
        }

        // 2. Building Architectural Lights (Terminal Facade Wash, Window Glows, Canopy)
        if (buildingLightingRig != null)
        {
            var bLights = buildingLightingRig.GetComponentsInChildren<Light>(true);
            foreach (var l in bLights)
            {
                if (l.name.Contains("WindowGlow"))
                {
                    // Terminal concourse interior lights stay active 24/7 like a real airport!
                    l.enabled = true;
                    l.intensity = (hour >= 17.8f || hour <= 7.5f) ? 26.0f : 4.5f;
                    if (l.range < 50f) l.range = 75f;
                }
                else if (l.name.Contains("FacadeWash"))
                {
                    // Exterior architectural ground floodlights washing the terminal facade
                    l.enabled = needsFloodlights;
                    l.intensity = isEvening ? Mathf.Lerp(6.0f, 42.0f, Mathf.InverseLerp(17.5f, 19.5f, hour)) :
                                  isDawn ? Mathf.Lerp(42.0f, 6.0f, Mathf.InverseLerp(6.0f, 7.5f, hour)) : 1.5f;
                    if (l.range < 90f) l.range = 120f;
                }
                else if (l.name.Contains("CanopyDownlight"))
                {
                    l.enabled = (hour >= 17.0f || hour <= 8.0f);
                    l.intensity = (hour >= 17.0f || hour <= 8.0f) ? 18.0f : 3.0f;
                    if (l.range < 25f) l.range = 35f;
                }
                else if (l.name.Contains("RoofContour"))
                {
                    l.enabled = needsFloodlights;
                    l.intensity = isEvening ? Mathf.Lerp(4.0f, 18.0f, Mathf.InverseLerp(17.5f, 19.5f, hour)) :
                                  isDawn ? Mathf.Lerp(18.0f, 4.0f, Mathf.InverseLerp(6.0f, 7.5f, hour)) : 1.0f;
                    if (l.range < 40f) l.range = 55f;
                }
                else
                {
                    l.enabled = needsFloodlights;
                }
            }
        }

        // 3. Boeing 737 Night Servicing & Belly Lights
        if (aircraftShowcase != null)
        {
            var aLights = aircraftShowcase.GetComponentsInChildren<Light>(true);
            foreach (var l in aLights)
            {
                if (l.name.Contains("BellyFillLight"))
                {
                    l.intensity = (hour >= 17.5f) ? 2.5f : 0.85f;
                }
                else if (l.name.Contains("GateServicing"))
                {
                    l.enabled = needsFloodlights;
                    l.intensity = isEvening ? Mathf.Lerp(3.0f, 22.0f, Mathf.InverseLerp(17.5f, 19.5f, hour)) :
                                  isDawn ? Mathf.Lerp(22.0f, 3.0f, Mathf.InverseLerp(6.0f, 7.5f, hour)) : 0f;
                }
            }
        }

        // 4. Fallback for gameplay scenes (e.g. SampleScene) without explicit airport rigs
        if (airportLightingRig == null && buildingLightingRig == null)
        {
            var sceneLights = FindObjectsOfType<Light>();
            foreach (var l in sceneLights)
            {
                if (l == sunLight || l.type == LightType.Directional) continue;

                // Turn on scene lamps, streetlights, spotlights in evening / night
                if (l.name.Contains("StreetLight") || l.name.Contains("Spotlight") || l.name.Contains("Point Light") || l.name.Contains("VRLight"))
                {
                    l.enabled = needsFloodlights;
                }
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
        else if (hour < 17.5f)
            phaseLabel = "AFTERNOON 🌤️";
        else if (hour < 19.0f)
            phaseLabel = "GOLDEN HOUR 🌇";
        else if (hour < 20.0f)
            phaseLabel = "SUNSET 🌆";
        else
            phaseLabel = "EVENING / DUSK 🌙";

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
