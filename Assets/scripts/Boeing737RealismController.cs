using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls authentic FAA/ICAO aviation lighting, 3D engine turbines, and thermal exhaust effects
/// for the Boeing 737-800.
/// </summary>
public class Boeing737RealismController : MonoBehaviour
{
    [Header("Aviation Lighting Controls")]
    public bool navLightsOn = true;
    public bool beaconLightsOn = true;
    public bool strobeLightsOn = true;
    public bool taxiLightsOn = true;
    public bool landingLightsOn = true;
    public bool logoLightsOn = true;
    public bool cockpitLightsOn = true;

    [Header("Lighting Fixtures")]
    public Light portNavLight;         // Red (Left Wingtip)
    public Light starboardNavLight;    // Green (Right Wingtip)
    public Light tailNavLight;         // White (Aft/Tail)
    public List<Light> strobeLights = new List<Light>();   // Wingtips & Tail (White Xenon)
    public List<Light> beaconLights = new List<Light>();   // Top & Belly (Red)
    public List<Light> taxiLights = new List<Light>();     // Nose Gear
    public List<Light> landingLights = new List<Light>();  // Wing Roots
    public List<Light> logoLights = new List<Light>();     // Vertical Stabilizer Floodlights
    public Light cockpitAmbientLight;

    [Header("Glow Meshes / Halos")]
    public List<Renderer> strobeRenderers = new List<Renderer>();
    public List<Renderer> beaconRenderers = new List<Renderer>();
    public List<Renderer> navRenderers = new List<Renderer>();

    [Header("Engine & Turbines")]
    public Transform leftTurbine;
    public Transform rightTurbine;
    public float idleRPM = 1200f;
    public float maxRPM = 4800f;
    public float currentRPM = 1400f;
    public ParticleSystem leftEngineHeat;
    public ParticleSystem rightEngineHeat;

    private Material strobeEmissiveMat;
    private Material beaconEmissiveMat;

    private void Awake()
    {
        InitializeMaterials();
    }

    private void Start()
    {
        StartCoroutine(StrobeRoutine());
        StartCoroutine(BeaconRoutine());
        ApplyStaticLighting();
    }

    private void InitializeMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        strobeEmissiveMat = new Material(litShader);
        strobeEmissiveMat.name = "Strobe_Emissive_Runtime";
        strobeEmissiveMat.EnableKeyword("_EMISSION");
        strobeEmissiveMat.SetColor("_BaseColor", Color.white);
        strobeEmissiveMat.SetColor("_EmissionColor", Color.black);

        beaconEmissiveMat = new Material(litShader);
        beaconEmissiveMat.name = "Beacon_Emissive_Runtime";
        beaconEmissiveMat.EnableKeyword("_EMISSION");
        beaconEmissiveMat.SetColor("_BaseColor", new Color(1f, 0.1f, 0.1f));
        beaconEmissiveMat.SetColor("_EmissionColor", Color.black);

        foreach (var r in strobeRenderers)
        {
            if (r != null) r.sharedMaterial = strobeEmissiveMat;
        }

        foreach (var r in beaconRenderers)
        {
            if (r != null) r.sharedMaterial = beaconEmissiveMat;
        }
    }

    private void Update()
    {
        // Spin Engine Turbines around local Z / forward axis
        float rotationDelta = currentRPM * 6f * Time.deltaTime; // 360 deg / 60 sec = 6
        if (leftTurbine != null)
        {
            leftTurbine.Rotate(Vector3.forward, rotationDelta, Space.Self);
        }
        if (rightTurbine != null)
        {
            rightTurbine.Rotate(Vector3.forward, rotationDelta, Space.Self);
        }
    }

    public void ApplyStaticLighting()
    {
        // Steady Navigation Lights (Port Red, Starboard Green, Tail White)
        if (portNavLight != null) portNavLight.enabled = navLightsOn;
        if (starboardNavLight != null) starboardNavLight.enabled = navLightsOn;
        if (tailNavLight != null) tailNavLight.enabled = navLightsOn;

        // Taxi & Landing Lights
        foreach (var l in taxiLights) if (l != null) l.enabled = taxiLightsOn;
        foreach (var l in landingLights) if (l != null) l.enabled = landingLightsOn;
        foreach (var l in logoLights) if (l != null) l.enabled = logoLightsOn;
        if (cockpitAmbientLight != null) cockpitAmbientLight.enabled = cockpitLightsOn;
    }

    /// <summary>
    /// FAA/ICAO Authentic Wingtip Xenon Double-Flash Strobe Sequence (approx 1.2s total period)
    /// </summary>
    private IEnumerator StrobeRoutine()
    {
        WaitForSeconds flashDuration = new WaitForSeconds(0.045f);
        WaitForSeconds doubleFlashInterval = new WaitForSeconds(0.085f);
        WaitForSeconds cycleInterval = new WaitForSeconds(1.05f);

        while (true)
        {
            if (strobeLightsOn)
            {
                // First Flash
                SetStrobes(true, 8f);
                yield return flashDuration;
                SetStrobes(false, 0f);
                yield return doubleFlashInterval;

                // Second Flash
                SetStrobes(true, 10f);
                yield return flashDuration;
                SetStrobes(false, 0f);
            }
            else
            {
                SetStrobes(false, 0f);
            }

            yield return cycleInterval;
        }
    }

    private void SetStrobes(bool enabled, float intensity)
    {
        foreach (var l in strobeLights)
        {
            if (l != null)
            {
                l.enabled = enabled;
                l.intensity = intensity;
            }
        }

        if (strobeEmissiveMat != null)
        {
            Color emission = enabled ? Color.white * (intensity * 1.5f) : Color.black;
            strobeEmissiveMat.SetColor("_EmissionColor", emission);
        }
    }

    /// <summary>
    /// FAA/ICAO Authentic Pulsing Red Anti-Collision Beacon (approx 1.0s sinusoidal pulse)
    /// </summary>
    private IEnumerator BeaconRoutine()
    {
        float timer = 0f;
        while (true)
        {
            if (beaconLightsOn)
            {
                timer += Time.deltaTime * 3.14159f * 1.05f; // ~1 pulse per second
                float pulse = Mathf.Sin(timer);
                if (pulse < 0f) pulse = 0f;
                pulse = Mathf.Pow(pulse, 4f); // Sharp incandescent flare

                float intensity = Mathf.Lerp(0f, 6.5f, pulse);
                bool active = intensity > 0.05f;

                foreach (var b in beaconLights)
                {
                    if (b != null)
                    {
                        b.enabled = active;
                        b.intensity = intensity;
                    }
                }

                if (beaconEmissiveMat != null)
                {
                    Color emission = new Color(1f, 0.05f, 0.05f) * (intensity * 2f);
                    beaconEmissiveMat.SetColor("_EmissionColor", emission);
                }
            }
            else
            {
                foreach (var b in beaconLights) if (b != null) b.enabled = false;
                if (beaconEmissiveMat != null) beaconEmissiveMat.SetColor("_EmissionColor", Color.black);
            }

            yield return null;
        }
    }
}
