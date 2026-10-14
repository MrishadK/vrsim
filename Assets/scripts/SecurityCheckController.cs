using System.Collections;
using UnityEngine;

/// <summary>
/// Master controller for the security check sequence.
/// Orchestrates: popup → tray placement → item filling → conveyor transport → exit.
/// Works with PopupTrigger's onPopupClosed event to trigger the sequence.
/// </summary>
public class SecurityCheckController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SecurityConveyorBelt in the scene.")]
    [SerializeField] private SecurityConveyorBelt conveyor;

    [Tooltip("The SecurityOfficerNPC in the scene.")]
    [SerializeField] private SecurityOfficerNPC officer;

    [Header("Tray Settings")]
    [SerializeField] private Color trayColor = new Color(0.55f, 0.55f, 0.58f, 1f);
    [SerializeField] private float trayLength = 0.55f;
    [SerializeField] private float trayWidth = 0.35f;
    [SerializeField] private float trayWallHeight = 0.04f;

    [Header("Timing")]
    [SerializeField] private float itemPlaceDelay = 0.4f;
    [SerializeField] private float conveyorTravelTime = 6f;
    [SerializeField] private float postScanDelay = 1.5f;

    [Header("Metal Detector")]
    [SerializeField] private bool buildMetalDetector = true;
    [SerializeField] private Color detectorColor = new Color(0.6f, 0.6f, 0.65f, 1f);

    // Runtime
    private GameObject trayObject;
    private bool sequenceStarted;
    private bool sequenceComplete;
    private AudioSource sfxSource;

    private void Start()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.spatialBlend = 1f;
        sfxSource.minDistance = 1f;
        sfxSource.maxDistance = 20f;

        // Auto-find references if not set
        if (conveyor == null)
            conveyor = FindObjectOfType<SecurityConveyorBelt>();
        if (officer == null)
            officer = FindObjectOfType<SecurityOfficerNPC>();

        if (buildMetalDetector)
            BuildMetalDetectorArch();
    }

    /// <summary>
    /// Call this from PopupTrigger.onPopupClosed or any button event.
    /// This starts the full security check tray sequence.
    /// </summary>
    public void OnUnderstoodPressed()
    {
        if (sequenceStarted) return;
        sequenceStarted = true;
        StartCoroutine(SecuritySequence());
    }

    private IEnumerator SecuritySequence()
    {
        Debug.Log("[SecurityCheck] Sequence started — building tray...");

        // ── Step 1: Create tray at conveyor entry ──
        Vector3 entryPos = conveyor != null ? conveyor.EntryPoint : transform.position + Vector3.right * -2f;
        float surfaceY = conveyor != null ? conveyor.SurfaceY : transform.position.y + 1f;

        entryPos.y = surfaceY + 0.01f;
        trayObject = BuildTray(entryPos);

        PlayPlacementSound(0.6f);
        yield return new WaitForSeconds(0.5f);

        // ── Step 2: Fill tray with personal items ──
        Debug.Log("[SecurityCheck] Filling tray with items...");

        // Watch
        yield return new WaitForSeconds(itemPlaceDelay);
        CreateItemInTray("Watch", PrimitiveType.Cylinder,
            new Vector3(-0.12f, 0.035f, 0.08f),
            new Vector3(0.06f, 0.005f, 0.06f),
            new Color(0.75f, 0.75f, 0.78f, 1f));
        PlayPlacementSound(0.8f);

        // Watch band (small cube beside it)
        CreateItemInTray("WatchBand", PrimitiveType.Cube,
            new Vector3(-0.12f, 0.032f, 0.08f),
            new Vector3(0.08f, 0.003f, 0.015f),
            new Color(0.15f, 0.12f, 0.08f, 1f));

        // Belt
        yield return new WaitForSeconds(itemPlaceDelay);
        CreateBeltInTray();
        PlayPlacementSound(0.7f);

        // Phone
        yield return new WaitForSeconds(itemPlaceDelay);
        CreateItemInTray("Phone", PrimitiveType.Cube,
            new Vector3(0.08f, 0.033f, -0.06f),
            new Vector3(0.04f, 0.005f, 0.08f),
            new Color(0.08f, 0.08f, 0.1f, 1f));

        // Phone screen glow
        var phoneScreen = CreateItemInTray("PhoneScreen", PrimitiveType.Cube,
            new Vector3(0.08f, 0.0365f, -0.06f),
            new Vector3(0.032f, 0.001f, 0.065f),
            new Color(0.05f, 0.1f, 0.15f, 1f));
        MakeEmissive(phoneScreen, new Color(0.02f, 0.04f, 0.06f));
        PlayPlacementSound(0.9f);

        // Keys
        yield return new WaitForSeconds(itemPlaceDelay);
        CreateItemInTray("Key_1", PrimitiveType.Sphere,
            new Vector3(-0.05f, 0.035f, -0.08f),
            new Vector3(0.02f, 0.02f, 0.02f),
            new Color(0.85f, 0.75f, 0.2f, 1f));
        CreateItemInTray("Key_2", PrimitiveType.Sphere,
            new Vector3(-0.035f, 0.035f, -0.09f),
            new Vector3(0.018f, 0.018f, 0.018f),
            new Color(0.7f, 0.7f, 0.72f, 1f));
        CreateItemInTray("KeyRing", PrimitiveType.Cylinder,
            new Vector3(-0.04f, 0.035f, -0.085f),
            new Vector3(0.03f, 0.002f, 0.03f),
            new Color(0.8f, 0.8f, 0.82f, 1f));
        PlayPlacementSound(1.2f);

        // Wallet
        yield return new WaitForSeconds(itemPlaceDelay);
        CreateItemInTray("Wallet", PrimitiveType.Cube,
            new Vector3(0.12f, 0.035f, 0.05f),
            new Vector3(0.06f, 0.012f, 0.09f),
            new Color(0.25f, 0.12f, 0.05f, 1f)); // Brown leather
        PlayPlacementSound(0.65f);

        // Sunglasses
        yield return new WaitForSeconds(itemPlaceDelay);
        CreateItemInTray("Sunglasses_L", PrimitiveType.Sphere,
            new Vector3(0.0f, 0.035f, 0.0f),
            new Vector3(0.03f, 0.015f, 0.03f),
            new Color(0.1f, 0.08f, 0.06f, 1f));
        CreateItemInTray("Sunglasses_R", PrimitiveType.Sphere,
            new Vector3(0.035f, 0.035f, 0.0f),
            new Vector3(0.03f, 0.015f, 0.03f),
            new Color(0.1f, 0.08f, 0.06f, 1f));
        CreateItemInTray("Sunglasses_Bridge", PrimitiveType.Cube,
            new Vector3(0.0175f, 0.035f, 0.0f),
            new Vector3(0.015f, 0.003f, 0.005f),
            new Color(0.08f, 0.06f, 0.04f, 1f));
        PlayPlacementSound(0.85f);

        yield return new WaitForSeconds(0.6f);

        Debug.Log("[SecurityCheck] Tray filled — starting conveyor transport...");

        // ── Step 3: Move tray along conveyor ──
        yield return StartCoroutine(MoveTrayAlongConveyor());

        // ── Step 4: Metal detector scan sound ──
        PlayScannerBeep();
        yield return new WaitForSeconds(postScanDelay);

        // ── Step 5: Officer clears player ──
        if (officer != null)
            officer.ClearPlayer();

        sequenceComplete = true;
        Debug.Log("[SecurityCheck] Sequence complete — player cleared!");
    }

    // ─────────────────────────────────────────────
    //  TRAY CONSTRUCTION
    // ─────────────────────────────────────────────

    private GameObject BuildTray(Vector3 position)
    {
        var tray = new GameObject("SecurityTray");
        tray.transform.position = position;

        // Base
        var basePlate = CreatePart(tray.transform, "TrayBase", PrimitiveType.Cube,
            Vector3.zero,
            new Vector3(trayLength, 0.012f, trayWidth),
            trayColor);

        // Walls
        float wallThick = 0.008f;

        // Front wall
        CreatePart(tray.transform, "TrayWall_Front", PrimitiveType.Cube,
            new Vector3(trayLength * 0.5f - wallThick * 0.5f, trayWallHeight * 0.5f, 0f),
            new Vector3(wallThick, trayWallHeight, trayWidth),
            trayColor);

        // Back wall
        CreatePart(tray.transform, "TrayWall_Back", PrimitiveType.Cube,
            new Vector3(-trayLength * 0.5f + wallThick * 0.5f, trayWallHeight * 0.5f, 0f),
            new Vector3(wallThick, trayWallHeight, trayWidth),
            trayColor);

        // Left wall
        CreatePart(tray.transform, "TrayWall_Left", PrimitiveType.Cube,
            new Vector3(0f, trayWallHeight * 0.5f, -trayWidth * 0.5f + wallThick * 0.5f),
            new Vector3(trayLength, trayWallHeight, wallThick),
            trayColor);

        // Right wall
        CreatePart(tray.transform, "TrayWall_Right", PrimitiveType.Cube,
            new Vector3(0f, trayWallHeight * 0.5f, trayWidth * 0.5f - wallThick * 0.5f),
            new Vector3(trayLength, trayWallHeight, wallThick),
            trayColor);

        return tray;
    }

    private void CreateBeltInTray()
    {
        if (trayObject == null) return;

        // Belt = a series of connected cubes to look like a coiled belt
        Color beltLeather = new Color(0.18f, 0.1f, 0.04f, 1f);
        Color buckleColor = new Color(0.8f, 0.78f, 0.7f, 1f);

        // Belt strap (curved by using multiple segments)
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            float radius = 0.04f;
            float x = -0.04f + Mathf.Cos(angle) * radius * 0.5f;
            float z = 0.06f + Mathf.Sin(angle) * radius * 0.5f;

            CreateItemInTray($"BeltSegment_{i}", PrimitiveType.Cube,
                new Vector3(x, 0.033f, z),
                new Vector3(0.04f, 0.003f, 0.015f),
                beltLeather);
        }

        // Buckle
        CreateItemInTray("BeltBuckle", PrimitiveType.Cube,
            new Vector3(-0.04f, 0.036f, 0.06f),
            new Vector3(0.025f, 0.004f, 0.02f),
            buckleColor);
    }

    // ─────────────────────────────────────────────
    //  CONVEYOR TRANSPORT
    // ─────────────────────────────────────────────

    private IEnumerator MoveTrayAlongConveyor()
    {
        if (trayObject == null || conveyor == null)
        {
            yield return new WaitForSeconds(conveyorTravelTime);
            yield break;
        }

        Vector3 startPos = trayObject.transform.position;
        Vector3 endPos = conveyor.ExitPoint;
        endPos.y = startPos.y;

        float elapsed = 0f;

        while (elapsed < conveyorTravelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / conveyorTravelTime);
            trayObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        trayObject.transform.position = endPos;

        // Destroy tray after a while
        Destroy(trayObject, 15f);
    }

    // ─────────────────────────────────────────────
    //  METAL DETECTOR ARCH
    // ─────────────────────────────────────────────

    private void BuildMetalDetectorArch()
    {
        var detector = new GameObject("MetalDetectorArch");
        detector.transform.SetParent(transform, false);

        // Position it beside the conveyor
        Vector3 archPos = Vector3.zero;
        if (conveyor != null)
        {
            archPos = conveyor.transform.position + conveyor.transform.forward * 2f;
        }
        detector.transform.localPosition = archPos;

        float archWidth = 1.0f;
        float archHeight = 2.3f;
        float pillarThick = 0.12f;
        float pillarDepth = 0.3f;

        // Left pillar
        CreatePart(detector.transform, "Pillar_Left", PrimitiveType.Cube,
            new Vector3(-archWidth * 0.5f, archHeight * 0.5f, 0f),
            new Vector3(pillarThick, archHeight, pillarDepth),
            detectorColor);

        // Right pillar
        CreatePart(detector.transform, "Pillar_Right", PrimitiveType.Cube,
            new Vector3(archWidth * 0.5f, archHeight * 0.5f, 0f),
            new Vector3(pillarThick, archHeight, pillarDepth),
            detectorColor);

        // Top beam
        CreatePart(detector.transform, "TopBeam", PrimitiveType.Cube,
            new Vector3(0f, archHeight, 0f),
            new Vector3(archWidth + pillarThick, pillarThick, pillarDepth),
            detectorColor);

        // Indicator light (green = ok, but we start it off / red)
        var light = CreatePart(detector.transform, "IndicatorLight", PrimitiveType.Sphere,
            new Vector3(0f, archHeight + pillarThick * 0.5f + 0.04f, 0f),
            new Vector3(0.06f, 0.06f, 0.06f),
            new Color(0.1f, 0.6f, 0.1f, 1f));

        // Make it emissive
        MakeEmissive(light, new Color(0.05f, 0.3f, 0.05f, 1f));

        // Base plates
        for (int i = 0; i < 2; i++)
        {
            float xOff = i == 0 ? -archWidth * 0.5f : archWidth * 0.5f;
            CreatePart(detector.transform, $"BasePlate_{i}", PrimitiveType.Cube,
                new Vector3(xOff, 0.02f, 0f),
                new Vector3(pillarThick + 0.08f, 0.04f, pillarDepth + 0.1f),
                new Color(0.3f, 0.3f, 0.33f, 1f));
        }
    }

    // ─────────────────────────────────────────────
    //  SOUND EFFECTS
    // ─────────────────────────────────────────────

    private void PlayPlacementSound(float pitch = 1f)
    {
        if (sfxSource == null) return;

        int sampleRate = 44100;
        int length = sampleRate / 8; // Very short click
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 40f); // Fast decay
            // Soft thud + click
            samples[i] = (Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.5f +
                          Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.15f +
                          (Random.value - 0.5f) * 0.2f) * envelope;
        }

        AudioClip clip = AudioClip.Create("PlaceItem", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, 0.3f);
    }

    private void PlayScannerBeep()
    {
        if (sfxSource == null) return;

        int sampleRate = 44100;
        int length = sampleRate; // 1 second
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;

            // Three short beeps
            float beepEnvelope = 0f;
            if (t < 0.15f) beepEnvelope = 1f;
            else if (t > 0.2f && t < 0.35f) beepEnvelope = 1f;
            else if (t > 0.4f && t < 0.55f) beepEnvelope = 1f;

            // Smooth edges
            float fadeIn = Mathf.Clamp01(t * 100f);
            float fadeOut = Mathf.Clamp01((1f - t) * 20f);

            samples[i] = Mathf.Sin(2f * Mathf.PI * 2400f * t) * beepEnvelope * fadeIn * fadeOut * 0.2f;
        }

        AudioClip clip = AudioClip.Create("ScannerBeep", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, 0.5f);
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private GameObject CreateItemInTray(string itemName, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
    {
        if (trayObject == null) return null;

        var item = GameObject.CreatePrimitive(type);
        item.name = itemName;
        item.transform.SetParent(trayObject.transform, false);
        item.transform.localPosition = localPos;
        item.transform.localScale = localScale;

        SetMaterial(item, color);

        // Disable collider on small items to avoid physics chaos
        var col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        return item;
    }

    private GameObject CreatePart(Transform parent, string partName, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        SetMaterial(part, color);
        return part;
    }

    private void SetMaterial(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.35f);
            renderer.material = mat;
        }
    }

    private void MakeEmissive(GameObject go, Color emissionColor)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", emissionColor);
        }
    }
}
