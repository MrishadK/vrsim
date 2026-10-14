using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A stationary security officer NPC that stands guard at the security checkpoint.
/// Faces approaching players, displays speech bubbles with instructions,
/// and has subtle idle animation (procedural breathing/weight shift).
/// </summary>
public class SecurityOfficerNPC : MonoBehaviour
{
    [Header("Officer Setup")]
    [SerializeField] private float lookAtSpeed = 2.5f;
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private Color uniformColor = new Color(0.08f, 0.1f, 0.22f, 1f);
    [SerializeField] private Color badgeColor = new Color(0.85f, 0.72f, 0.15f, 1f);

    [Header("Speech Bubble")]
    [SerializeField] private float bubbleDuration = 5f;

    // Runtime
    private Transform playerTarget;
    private bool playerInRange;
    private bool hasGreeted;
    private bool hasClearedPlayer;
    private float idleTimer;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Transform headTransform;
    private GameObject activeBubble;

    // Procedural body parts (if no model loaded)
    private Transform bodyRoot;

    private void Start()
    {
        basePosition = transform.position;
        baseRotation = transform.rotation;

        // Try to find an Animator for a proper model
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
            ApplyUniformMaterial(animator);
        }
        else
        {
            // Build a procedural officer if no model is attached
            BuildProceduralOfficer();
        }
    }

    private void Update()
    {
        FindPlayer();
        UpdateLookAt();
        UpdateIdleAnimation();
        UpdateGreeting();
    }

    // ─────────────────────────────────────────────
    //  PLAYER DETECTION
    // ─────────────────────────────────────────────

    private void FindPlayer()
    {
        if (playerTarget == null)
        {
            var cam = Camera.main;
            if (cam != null) playerTarget = cam.transform;
        }

        if (playerTarget != null)
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            playerInRange = dist <= detectionRadius;
        }
    }

    private void UpdateLookAt()
    {
        if (!playerInRange || playerTarget == null) return;

        Vector3 lookDir = playerTarget.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookAtSpeed);
        }
    }

    private void UpdateGreeting()
    {
        if (playerInRange && !hasGreeted)
        {
            hasGreeted = true;
            ShowSpeechBubble("Please place your belongings\nin the tray and step through\nthe scanner.");
        }
    }

    /// <summary>
    /// Called by SecurityCheckController after the tray passes through.
    /// </summary>
    public void ClearPlayer()
    {
        if (!hasClearedPlayer)
        {
            hasClearedPlayer = true;
            ShowSpeechBubble("All clear. Collect your\nbelongings and proceed.\nHave a safe flight!");
        }
    }

    /// <summary>
    /// Reset so the officer can greet the next player.
    /// </summary>
    public void ResetOfficer()
    {
        hasGreeted = false;
        hasClearedPlayer = false;
        if (activeBubble != null) Destroy(activeBubble);
    }

    // ─────────────────────────────────────────────
    //  IDLE ANIMATION (Procedural breathing / weight shift)
    // ─────────────────────────────────────────────

    private void UpdateIdleAnimation()
    {
        idleTimer += Time.deltaTime;

        // Subtle breathing — body rises/falls ~1cm
        float breathe = Mathf.Sin(idleTimer * 1.2f) * 0.005f;
        transform.position = basePosition + new Vector3(0f, breathe, 0f);

        // Subtle weight shift — lean side to side
        float lean = Mathf.Sin(idleTimer * 0.3f) * 0.3f;
        Quaternion leanRot = Quaternion.Euler(0f, 0f, lean);

        if (!playerInRange)
        {
            transform.rotation = baseRotation * leanRot;
        }
    }

    // ─────────────────────────────────────────────
    //  SPEECH BUBBLE
    // ─────────────────────────────────────────────

    private void ShowSpeechBubble(string message)
    {
        if (activeBubble != null) Destroy(activeBubble);

        activeBubble = new GameObject("OfficerSpeechBubble");

        Vector3 bubblePos = transform.position + Vector3.up * 2.3f;
        if (headTransform != null)
            bubblePos = headTransform.position + Vector3.up * 0.4f;

        activeBubble.transform.position = bubblePos;

        // Canvas
        Canvas canvas = activeBubble.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = activeBubble.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3f, 1f);
        activeBubble.transform.localScale = Vector3.one * 0.5f;

        // Background panel
        GameObject bgObj = new GameObject("BubbleBG", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(activeBubble.transform, false);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(0.02f, 0.05f, 0.12f, 0.92f);

        // Text
        GameObject textObj = new GameObject("BubbleText", typeof(RectTransform));
        textObj.transform.SetParent(activeBubble.transform, false);
        var tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = message;
        tmpText.fontSize = 0.28f;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableWordWrapping = true;
        tmpText.raycastTarget = false;

        var textRect = tmpText.rectTransform;
        textRect.anchorMin = new Vector2(0.05f, 0.05f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Billboard component
        activeBubble.AddComponent<SecurityBubbleBillboard>();

        // Auto-destroy
        Destroy(activeBubble, bubbleDuration);

        // Play a small notification sound
        PlayNotificationSound();
    }

    private void PlayNotificationSound()
    {
        AudioSource audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null)
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.spatialBlend = 1f;
            audioSrc.minDistance = 1f;
            audioSrc.maxDistance = 12f;
        }

        // Generate a soft two-tone notification
        int sampleRate = 44100;
        int length = sampleRate / 3; // ~0.33 seconds
        float[] samples = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float freq = t < 0.15f ? 880f : 1100f; // A5 → C#6
            float envelope = 1f - (t / (length / (float)sampleRate));
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.15f;
        }

        AudioClip clip = AudioClip.Create("OfficerNotif", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSrc.PlayOneShot(clip, 0.4f);
    }

    // ─────────────────────────────────────────────
    //  PROCEDURAL OFFICER (fallback if no model)
    // ─────────────────────────────────────────────

    private void BuildProceduralOfficer()
    {
        bodyRoot = new GameObject("OfficerBody").transform;
        bodyRoot.SetParent(transform, false);

        // Torso
        var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(bodyRoot, false);
        torso.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        torso.transform.localScale = new Vector3(0.45f, 0.55f, 0.3f);
        SetColor(torso, uniformColor);

        // Head
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(bodyRoot, false);
        head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        head.transform.localScale = new Vector3(0.28f, 0.3f, 0.28f);
        SetColor(head, new Color(0.76f, 0.6f, 0.46f, 1f)); // Skin tone
        headTransform = head.transform;

        // Cap
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "OfficerCap";
        cap.transform.SetParent(bodyRoot, false);
        cap.transform.localPosition = new Vector3(0f, 1.92f, 0f);
        cap.transform.localScale = new Vector3(0.3f, 0.04f, 0.3f);
        SetColor(cap, new Color(0.05f, 0.05f, 0.15f, 1f));

        // Cap visor
        var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor";
        visor.transform.SetParent(bodyRoot, false);
        visor.transform.localPosition = new Vector3(0f, 1.9f, 0.14f);
        visor.transform.localScale = new Vector3(0.22f, 0.015f, 0.1f);
        SetColor(visor, new Color(0.02f, 0.02f, 0.05f, 1f));

        // Badge
        var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        badge.name = "Badge";
        badge.transform.SetParent(bodyRoot, false);
        badge.transform.localPosition = new Vector3(-0.12f, 1.25f, 0.14f);
        badge.transform.localScale = new Vector3(0.06f, 0.08f, 0.01f);
        SetColor(badge, badgeColor);

        // Legs
        for (int i = 0; i < 2; i++)
        {
            float xOff = i == 0 ? -0.1f : 0.1f;
            var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = $"Leg_{i}";
            leg.transform.SetParent(bodyRoot, false);
            leg.transform.localPosition = new Vector3(xOff, 0.45f, 0f);
            leg.transform.localScale = new Vector3(0.18f, 0.45f, 0.18f);
            SetColor(leg, new Color(0.06f, 0.06f, 0.12f, 1f)); // Dark pants
        }

        // Shoes
        for (int i = 0; i < 2; i++)
        {
            float xOff = i == 0 ? -0.1f : 0.1f;
            var shoe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoe.name = $"Shoe_{i}";
            shoe.transform.SetParent(bodyRoot, false);
            shoe.transform.localPosition = new Vector3(xOff, 0.04f, 0.04f);
            shoe.transform.localScale = new Vector3(0.12f, 0.08f, 0.2f);
            SetColor(shoe, new Color(0.05f, 0.03f, 0.02f, 1f));
        }

        // Arms (at sides)
        for (int i = 0; i < 2; i++)
        {
            float xOff = i == 0 ? -0.28f : 0.28f;
            var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.name = $"Arm_{i}";
            arm.transform.SetParent(bodyRoot, false);
            arm.transform.localPosition = new Vector3(xOff, 0.9f, 0f);
            arm.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            SetColor(arm, uniformColor);
        }

        // Radio on shoulder
        var radio = GameObject.CreatePrimitive(PrimitiveType.Cube);
        radio.name = "Radio";
        radio.transform.SetParent(bodyRoot, false);
        radio.transform.localPosition = new Vector3(-0.2f, 1.4f, 0.05f);
        radio.transform.localScale = new Vector3(0.04f, 0.12f, 0.03f);
        SetColor(radio, new Color(0.1f, 0.1f, 0.1f, 1f));

        // Disable all colliders on the visual parts
        foreach (var col in bodyRoot.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Add a single capsule collider for the whole officer
        CapsuleCollider mainCol = gameObject.AddComponent<CapsuleCollider>();
        mainCol.center = new Vector3(0f, 1f, 0f);
        mainCol.height = 2f;
        mainCol.radius = 0.35f;
    }

    private void ApplyUniformMaterial(Animator animator)
    {
        // Try to color the mesh renderers to look like a uniform
        Renderer[] renderers = animator.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.name.ToLower().Contains("body") || r.name.ToLower().Contains("jacket") ||
                r.name.ToLower().Contains("torso") || r.name.ToLower().Contains("shirt"))
            {
                Material mat = new Material(r.sharedMaterial);
                mat.color = uniformColor;
                r.material = mat;
            }
        }
    }

    private void SetColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.25f);
            renderer.material = mat;
        }
    }
}

/// <summary>
/// Simple billboard that always faces the camera. Used for speech bubbles.
/// </summary>
public class SecurityBubbleBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = cam.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
