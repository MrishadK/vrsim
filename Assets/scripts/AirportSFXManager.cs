using System.Collections;
using UnityEngine;

/// <summary>
/// AeroSim VR — Airport Apron & UI Audio / SFX Manager.
/// Manages high-fidelity ambient APU hum, jet turbine whine, atmospheric tarmac wind,
/// periodic ATC radio chatter, rain effects, and high-tech UI feedback sounds.
/// </summary>
public class AirportSFXManager : MonoBehaviour
{
    private static AirportSFXManager instance;
    public static AirportSFXManager Instance => instance;

    [Header("Audio Clips")]
    public AudioClip apronAmbienceClip;
    public AudioClip atcChatterClip;
    public AudioClip rainLoopClip;
    public AudioClip uiHoverClip;
    public AudioClip uiClickClip;

    [Header("Volume Balances")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float apronVolume = 0.55f;
    [Range(0f, 1f)] public float atcVolume = 0.40f;
    [Range(0f, 1f)] public float rainVolume = 0.65f;
    [Range(0f, 1f)] public float uiVolume = 0.60f;

    // Internal Audio Sources
    private AudioSource apronSource;
    private AudioSource atcSource;
    private AudioSource rainSource;
    private AudioSource uiSource;

    private Coroutine atcRoutine;
    private bool isRaining = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
            return;

        if (FindObjectOfType<AirportSFXManager>() == null)
        {
            var go = new GameObject("AirportSFXManager");
            go.AddComponent<AirportSFXManager>();
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

        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        EnsureClips();
        SetupAudioSources();
    }

    private void EnsureClips()
    {
        if (apronAmbienceClip == null)
        {
            apronAmbienceClip = GenerateApronWindClip();
        }
        if (atcChatterClip == null)
        {
            atcChatterClip = GenerateRadioTransmissionClip();
        }
        if (rainLoopClip == null)
        {
            rainLoopClip = GenerateRainSoundClip();
        }
    }

    private AudioClip GenerateApronWindClip()
    {
        int sampleRate = 22050;
        int length = sampleRate * 5;
        float[] samples = new float[length];
        float lp = 0f;
        for (int i = 0; i < length; i++)
        {
            float white = Random.Range(-1f, 1f);
            lp += (white - lp) * 0.05f; // Deep low-frequency tarmac wind & APU hum
            float sub = Mathf.Sin(2f * Mathf.PI * 60f * i / sampleRate) * 0.15f; // 60Hz transformer/APU hum
            samples[i] = (lp * 0.85f + sub) * 0.35f;
        }
        var clip = AudioClip.Create("Procedural_ApronWind", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateRadioTransmissionClip()
    {
        int sampleRate = 22050;
        int length = (int)(sampleRate * 2.2f);
        float[] samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float burst = 0f;
            // Roger/Squelch beep at start and end
            if (t < 0.12f)
            {
                burst = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.35f;
            }
            else if (t > 2.05f)
            {
                burst = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.35f;
            }
            else
            {
                // Simulated band-limited speech formant chatter
                float formant = Mathf.Sin(2f * Mathf.PI * 400f * t) * 0.3f
                              + Mathf.Sin(2f * Mathf.PI * 850f * t) * 0.25f
                              + Mathf.Sin(2f * Mathf.PI * 1600f * t) * 0.2f;
                float noise = Random.Range(-0.15f, 0.15f);
                burst = (formant + noise) * 0.25f;
            }
            samples[i] = burst;
        }
        var clip = AudioClip.Create("Procedural_ATCRadio", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateRainSoundClip()
    {
        int sampleRate = 22050;
        int length = sampleRate * 3;
        float[] samples = new float[length];
        float lp = 0f;
        for (int i = 0; i < length; i++)
        {
            float white = Random.Range(-1f, 1f);
            lp += (white - lp) * 0.12f;
            samples[i] = lp * 0.45f;
        }
        var clip = AudioClip.Create("Procedural_RainLoop", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void Start()
    {
        StartAudio();
    }

    private void SetupAudioSources()
    {
        // 1. Apron Ambience Loop (APU drone, compressor whine, tarmac wind)
        apronSource = gameObject.AddComponent<AudioSource>();
        apronSource.clip = apronAmbienceClip;
        apronSource.loop = true;
        apronSource.playOnAwake = false;
        apronSource.volume = apronVolume * masterVolume;
        apronSource.spatialBlend = 0f; // 2D immersion

        // 2. ATC Radio Transmission Chatter
        atcSource = gameObject.AddComponent<AudioSource>();
        atcSource.clip = atcChatterClip;
        atcSource.loop = false;
        atcSource.playOnAwake = false;
        atcSource.volume = atcVolume * masterVolume;
        atcSource.spatialBlend = 0f;

        // 3. Rain Loop (Active during rainy weather)
        rainSource = gameObject.AddComponent<AudioSource>();
        rainSource.clip = rainLoopClip;
        rainSource.loop = true;
        rainSource.playOnAwake = false;
        rainSource.volume = 0f;
        rainSource.spatialBlend = 0f;

        // 4. UI Tactile Audio Source
        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.loop = false;
        uiSource.playOnAwake = false;
        uiSource.volume = uiVolume * masterVolume;
        uiSource.spatialBlend = 0f;
    }

    public void StartAudio()
    {
        if (apronSource != null && apronAmbienceClip != null)
        {
            apronSource.Play();
        }

        if (atcRoutine != null) StopCoroutine(atcRoutine);
        atcRoutine = StartCoroutine(ATCChatterLoop());
    }

    private IEnumerator ATCChatterLoop()
    {
        // Initial delay before first ATC communication
        yield return new WaitForSeconds(6.0f);

        while (true)
        {
            if (atcSource != null && atcChatterClip != null && masterVolume > 0.05f)
            {
                atcSource.volume = atcVolume * masterVolume * Random.Range(0.85f, 1.15f);
                atcSource.pitch = Random.Range(0.96f, 1.04f);
                atcSource.Play();
            }

            // Authentic random interval between air traffic control bursts (12s to 24s)
            float waitTime = Random.Range(14f, 26f);
            yield return new WaitForSeconds(waitTime);
        }
    }

    /// <summary>
    /// Smoothly transitions the rainy tarmac audio loop on or off.
    /// </summary>
    public void SetRainActive(bool active)
    {
        isRaining = active;
        StopCoroutine("FadeRainRoutine");
        StartCoroutine(FadeRainRoutine(active ? rainVolume * masterVolume : 0f, 1.8f));
    }

    private IEnumerator FadeRainRoutine(float targetVol, float duration)
    {
        if (rainSource == null) yield break;

        if (targetVol > 0.01f && !rainSource.isPlaying)
        {
            rainSource.Play();
        }

        float startVol = rainSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rainSource.volume = Mathf.Lerp(startVol, targetVol, elapsed / duration);
            yield return null;
        }

        rainSource.volume = targetVol;
        if (targetVol <= 0.001f && rainSource.isPlaying)
        {
            rainSource.Stop();
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);

        if (apronSource != null) apronSource.volume = apronVolume * masterVolume;
        if (uiSource != null) uiSource.volume = uiVolume * masterVolume;
        if (rainSource != null && isRaining) rainSource.volume = rainVolume * masterVolume;
    }

    public void PlayClick()
    {
        if (uiSource != null && uiClickClip != null && masterVolume > 0.01f)
        {
            uiSource.pitch = Random.Range(0.98f, 1.02f);
            uiSource.PlayOneShot(uiClickClip, uiVolume * masterVolume);
        }
    }

    public void PlayHover()
    {
        if (uiSource != null && uiHoverClip != null && masterVolume > 0.01f)
        {
            uiSource.pitch = Random.Range(0.97f, 1.03f);
            uiSource.PlayOneShot(uiHoverClip, (uiVolume * 0.7f) * masterVolume);
        }
    }
}
