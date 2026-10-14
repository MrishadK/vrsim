using System.Collections;
using UnityEngine;

/// <summary>
/// AeroSim VR — Dynamic Airport Weather & Atmospheric System.
/// Controls weather states (Clear Sky, Golden Hour, Night Apron, Rainy Storm),
/// procedural falling rain particles, wet asphalt specular sheen reflections,
/// atmospheric fog densities, and audio soundscapes.
/// </summary>
public class AirportWeatherController : MonoBehaviour
{
    public enum WeatherMode
    {
        ClearSky,
        GoldenHour,
        NightApron,
        RainyStorm
    }

    public const string PREF_WEATHER = "GameWeatherMode";

    private static AirportWeatherController instance;
    public static AirportWeatherController Instance => instance;

    [Header("Current State")]
    public WeatherMode currentWeather = WeatherMode.ClearSky;

    [Header("Rain FX Targets")]
    public ParticleSystem rainParticleSystem;
    public Material tarmacMaterial;

    [Header("References")]
    public TimeOfDayController timeController;

    private Color dryTarmacColor = new Color(0.18f, 0.18f, 0.20f, 1f);
    private Color wetTarmacColor = new Color(0.10f, 0.11f, 0.13f, 1f);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        int saved = PlayerPrefs.GetInt(PREF_WEATHER, (int)WeatherMode.ClearSky);
        currentWeather = (WeatherMode)saved;
    }

    private void Start()
    {
        if (timeController == null)
        {
            timeController = FindObjectOfType<TimeOfDayController>();
        }

        ApplyWeather(currentWeather);
    }

    public void SetClearSky()
    {
        ApplyWeather(WeatherMode.ClearSky);
    }

    public void SetGoldenHour()
    {
        ApplyWeather(WeatherMode.GoldenHour);
    }

    public void SetNightApron()
    {
        ApplyWeather(WeatherMode.NightApron);
    }

    public void SetRainyStorm()
    {
        ApplyWeather(WeatherMode.RainyStorm);
    }

    public void ApplyWeather(WeatherMode mode)
    {
        currentWeather = mode;
        PlayerPrefs.SetInt(PREF_WEATHER, (int)mode);

        if (timeController == null)
        {
            timeController = FindObjectOfType<TimeOfDayController>();
        }

        bool isRain = (mode == WeatherMode.RainyStorm);

        // 1. Rain Particle System
        if (rainParticleSystem != null)
        {
            if (isRain)
            {
                if (!rainParticleSystem.isPlaying) rainParticleSystem.Play();
            }
            else
            {
                if (rainParticleSystem.isPlaying) rainParticleSystem.Stop();
            }
        }

        // 2. Tarmac Wet Surface Reflection Sheen
        if (tarmacMaterial != null)
        {
            if (isRain)
            {
                tarmacMaterial.SetFloat("_Smoothness", 0.92f); // High specular wet reflections
                tarmacMaterial.SetFloat("_Metallic", 0.35f);
                if (tarmacMaterial.HasProperty("_BaseColor")) tarmacMaterial.SetColor("_BaseColor", wetTarmacColor);
            }
            else
            {
                tarmacMaterial.SetFloat("_Smoothness", 0.40f); // Normal dry asphalt
                tarmacMaterial.SetFloat("_Metallic", 0.05f);
                if (tarmacMaterial.HasProperty("_BaseColor")) tarmacMaterial.SetColor("_BaseColor", dryTarmacColor);
            }
        }

        // 3. Audio SFX Transition
        if (AirportSFXManager.Instance != null)
        {
            AirportSFXManager.Instance.SetRainActive(isRain);
        }

        // 4. Synchronize Time & Atmospheric Lighting
        if (timeController != null)
        {
            switch (mode)
            {
                case WeatherMode.ClearSky:
                    timeController.SetTime(8.5f); // 08:30 AM
                    break;
                case WeatherMode.GoldenHour:
                    timeController.SetTime(18.25f); // 06:15 PM Sunset
                    break;
                case WeatherMode.NightApron:
                    timeController.SetTime(20.0f); // 08:00 PM Night
                    break;
                case WeatherMode.RainyStorm:
                    timeController.SetTime(15.5f); // Overcast afternoon
                    // Apply dense overcast rain fog
                    RenderSettings.fog = true;
                    RenderSettings.fogMode = FogMode.Linear;
                    RenderSettings.fogStartDistance = 40f;
                    RenderSettings.fogEndDistance = 320f;
                    RenderSettings.fogColor = new Color(0.35f, 0.40f, 0.48f, 1f);
                    break;
            }
        }
    }
}
