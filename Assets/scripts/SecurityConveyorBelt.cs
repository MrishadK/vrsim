using UnityEngine;

/// <summary>
/// Procedurally builds and animates a realistic X-ray security conveyor belt.
/// Creates the belt surface, roller supports, X-ray tunnel housing, and rubber flaps.
/// Items placed on the belt are pushed along via OnTriggerStay physics.
/// </summary>
public class SecurityConveyorBelt : MonoBehaviour
{
    [Header("Conveyor Dimensions")]
    [SerializeField] private float beltLength = 6f;
    [SerializeField] private float beltWidth = 1.2f;
    [SerializeField] private float beltHeight = 0.06f;
    [SerializeField] private float legHeight = 0.9f;

    [Header("Conveyor Speed")]
    [SerializeField] private float speed = 0.8f;
    [SerializeField] private Vector3 moveDirection = Vector3.right;

    [Header("X-Ray Tunnel")]
    [SerializeField] private float tunnelLength = 2.2f;
    [SerializeField] private float tunnelHeight = 1.1f;
    [SerializeField] private Color tunnelColor = new Color(0.15f, 0.15f, 0.18f, 1f);

    [Header("Appearance")]
    [SerializeField] private Color beltColor = new Color(0.12f, 0.12f, 0.14f, 1f);
    [SerializeField] private Color frameColor = new Color(0.35f, 0.35f, 0.38f, 1f);
    [SerializeField] private Color rollerColor = new Color(0.5f, 0.5f, 0.52f, 1f);

    // Runtime references
    private Material beltMaterial;
    private Vector2 textureOffset = Vector2.zero;
    private Transform[] rollers;
    private GameObject beltSurface;
    private AudioSource conveyorHumSource;

    /// <summary>The world-space entry point of the conveyor (where trays get placed).</summary>
    public Vector3 EntryPoint => transform.TransformPoint(new Vector3(-beltLength * 0.45f, legHeight + beltHeight, 0f));

    /// <summary>The world-space exit point of the conveyor (where trays arrive).</summary>
    public Vector3 ExitPoint => transform.TransformPoint(new Vector3(beltLength * 0.45f, legHeight + beltHeight, 0f));

    /// <summary>World-space direction items travel along the belt.</summary>
    public Vector3 WorldMoveDirection => transform.TransformDirection(moveDirection.normalized);

    /// <summary>The height of the belt surface in world Y.</summary>
    public float SurfaceY => transform.position.y + legHeight + beltHeight * 0.5f;

    private void Start()
    {
        BuildConveyor();
        CreateConveyorHum();
    }

    private void Update()
    {
        // Animate belt texture scrolling
        if (beltMaterial != null)
        {
            textureOffset.x += speed * Time.deltaTime * 0.3f;
            beltMaterial.SetTextureOffset("_BaseMap", textureOffset);
            if (beltMaterial.HasProperty("_MainTex"))
                beltMaterial.SetTextureOffset("_MainTex", textureOffset);
        }

        // Spin rollers
        if (rollers != null)
        {
            float spinSpeed = speed * 360f;
            foreach (var roller in rollers)
            {
                if (roller != null)
                    roller.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)
        {
            Vector3 worldDir = transform.TransformDirection(moveDirection.normalized);
            other.attachedRigidbody.position += worldDir * (speed * Time.deltaTime);
        }
    }

    // ─────────────────────────────────────────────
    //  PROCEDURAL CONSTRUCTION
    // ─────────────────────────────────────────────

    private void BuildConveyor()
    {
        // === BELT SURFACE ===
        beltSurface = CreatePrimitive("Belt_Surface", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight * 0.5f, 0f),
            new Vector3(beltLength, beltHeight, beltWidth),
            beltColor, isTrigger: true);

        beltMaterial = beltSurface.GetComponent<Renderer>().material;

        // === FRAME / SIDE RAILS ===
        float railThickness = 0.04f;
        float railHeight = 0.12f;

        // Left rail
        CreatePrimitive("Rail_Left", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight + railHeight * 0.5f, -beltWidth * 0.5f - railThickness * 0.5f),
            new Vector3(beltLength, railHeight, railThickness),
            frameColor);

        // Right rail
        CreatePrimitive("Rail_Right", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight + railHeight * 0.5f, beltWidth * 0.5f + railThickness * 0.5f),
            new Vector3(beltLength, railHeight, railThickness),
            frameColor);

        // === LEGS ===
        float legThickness = 0.08f;
        Vector3[] legPositions = new Vector3[]
        {
            new Vector3(-beltLength * 0.4f, legHeight * 0.5f, -beltWidth * 0.4f),
            new Vector3(-beltLength * 0.4f, legHeight * 0.5f,  beltWidth * 0.4f),
            new Vector3( beltLength * 0.4f, legHeight * 0.5f, -beltWidth * 0.4f),
            new Vector3( beltLength * 0.4f, legHeight * 0.5f,  beltWidth * 0.4f),
        };

        for (int i = 0; i < legPositions.Length; i++)
        {
            CreatePrimitive($"Leg_{i}", PrimitiveType.Cube,
                legPositions[i],
                new Vector3(legThickness, legHeight, legThickness),
                frameColor);
        }

        // === ROLLERS ===
        rollers = new Transform[4];
        float rollerRadius = 0.06f;
        float[] rollerXPositions = new float[]
        {
            -beltLength * 0.48f, -beltLength * 0.16f,
             beltLength * 0.16f,  beltLength * 0.48f
        };

        for (int i = 0; i < rollerXPositions.Length; i++)
        {
            var roller = CreatePrimitive($"Roller_{i}", PrimitiveType.Cylinder,
                new Vector3(rollerXPositions[i], legHeight, 0f),
                new Vector3(rollerRadius * 2f, beltWidth * 0.48f, rollerRadius * 2f),
                rollerColor);
            roller.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rollers[i] = roller.transform;

            // Disable collider on rollers
            var col = roller.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // === X-RAY TUNNEL ===
        BuildTunnel();

        // === RUBBER FLAPS (at tunnel entry/exit) ===
        BuildFlaps();
    }

    private void BuildTunnel()
    {
        float wallThickness = 0.06f;

        // Top
        CreatePrimitive("Tunnel_Top", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight + tunnelHeight, 0f),
            new Vector3(tunnelLength, wallThickness, beltWidth + wallThickness * 2f + 0.08f),
            tunnelColor);

        // Left wall
        CreatePrimitive("Tunnel_Left", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight + tunnelHeight * 0.5f, -beltWidth * 0.5f - wallThickness * 0.5f - 0.04f),
            new Vector3(tunnelLength, tunnelHeight, wallThickness),
            tunnelColor);

        // Right wall
        CreatePrimitive("Tunnel_Right", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight + tunnelHeight * 0.5f, beltWidth * 0.5f + wallThickness * 0.5f + 0.04f),
            new Vector3(tunnelLength, tunnelHeight, wallThickness),
            tunnelColor);

        // Back panel (behind belt, visible from entry side)
        CreatePrimitive("Tunnel_Back", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight + tunnelHeight * 0.5f + 0.3f, 0f),
            new Vector3(tunnelLength, tunnelHeight * 0.4f, beltWidth + 0.16f),
            new Color(0.08f, 0.08f, 0.1f, 1f));

        // Screen on top (simulated monitor)
        var screen = CreatePrimitive("XRay_Screen", PrimitiveType.Cube,
            new Vector3(0f, legHeight + beltHeight + tunnelHeight + 0.35f, -beltWidth * 0.5f - 0.12f),
            new Vector3(0.8f, 0.5f, 0.04f),
            new Color(0.1f, 0.2f, 0.1f, 1f));

        // Make screen slightly emissive
        var screenMat = screen.GetComponent<Renderer>().material;
        screenMat.EnableKeyword("_EMISSION");
        screenMat.SetColor("_EmissionColor", new Color(0.05f, 0.15f, 0.05f, 1f));

        // Screen stand
        CreatePrimitive("Screen_Stand", PrimitiveType.Cylinder,
            new Vector3(0f, legHeight + beltHeight + tunnelHeight + 0.12f, -beltWidth * 0.5f - 0.12f),
            new Vector3(0.04f, 0.12f, 0.04f),
            frameColor);
    }

    private void BuildFlaps()
    {
        float flapWidth = beltWidth / 8f;
        float flapHeight = 0.4f;
        float flapThickness = 0.008f;

        float[] flapXPositions = new float[] { -tunnelLength * 0.5f, tunnelLength * 0.5f };
        Color flapColor = new Color(0.05f, 0.05f, 0.07f, 0.85f);

        foreach (float xPos in flapXPositions)
        {
            for (int i = 0; i < 6; i++)
            {
                float zOff = (i - 2.5f) * flapWidth;
                var flap = CreatePrimitive($"Flap_{xPos:F0}_{i}", PrimitiveType.Cube,
                    new Vector3(xPos, legHeight + beltHeight + flapHeight * 0.5f + 0.02f, zOff),
                    new Vector3(flapThickness, flapHeight, flapWidth * 0.9f),
                    flapColor);

                var col = flap.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  CONVEYOR HUM AUDIO
    // ─────────────────────────────────────────────

    private void CreateConveyorHum()
    {
        conveyorHumSource = gameObject.AddComponent<AudioSource>();
        conveyorHumSource.loop = true;
        conveyorHumSource.volume = 0.12f;
        conveyorHumSource.spatialBlend = 1f;
        conveyorHumSource.minDistance = 1f;
        conveyorHumSource.maxDistance = 15f;
        conveyorHumSource.rolloffMode = AudioRolloffMode.Linear;

        // Generate a low-frequency conveyor hum
        int sampleRate = 44100;
        int lengthSamples = sampleRate * 2; // 2 seconds looping
        float[] samples = new float[lengthSamples];

        for (int i = 0; i < lengthSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Low motor hum: 60Hz + 120Hz harmonics + noise
            float hum = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.4f;
            hum += Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.2f;
            hum += Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.08f;
            // Add slight mechanical noise
            hum += (Random.value - 0.5f) * 0.06f;
            samples[i] = hum;
        }

        AudioClip humClip = AudioClip.Create("ConveyorHum", lengthSamples, 1, sampleRate, false);
        humClip.SetData(samples, 0);
        conveyorHumSource.clip = humClip;
        conveyorHumSource.Play();
    }

    // ─────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────

    private GameObject CreatePrimitive(string objName, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color, bool isTrigger = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = objName;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.3f);
            renderer.material = mat;
        }

        if (isTrigger)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        return go;
    }
}
