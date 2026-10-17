using System.Collections;
using UnityEngine;

/// <summary>
/// Creates a realistic, alive airport atmosphere with procedural audio and ambient visual effects.
/// All audio is generated at runtime via AudioClip.Create() — no external files needed.
/// Visual effects include dust particles, light flicker, and an information display board.
/// </summary>
public class AirportAmbianceController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private float masterVolume = 0.5f;
    [SerializeField] private bool enablePAAnnouncements = true;
    [SerializeField] private float paAnnouncementInterval = 45f;
    [SerializeField] private bool enableCrowdMurmur = true;
    [SerializeField] private bool enableEngineRumble = true;
    [SerializeField] private bool enableAirConHum = true;

    [Header("Visual Effects")]
    [SerializeField] private bool enableDustParticles = true;
    [SerializeField] private bool enableLightFlicker = true;
    [SerializeField] private int dustParticleCount = 200;

    [Header("Positioning")]
    [SerializeField] private float ambientRadius = 50f;

    // Audio sources
    private AudioSource crowdSource;
    private AudioSource engineSource;
    private AudioSource airConSource;
    private AudioSource paSource;

    // Visual
    private ParticleSystem dustSystem;
    private Light[] flickerLights;
    private float flickerTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
            return;

        if (FindObjectOfType<AirportAmbianceController>() == null)
        {
            var go = new GameObject("AirportAmbianceController");
            go.AddComponent<AirportAmbianceController>();
        }
    }

    private void Start()
    {
        if (enableCrowdMurmur) CreateCrowdMurmur();
        if (enableEngineRumble) CreateEngineRumble();
        if (enableAirConHum) CreateAirConHum();
        if (enablePAAnnouncements) StartCoroutine(PAAnnoucementLoop());
        if (enableDustParticles) CreateDustParticles();
        if (enableLightFlicker) FindAndSetupFlickerLights();

        CreateAmbientSoundscape();
    }

    private void Update()
    {
        if (enableLightFlicker) UpdateLightFlicker();
    }

    // ─────────────────────────────────────────────
    //  CROWD MURMUR (filtered noise)
    // ─────────────────────────────────────────────

    private void CreateCrowdMurmur()
    {
        crowdSource = CreateAudioSource("CrowdMurmur", 0.08f * masterVolume, true, 0.5f);

        int sampleRate = 22050; // Lower rate for noise is fine
        int length = sampleRate * 4; // 4 second loop
        float[] samples = new float[length];

        // Simple low-pass filtered white noise to simulate crowd
        float prevSample = 0f;
        float filterCoeff = 0.92f; // Heavy low-pass

        for (int i = 0; i < length; i++)
        {
            float noise = Random.Range(-1f, 1f);
            // One-pole low-pass filter
            float filtered = prevSample * filterCoeff + noise * (1f - filterCoeff);
            prevSample = filtered;

            // Add very subtle periodic modulation (people talking in waves)
            float t = (float)i / sampleRate;
            float modulation = 1f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);

            samples[i] = filtered * modulation * 0.4f;
        }

        AudioClip clip = AudioClip.Create("CrowdMurmur", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        crowdSource.clip = clip;
        crowdSource.Play();
    }

    // ─────────────────────────────────────────────
    //  ENGINE RUMBLE (distant aircraft)
    // ─────────────────────────────────────────────

    private void CreateEngineRumble()
    {
        engineSource = CreateAudioSource("EngineRumble", 0.06f * masterVolume, true, 0.3f);

        int sampleRate = 22050;
        int length = sampleRate * 3;
        float[] samples = new float[length];

        float prevSample = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;

            // Low-frequency rumble: 30-80 Hz
            float rumble = Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.3f;
            rumble += Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.2f;
            rumble += Mathf.Sin(2f * Mathf.PI * 78f * t) * 0.1f;

            // Add band-limited noise
            float noise = Random.Range(-1f, 1f);
            float filtered = prevSample * 0.95f + noise * 0.05f;
            prevSample = filtered;

            // Slow amplitude modulation (engines powering up/down)
            float modulation = 1f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.08f * t);

            samples[i] = (rumble + filtered * 0.15f) * modulation;
        }

        AudioClip clip = AudioClip.Create("EngineRumble", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        engineSource.clip = clip;
        engineSource.Play();
    }

    // ─────────────────────────────────────────────
    //  AIR CONDITIONING HUM
    // ─────────────────────────────────────────────

    private void CreateAirConHum()
    {
        airConSource = CreateAudioSource("AirConHum", 0.04f * masterVolume, true, 0.4f);

        int sampleRate = 22050;
        int length = sampleRate * 2;
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            // Constant low hum with slight wobble
            float hum = Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.3f;
            hum += Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.15f;

            // White noise component (air rushing)
            float noise = Random.Range(-1f, 1f) * 0.08f;

            // Slow wobble
            float wobble = 1f + 0.05f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);

            samples[i] = (hum + noise) * wobble;
        }

        AudioClip clip = AudioClip.Create("AirConHum", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        airConSource.clip = clip;
        airConSource.Play();
    }

    // ─────────────────────────────────────────────
    //  PA ANNOUNCEMENT CHIMES
    // ─────────────────────────────────────────────

    private IEnumerator PAAnnoucementLoop()
    {
        paSource = CreateAudioSource("PAAnnouncement", 0.2f * masterVolume, false, 0.2f);

        // Wait a bit before first announcement
        yield return new WaitForSeconds(Random.Range(10f, 20f));

        while (true)
        {
            PlayPAChime();
            yield return new WaitForSeconds(paAnnouncementInterval + Random.Range(-10f, 10f));
        }
    }

    private void PlayPAChime()
    {
        if (paSource == null) return;

        int sampleRate = 44100;
        int length = (int)(sampleRate * 3.5f); // 3.5 seconds
        float[] samples = new float[length];

        // Classic airport "ding-dong-ding" three-tone chime
        float[] frequencies = { 880f, 660f, 880f }; // A5, E5, A5
        float[] startTimes = { 0f, 0.6f, 1.2f };
        float toneDuration = 0.5f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;

            for (int tone = 0; tone < frequencies.Length; tone++)
            {
                float toneStart = startTimes[tone];
                float toneT = t - toneStart;

                if (toneT >= 0f && toneT < toneDuration)
                {
                    float envelope = Mathf.Exp(-toneT * 4f); // Gentle decay
                    float fadeIn = Mathf.Clamp01(toneT * 50f); // Quick fade-in
                    sample += Mathf.Sin(2f * Mathf.PI * frequencies[tone] * toneT) * envelope * fadeIn * 0.25f;

                    // Add a soft harmonic
                    sample += Mathf.Sin(2f * Mathf.PI * frequencies[tone] * 2f * toneT) * envelope * fadeIn * 0.06f;
                }
            }

            // After chimes, add a brief low murmur to simulate announcement voice
            if (t > 2.0f && t < 3.2f)
            {
                float voiceT = t - 2.0f;
                float voiceEnvelope = Mathf.Sin(voiceT / 1.2f * Mathf.PI);
                // Simulate muffled speech with modulated noise
                float voice = Mathf.Sin(2f * Mathf.PI * 180f * voiceT) * 0.08f;
                voice += Mathf.Sin(2f * Mathf.PI * 260f * voiceT) * 0.04f;
                voice *= voiceEnvelope;
                sample += voice;
            }

            samples[i] = sample;
        }

        AudioClip clip = AudioClip.Create("PAChime", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        paSource.PlayOneShot(clip);
    }

    // ─────────────────────────────────────────────
    //  AMBIENT SOUNDSCAPE (footsteps, luggage wheels)
    // ─────────────────────────────────────────────

    private void CreateAmbientSoundscape()
    {
        StartCoroutine(RandomFootstepsLoop());
        StartCoroutine(RandomLuggageWheelLoop());
    }

    private IEnumerator RandomFootstepsLoop()
    {
        var footstepSource = CreateAudioSource("Footsteps", 0.03f * masterVolume, false, 0.8f);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.3f, 1.2f));

            int sampleRate = 22050;
            int length = sampleRate / 10; // Very short
            float[] samples = new float[length];

            float freq = Random.Range(300f, 600f);
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 60f);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f +
                              (Random.value - 0.5f) * 0.5f) * envelope;
            }

            AudioClip clip = AudioClip.Create("Footstep", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            footstepSource.pitch = Random.Range(0.8f, 1.3f);
            footstepSource.PlayOneShot(clip, Random.Range(0.1f, 0.25f));
        }
    }

    private IEnumerator RandomLuggageWheelLoop()
    {
        var wheelSource = CreateAudioSource("LuggageWheels", 0.02f * masterVolume, false, 0.7f);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 8f));

            int sampleRate = 22050;
            float duration = Random.Range(1f, 3f);
            int length = (int)(sampleRate * duration);
            float[] samples = new float[length];

            float prevSample = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                // Luggage wheel = high-frequency filtered noise with periodic bumps
                float noise = Random.Range(-1f, 1f);
                float filtered = prevSample * 0.7f + noise * 0.3f;
                prevSample = filtered;

                // Periodic bump pattern (wheel imperfections)
                float bump = Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 4f * t)) * 0.3f;

                // Fade in/out
                float envelope = Mathf.Sin(t / duration * Mathf.PI);

                samples[i] = (filtered * 0.3f + bump * 0.1f) * envelope;
            }

            AudioClip clip = AudioClip.Create("LuggageWheel", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            wheelSource.PlayOneShot(clip, Random.Range(0.05f, 0.15f));
        }
    }

    // ─────────────────────────────────────────────
    //  DUST PARTICLES
    // ─────────────────────────────────────────────

    private void CreateDustParticles()
    {
        var dustObj = new GameObject("DustParticles");
        dustObj.transform.SetParent(transform, false);
        dustObj.transform.localPosition = Vector3.up * 4f;

        dustSystem = dustObj.AddComponent<ParticleSystem>();

        var main = dustSystem.main;
        main.maxParticles = dustParticleCount;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.025f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.15f),
            new Color(0.9f, 0.85f, 0.7f, 0.08f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.002f; // Slight upward drift

        var emission = dustSystem.emission;
        emission.rateOverTime = dustParticleCount / 10f;

        var shape = dustSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(ambientRadius * 0.5f, 3f, ambientRadius * 0.5f);

        var noise = dustSystem.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.02f;

        // Use the default particle material
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit") ??
                                         Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                         Shader.Find("Standard"));
        renderer.material.color = new Color(1f, 1f, 1f, 0.1f);
    }

    // ─────────────────────────────────────────────
    //  LIGHT FLICKER (fluorescent)
    // ─────────────────────────────────────────────

    private void FindAndSetupFlickerLights()
    {
        // Find some existing lights in the scene
        flickerLights = FindObjectsOfType<Light>();
        flickerTimer = 0f;
    }

    private void UpdateLightFlicker()
    {
        if (flickerLights == null || flickerLights.Length == 0) return;

        flickerTimer += Time.deltaTime;

        // Only flicker occasionally — subtle realism
        if (flickerTimer > Random.Range(8f, 25f))
        {
            flickerTimer = 0f;

            // Pick a random light to flicker
            int idx = Random.Range(0, flickerLights.Length);
            if (flickerLights[idx] != null && flickerLights[idx].type == LightType.Point)
            {
                StartCoroutine(FlickerLight(flickerLights[idx]));
            }
        }
    }

    private IEnumerator FlickerLight(Light light)
    {
        if (light == null) yield break;

        float originalIntensity = light.intensity;
        int flickers = Random.Range(2, 5);

        for (int i = 0; i < flickers; i++)
        {
            light.intensity = originalIntensity * Random.Range(0.3f, 0.7f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
            light.intensity = originalIntensity;
            yield return new WaitForSeconds(Random.Range(0.02f, 0.06f));
        }

        light.intensity = originalIntensity;
    }

    // ─────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────

    private AudioSource CreateAudioSource(string sourceName, float volume, bool loop, float spatialBlend)
    {
        var sourceObj = new GameObject($"Ambient_{sourceName}");
        sourceObj.transform.SetParent(transform, false);

        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.volume = volume;
        source.loop = loop;
        source.spatialBlend = spatialBlend;
        source.minDistance = 2f;
        source.maxDistance = ambientRadius;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.playOnAwake = false;

        return source;
    }
}
