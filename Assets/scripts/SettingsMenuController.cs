using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AeroSim VR — Settings Menu Controller.
/// Manages Main Menu preferences including:
/// - Time of Day (Morning to Evening slider time adjuster & presets)
/// - Master Audio Volume
/// - VR Comfort Mode (Turning Profile)
/// - Graphics Quality
/// </summary>
public class SettingsMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Time of Day Controls")]
    public Slider timeOfDaySlider;
    public TextMeshProUGUI timeOfDayText;
    [Tooltip("Optional reference to synchronize with the departures board clock.")]
    public TextMeshProUGUI departuresTimeText;

    [Header("UI Controls")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;
    public Dropdown graphicsDropdown;
    public Toggle comfortToggle;
    public TextMeshProUGUI comfortStatusText;

    private const string PREF_VOLUME = "MasterVolume";
    private const string PREF_GRAPHICS = "GraphicsQuality";
    private const string PREF_COMFORT = "VR_Comfort";

    private void Start()
    {
        InitializeTimeOfDay();
        InitializeAudio();
        InitializeGraphics();
        InitializeComfort();
    }

    // ─────────────────────────────────────────────
    //  TIME OF DAY SETUP & HANDLERS
    // ─────────────────────────────────────────────

    private void InitializeTimeOfDay()
    {
        float savedHour = TimeOfDayController.GetSavedHour();

        if (timeOfDaySlider != null)
        {
            timeOfDaySlider.minValue = 6.0f;  // 06:00 AM (Early morning)
            timeOfDaySlider.maxValue = 20.5f; // 08:30 PM (Evening/Night)
            timeOfDaySlider.wholeNumbers = false;
            timeOfDaySlider.value = savedHour;
            timeOfDaySlider.onValueChanged.RemoveAllListeners();
            timeOfDaySlider.onValueChanged.AddListener(OnTimeOfDayChanged);
        }

        UpdateTimeDisplays(savedHour);
    }

    public void OnTimeOfDayChanged(float hour)
    {
        TimeOfDayController.SaveHour(hour);
        UpdateTimeDisplays(hour);

        // If in a scene with active TimeOfDayController, apply immediately
        if (TimeOfDayController.Instance != null)
        {
            TimeOfDayController.Instance.ApplyTimeOfDay(hour);
        }
        else
        {
            var tod = FindObjectOfType<TimeOfDayController>();
            if (tod != null)
            {
                tod.ApplyTimeOfDay(hour);
            }
        }
    }

    private void UpdateTimeDisplays(float hour)
    {
        if (timeOfDayText != null)
        {
            timeOfDayText.text = TimeOfDayController.FormatTimeString(hour);
        }

        if (departuresTimeText != null)
        {
            departuresTimeText.text = TimeOfDayController.Format24HourClock(hour);
        }
    }

    // Preset button shortcuts
    public void SetMorningPreset()
    {
        AirportSFXManager.Instance?.PlayClick();
        SetTimePreset(8.5f); // 08:30 AM
    }

    public void SetNoonPreset()
    {
        AirportSFXManager.Instance?.PlayClick();
        SetTimePreset(12.0f); // 12:00 PM
    }

    public void SetSunsetPreset()
    {
        AirportSFXManager.Instance?.PlayClick();
        SetTimePreset(18.0f); // 06:00 PM
    }

    public void SetEveningPreset()
    {
        AirportSFXManager.Instance?.PlayClick();
        SetTimePreset(20.0f); // 08:00 PM
    }

    // Weather Presets
    public void SetWeatherClear()
    {
        AirportSFXManager.Instance?.PlayClick();
        if (AirportWeatherController.Instance != null) AirportWeatherController.Instance.SetClearSky();
        else SetMorningPreset();
    }

    public void SetWeatherSunset()
    {
        AirportSFXManager.Instance?.PlayClick();
        if (AirportWeatherController.Instance != null) AirportWeatherController.Instance.SetGoldenHour();
        else SetSunsetPreset();
    }

    public void SetWeatherNight()
    {
        AirportSFXManager.Instance?.PlayClick();
        if (AirportWeatherController.Instance != null) AirportWeatherController.Instance.SetNightApron();
        else SetEveningPreset();
    }

    public void SetWeatherRain()
    {
        AirportSFXManager.Instance?.PlayClick();
        if (AirportWeatherController.Instance != null) AirportWeatherController.Instance.SetRainyStorm();
    }

    public void SetTimePreset(float hour)
    {
        if (timeOfDaySlider != null)
        {
            timeOfDaySlider.value = hour;
        }
        OnTimeOfDayChanged(hour);
    }

    // ─────────────────────────────────────────────
    //  AUDIO SETUP & HANDLERS
    // ─────────────────────────────────────────────

    private void InitializeAudio()
    {
        float savedVolume = PlayerPrefs.GetFloat(PREF_VOLUME, 1.0f);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        SetVolume(savedVolume);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PREF_VOLUME, value);

        if (AirportSFXManager.Instance != null)
        {
            AirportSFXManager.Instance.SetMasterVolume(value);
        }

        if (volumeText != null)
        {
            volumeText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    // ─────────────────────────────────────────────
    //  GRAPHICS SETUP & HANDLERS
    // ─────────────────────────────────────────────

    private void InitializeGraphics()
    {
        int savedGraphics = PlayerPrefs.GetInt(PREF_GRAPHICS, QualitySettings.GetQualityLevel());

        if (graphicsDropdown != null)
        {
            if (graphicsDropdown.options.Count == 0)
            {
                graphicsDropdown.ClearOptions();
                graphicsDropdown.AddOptions(new List<string>(QualitySettings.names));
            }

            graphicsDropdown.value = Mathf.Clamp(savedGraphics, 0, QualitySettings.names.Length - 1);
            graphicsDropdown.onValueChanged.RemoveAllListeners();
            graphicsDropdown.onValueChanged.AddListener(SetGraphicsQuality);
        }

        SetGraphicsQuality(savedGraphics);
    }

    public void SetGraphicsQuality(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
        PlayerPrefs.SetInt(PREF_GRAPHICS, index);
    }

    // ─────────────────────────────────────────────
    //  COMFORT SETUP & HANDLERS
    // ─────────────────────────────────────────────

    private void InitializeComfort()
    {
        bool savedComfort = PlayerPrefs.GetInt(PREF_COMFORT, 1) == 1;

        if (comfortToggle != null)
        {
            comfortToggle.isOn = savedComfort;
            comfortToggle.onValueChanged.RemoveAllListeners();
            comfortToggle.onValueChanged.AddListener(SetComfortMode);
        }

        UpdateComfortLabel(savedComfort);
    }

    public void SetComfortMode(bool isOn)
    {
        PlayerPrefs.SetInt(PREF_COMFORT, isOn ? 1 : 0);
        UpdateComfortLabel(isOn);
    }

    private void UpdateComfortLabel(bool isOn)
    {
        if (comfortStatusText != null)
        {
            comfortStatusText.text = isOn ? "ENABLED (Snap Turning)" : "DISABLED (Smooth Turning)";
        }
    }

    // ─────────────────────────────────────────────
    //  PANEL NAVIGATION
    // ─────────────────────────────────────────────

    public void OpenSettings()
    {
        AirportSFXManager.Instance?.PlayClick();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        // Refresh time display on open
        float currentHour = TimeOfDayController.GetSavedHour();
        if (timeOfDaySlider != null) timeOfDaySlider.value = currentHour;
        UpdateTimeDisplays(currentHour);
    }

    public void CloseSettings()
    {
        AirportSFXManager.Instance?.PlayClick();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        PlayerPrefs.Save();
    }
}
