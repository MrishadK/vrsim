using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AeroSim VR — Aircraft Showcase FX.
/// Provides authentic aviation lights (flashing red beacons, wingtip strobes, navigation lights)
/// and gentle idle turbine rotation for the showcase aircraft in the Main Menu.
/// </summary>
public class AircraftShowcaseFX : MonoBehaviour
{
    [Header("Engine Turbines")]
    public Transform leftTurbine;
    public Transform rightTurbine;
    public float turbineSpinSpeed = 120f; // Idle spin in deg/sec

    [Header("Beacon Lights (Red)")]
    public List<Light> beaconLights = new List<Light>();
    public float beaconInterval = 1.15f;

    [Header("Strobe Lights (White)")]
    public List<Light> strobeLights = new List<Light>();
    public float strobeInterval = 1.35f;

    [Header("Navigation Lights (Constant)")]
    public List<Light> navLights = new List<Light>();

    private void Start()
    {
        StartCoroutine(BeaconFlashRoutine());
        StartCoroutine(StrobeFlashRoutine());
    }

    private void Update()
    {
        // Gentle turbine idle spin
        float rotDelta = turbineSpinSpeed * Time.deltaTime;
        if (leftTurbine != null) leftTurbine.Rotate(0f, 0f, rotDelta, Space.Self);
        if (rightTurbine != null) rightTurbine.Rotate(0f, 0f, rotDelta, Space.Self);
    }

    private IEnumerator BeaconFlashRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(beaconInterval);

            // Pulse ON
            SetLightsIntensity(beaconLights, 3.5f, true);
            yield return new WaitForSeconds(0.12f);

            // Fade OFF
            float elapsed = 0f;
            float fadeDur = 0.25f;
            while (elapsed < fadeDur)
            {
                elapsed += Time.deltaTime;
                float intensity = Mathf.Lerp(3.5f, 0f, elapsed / fadeDur);
                SetLightsIntensity(beaconLights, intensity, true);
                yield return null;
            }
            SetLightsIntensity(beaconLights, 0f, false);
        }
    }

    private IEnumerator StrobeFlashRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(strobeInterval);

            // Flash 1
            SetLightsIntensity(strobeLights, 5.0f, true);
            yield return new WaitForSeconds(0.05f);
            SetLightsIntensity(strobeLights, 0f, false);
            yield return new WaitForSeconds(0.10f);

            // Flash 2 (FAA double flash)
            SetLightsIntensity(strobeLights, 5.0f, true);
            yield return new WaitForSeconds(0.05f);
            SetLightsIntensity(strobeLights, 0f, false);
        }
    }

    private void SetLightsIntensity(List<Light> lights, float intensity, bool enabled)
    {
        if (lights == null) return;
        foreach (var l in lights)
        {
            if (l != null)
            {
                l.intensity = intensity;
                l.enabled = enabled;
            }
        }
    }
}
