using UnityEngine;

/// <summary>
/// AeroSim VR — High-Fidelity Dynamic Footstep Audio Engine.
/// Synthesizes and plays authentic, surface-responsive footstep sounds synchronized
/// with player gait and stride cycles.
/// 
/// Automatically detects surface material (polished terminal marble/tile, carpet lounge, tarmac/apron)
/// and modulates pitch/volume between left and right footfalls.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    public enum SurfaceType
    {
        TerminalTile,
        CarpetLounge,
        ApronTarmac
    }

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float footstepVolume = 0.55f;
    [Range(0.8f, 1.2f)] public float basePitch = 1.0f;
    [Tooltip("Layer mask for ground detection.")]
    public LayerMask groundLayerMask = ~0;

    private AudioSource audioSource;
    private AirportLocomotion locomotion;

    // Procedural cached footstep audio clips for zero-dependency reliability
    private AudioClip tileClipLeft;
    private AudioClip tileClipRight;
    private AudioClip carpetClip;
    private AudioClip tarmacClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.4f; // Semi-spatialized so player hears it at their feet
        audioSource.loop = false;

        locomotion = GetComponent<AirportLocomotion>() ?? GetComponentInParent<AirportLocomotion>();

        GenerateAcousticClips();
    }

    private void OnEnable()
    {
        if (locomotion != null)
        {
            locomotion.OnFootstep += HandleFootstep;
        }
    }

    private void OnDisable()
    {
        if (locomotion != null)
        {
            locomotion.OnFootstep -= HandleFootstep;
        }
    }

    public void BindLocomotion(AirportLocomotion loc)
    {
        if (locomotion != null)
        {
            locomotion.OnFootstep -= HandleFootstep;
        }
        locomotion = loc;
        if (locomotion != null && isActiveAndEnabled)
        {
            locomotion.OnFootstep += HandleFootstep;
        }
    }

    private void HandleFootstep(bool isLeftFoot, float intensity)
    {
        if (audioSource == null) return;

        SurfaceType surface = DetectSurface();
        AudioClip clip = GetClipForSurface(surface, isLeftFoot);
        if (clip == null) return;

        // Modulate pitch slightly per foot (human feet produce slightly different resonances)
        float footPitchVariance = isLeftFoot ? 0.98f : 1.02f;
        float randomJitter = Random.Range(-0.02f, 0.02f);
        audioSource.pitch = basePitch * footPitchVariance + randomJitter;

        // Modulate stereo pan for left vs right foot
        audioSource.panStereo = isLeftFoot ? -0.15f : 0.15f;

        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float finalVolume = footstepVolume * intensity * masterVol;

        audioSource.PlayOneShot(clip, Mathf.Clamp01(finalVolume));
    }

    private SurfaceType DetectSurface()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.3f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1.2f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            string name = hit.collider.gameObject.name.ToLower();
            if (name.Contains("carpet") || name.Contains("rug") || name.Contains("lounge"))
                return SurfaceType.CarpetLounge;

            if (name.Contains("tarmac") || name.Contains("road") || name.Contains("asphalt") || name.Contains("apron") || name.Contains("runway") || name.Contains("jetway"))
                return SurfaceType.ApronTarmac;
        }

        // Default: Polished airport terminal concourse tile
        return SurfaceType.TerminalTile;
    }

    private AudioClip GetClipForSurface(SurfaceType surface, bool isLeftFoot)
    {
        switch (surface)
        {
            case SurfaceType.CarpetLounge:
                return carpetClip;
            case SurfaceType.ApronTarmac:
                return tarmacClip;
            case SurfaceType.TerminalTile:
            default:
                return isLeftFoot ? tileClipLeft : tileClipRight;
        }
    }

    /// <summary>
    /// Procedurally synthesizes authentic acoustic footsteps in memory.
    /// Eliminates external asset dependencies and ensures crisp, clean audio.
    /// </summary>
    private void GenerateAcousticClips()
    {
        tileClipLeft = SynthesizeFootstep(44100, 0.09f, 2200f, 320f, 0.65f, "Step_Tile_Left");
        tileClipRight = SynthesizeFootstep(44100, 0.09f, 2400f, 350f, 0.70f, "Step_Tile_Right");
        carpetClip = SynthesizeFootstep(44100, 0.11f, 850f, 180f, 0.25f, "Step_Carpet");
        tarmacClip = SynthesizeFootstep(44100, 0.12f, 1600f, 240f, 0.50f, "Step_Tarmac");
    }

    private static AudioClip SynthesizeFootstep(int sampleRate, float duration, float clickFreq, float thudFreq, float crispness, string name)
    {
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 45f); // Fast decay envelope

            // Crisp sole impact (high frequency click/friction)
            float click = Mathf.Sin(2f * Mathf.PI * clickFreq * t) * Mathf.Exp(-t * 90f);
            
            // Sub-surface body thud (low frequency floor resonance)
            float thud = Mathf.Sin(2f * Mathf.PI * thudFreq * t) * Mathf.Exp(-t * 30f);

            // Subtle textured noise
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 60f) * 0.15f;

            samples[i] = (click * crispness + thud * (1f - crispness * 0.5f) + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
