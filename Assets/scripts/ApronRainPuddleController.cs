using System.Collections;
using UnityEngine;

/// <summary>
/// AeroSim VR — Apron Tarmac Rain Ripple, Puddle & Water Accumulation Controller.
/// Manages dynamic wet asphalt darkening, low-lying puddle accumulation,
/// animated concentric rain ripples, and ground-impact droplet splash particles.
/// </summary>
[ExecuteAlways]
[SelectionBase]
public class ApronRainPuddleController : MonoBehaviour
{
    private static ApronRainPuddleController instance;
    public static ApronRainPuddleController Instance => instance;

    [Header("Weather & Wetness State")]
    [Tooltip("Is it currently raining on the apron?")]
    public bool isRaining = true;

    [Range(0f, 1f)]
    [Tooltip("Current live rain wetness intensity (0 = dry tarmac, 1 = stormy downpour).")]
    public float rainIntensity = 1.0f;

    [Header("Puddle Configuration")]
    [Range(0f, 1f)]
    [Tooltip("Coverage of water puddles forming in surface depressions.")]
    public float puddleCoverage = 0.85f;

    [Range(0.80f, 1f)]
    [Tooltip("Mirror specular smoothness of standing puddle water.")]
    public float puddleSmoothness = 0.98f;

    [Range(0f, 1f)]
    [Tooltip("How much wet asphalt darkens compared to dry asphalt.")]
    public float wetDarkening = 0.65f;

    public Color puddleWaterTint = new Color(0.06f, 0.07f, 0.09f, 1.0f);

    [Header("Rain Ripple Waves")]
    [Tooltip("Grid density / tiling of falling raindrop ripple impacts.")]
    public float rippleTiling = 45.0f;

    [Tooltip("Speed of concentric ripple wave expansion.")]
    public float rippleSpeed = 2.4f;

    [Range(0f, 3f)]
    [Tooltip("Height / perturbation strength of the expanding water ripples.")]
    public float rippleIntensity = 1.6f;

    [Header("Physical Splash Particle FX")]
    [Tooltip("Enable visible expanding ripple rings and droplet splashes on the tarmac surface.")]
    public bool enableSplashParticles = true;
    public ParticleSystem rainSplashParticles;

    [Header("Transitions & Smoothing")]
    [Tooltip("How quickly the tarmac gets soaked when rain starts (seconds).")]
    public float soakDuration = 3.5f;

    [Tooltip("How quickly puddles evaporate and tarmac dries up when rain stops (seconds).")]
    public float dryDuration = 6.0f;

    [Header("References")]
    public Renderer tarmacRenderer;
    public Material tarmacMaterial;

    [Header("Interactive Testing")]
    public bool enableHotkeys = true; // Press 'R' in Play Mode to toggle rain

    private float currentWetLevel = 1.0f;
    private MaterialPropertyBlock propBlock;

    // Shader Property IDs
    private static readonly int PropRainIntensity = Shader.PropertyToID("_RainIntensity");
    private static readonly int PropPuddleAmount = Shader.PropertyToID("_PuddleAmount");
    private static readonly int PropPuddleSmoothness = Shader.PropertyToID("_PuddleSmoothness");
    private static readonly int PropPuddleTint = Shader.PropertyToID("_PuddleTint");
    private static readonly int PropRippleTiling = Shader.PropertyToID("_RippleTiling");
    private static readonly int PropRippleSpeed = Shader.PropertyToID("_RippleSpeed");
    private static readonly int PropRippleIntensity = Shader.PropertyToID("_RippleIntensity");
    private static readonly int PropWetDarkening = Shader.PropertyToID("_WetDarkening");

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        propBlock = new MaterialPropertyBlock();
        AutoFindReferences();
    }

    private void Start()
    {
        AutoFindReferences();
        EnsureMaterialConfigured();
        SetupSplashParticleSystem();

        // Check AirportWeatherController on startup
        if (AirportWeatherController.Instance != null)
        {
            isRaining = (AirportWeatherController.Instance.currentWeather == AirportWeatherController.WeatherMode.RainyStorm);
            currentWetLevel = isRaining ? 1.0f : 0.0f;
            rainIntensity = currentWetLevel;
        }

        ApplyMaterialProperties();
    }

    private void Update()
    {
        HandleKeyboardInput();
        UpdateWetnessTransition();
        ApplyMaterialProperties();
        UpdateSplashParticles();
    }

    private void AutoFindReferences()
    {
        if (tarmacRenderer == null)
        {
            tarmacRenderer = GetComponent<Renderer>();
            if (tarmacRenderer == null)
            {
                var tarmacGo = GameObject.Find("ApronTarmac");
                if (tarmacGo != null) tarmacRenderer = tarmacGo.GetComponent<Renderer>();
            }
        }

        if (tarmacRenderer != null && tarmacMaterial == null)
        {
            tarmacMaterial = tarmacRenderer.sharedMaterial;
        }

        if (rainSplashParticles == null)
        {
            Transform splashChild = transform.Find("ApronRainSplashSystem");
            if (splashChild != null)
            {
                rainSplashParticles = splashChild.GetComponent<ParticleSystem>();
            }
        }
    }

    public void EnsureMaterialConfigured()
    {
        Shader puddleShader = Shader.Find("AeroSim/ApronTarmac_RainPuddle");
        if (puddleShader == null) return;

        if (tarmacRenderer != null)
        {
            Material currentMat = tarmacRenderer.sharedMaterial;
            if (currentMat == null || currentMat.shader != puddleShader)
            {
                Material newMat = new Material(puddleShader);
                newMat.name = "ApronTarmac_RainPuddle_Instance";

                // Retain textures from existing material if available
                if (currentMat != null)
                {
                    if (currentMat.HasProperty("_BaseMap") && currentMat.GetTexture("_BaseMap") != null)
                    {
                        newMat.SetTexture("_BaseMap", currentMat.GetTexture("_BaseMap"));
                        newMat.SetTextureScale("_BaseMap", currentMat.GetTextureScale("_BaseMap"));
                    }
                    if (currentMat.HasProperty("_BumpMap") && currentMat.GetTexture("_BumpMap") != null)
                    {
                        newMat.SetTexture("_BumpMap", currentMat.GetTexture("_BumpMap"));
                    }
                    if (currentMat.HasProperty("_OcclusionMap") && currentMat.GetTexture("_OcclusionMap") != null)
                    {
                        newMat.SetTexture("_OcclusionMap", currentMat.GetTexture("_OcclusionMap"));
                    }
                }

#if UNITY_EDITOR
                // Load high-resolution rain ripple normal and puddle mask
                Texture2D puddleMask = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/planetextures/Apron_Puddle_Mask.png");
                Texture2D rippleNorm = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/planetextures/Rain_Ripple_Normal.png");

                if (puddleMask != null) newMat.SetTexture("_PuddleMask", puddleMask);
                if (rippleNorm != null) newMat.SetTexture("_RippleNormalMap", rippleNorm);

                if (newMat.GetTexture("_BaseMap") == null)
                {
                    Texture2D albedo = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/pebbled-asphalt1-unity/pebbled_asphalt_albedo.png");
                    if (albedo != null)
                    {
                        newMat.SetTexture("_BaseMap", albedo);
                        newMat.SetTextureScale("_BaseMap", new Vector2(40f, 40f));
                    }
                }
                if (newMat.GetTexture("_BumpMap") == null)
                {
                    Texture2D norm = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/pebbled-asphalt1-unity/pebbled_asphalt_Normal-ogl.png");
                    if (norm != null) newMat.SetTexture("_BumpMap", norm);
                }
#endif
                tarmacRenderer.sharedMaterial = newMat;
                tarmacMaterial = newMat;
                Debug.Log("<color=#00e676><b>[ApronRainPuddleController]</b></color> Configured ApronTarmac with high-fidelity Rain Puddle & Ripple Shader!");
            }
        }
    }

    public void SetupSplashParticleSystem()
    {
        if (rainSplashParticles != null) return;

        Transform parentTransform = tarmacRenderer != null ? tarmacRenderer.transform : transform;
        Transform existing = parentTransform.Find("ApronRainSplashSystem");
        if (existing != null)
        {
            rainSplashParticles = existing.GetComponent<ParticleSystem>();
            return;
        }

        GameObject splashGo = new GameObject("ApronRainSplashSystem");
        splashGo.transform.SetParent(parentTransform, false);
        // Position slightly above the tarmac surface to prevent z-fighting
        splashGo.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        splashGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face flat against ground

        ParticleSystem ps = splashGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = 1200;
        main.startLifetime = 0.55f;
        main.startSpeed = 0.05f;
        main.startSize = 0.12f;
        main.startColor = new Color(0.92f, 0.96f, 1.0f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 380f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // Cover viewing area around terminal apron
        shape.scale = new Vector3(80f, 80f, 0.1f);

        // Size over lifetime curve (expanding ripple wave)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.35f);
        sizeCurve.AddKey(1f, 2.5f); // Expands outward from drop impact
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // Color over lifetime (fades out smoothly)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.88f, 0.94f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.70f, 0f), new GradientAlphaKey(0.45f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var psr = splashGo.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        Material splashMat = new Material(particleShader);
        splashMat.name = "Mat_RainSplashRing_Instance";

#if UNITY_EDITOR
        Texture2D splashTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/planetextures/Rain_Splash_Ring.png");
        if (splashTex != null)
        {
            splashMat.mainTexture = splashTex;
            if (splashMat.HasProperty("_BaseMap")) splashMat.SetTexture("_BaseMap", splashTex);
        }
#endif
        splashMat.color = new Color(0.92f, 0.96f, 1f, 0.65f);
        psr.sharedMaterial = splashMat;

        rainSplashParticles = ps;
    }

    private void UpdateWetnessTransition()
    {
        float target = isRaining ? 1.0f : 0.0f;
        float duration = isRaining ? soakDuration : dryDuration;
        if (duration <= 0.05f) duration = 0.05f;

        float step = (Time.deltaTime > 0f ? Time.deltaTime : 0.016f) / duration;
        currentWetLevel = Mathf.MoveTowards(currentWetLevel, target, step);
        rainIntensity = currentWetLevel;
    }

    private void ApplyMaterialProperties()
    {
        if (tarmacMaterial == null) return;

        if (tarmacMaterial.HasProperty(PropRainIntensity))
        {
            tarmacMaterial.SetFloat(PropRainIntensity, rainIntensity);
            tarmacMaterial.SetFloat(PropPuddleAmount, puddleCoverage);
            tarmacMaterial.SetFloat(PropPuddleSmoothness, puddleSmoothness);
            tarmacMaterial.SetColor(PropPuddleTint, puddleWaterTint);
            tarmacMaterial.SetFloat(PropRippleTiling, rippleTiling);
            tarmacMaterial.SetFloat(PropRippleSpeed, rippleSpeed);
            tarmacMaterial.SetFloat(PropRippleIntensity, rippleIntensity);
            tarmacMaterial.SetFloat(PropWetDarkening, wetDarkening);
        }
        else
        {
            // Fallback for Standard URP/Lit if custom shader is not yet applied
            float smooth = Mathf.Lerp(0.38f, puddleSmoothness, rainIntensity);
            tarmacMaterial.SetFloat("_Smoothness", smooth);
            tarmacMaterial.SetFloat("_Metallic", Mathf.Lerp(0.05f, 0.25f, rainIntensity));
        }
    }

    private void UpdateSplashParticles()
    {
        if (rainSplashParticles == null) return;

        var emission = rainSplashParticles.emission;
        bool shouldEmit = enableSplashParticles && (rainIntensity > 0.05f);

        emission.enabled = shouldEmit;
        if (shouldEmit)
        {
            emission.rateOverTime = Mathf.Lerp(30f, 420f, rainIntensity);
            if (!rainSplashParticles.isPlaying && Application.isPlaying) rainSplashParticles.Play();
        }
        else
        {
            if (rainSplashParticles.isPlaying && Application.isPlaying) rainSplashParticles.Stop();
        }
    }

    public void SetRainActive(bool active)
    {
        isRaining = active;
    }

    public void SetInstantRain(bool active)
    {
        isRaining = active;
        currentWetLevel = active ? 1.0f : 0.0f;
        rainIntensity = currentWetLevel;
        ApplyMaterialProperties();
        UpdateSplashParticles();
    }

    private void HandleKeyboardInput()
    {
        if (!enableHotkeys || !Application.isPlaying) return;

        // Press 'R' to toggle rain wetness, puddles & ripples
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetRainActive(!isRaining);
            Debug.Log($"<color=#29b6f6><b>[ApronRainPuddleController]</b></color> Toggled Rain Surface FX: {(isRaining ? "RAINY (Puddles & Ripples Active)" : "DRYING UP")}");
        }
    }
}
