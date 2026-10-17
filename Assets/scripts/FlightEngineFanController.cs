using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// AeroSim Aviation — High-Fidelity Flight Engine Turbofan Controller.
/// Spins aircraft turbofans like real flight engines with authentic spooling physics,
/// titanium rotor inertia, dual-engine micro-drift, 3D spatial acoustic whine,
/// and automatic extraction for combined meshes like 'Material2.010'.
/// </summary>
[ExecuteAlways]
[SelectionBase]
public class FlightEngineFanController : MonoBehaviour
{
    public enum EngineState
    {
        Cutoff = 0,
        Starting = 1,
        Idle = 2,
        Taxi = 3,
        Cruise = 4,
        TakeoffTOGA = 5,
        ShuttingDown = 6
    }

    [Header("Engine Power & Throttle")]
    [Tooltip("Current operational flight engine state.")]
    public EngineState engineState = EngineState.Idle;

    [Range(0f, 1f)]
    [Tooltip("Throttle lever position from 0% (Cutoff/Idle) to 100% (TOGA).")]
    public float throttle = 0.22f; // Default Ground Idle (~22% N1)

    [Header("Turbofan RPM Specs")]
    [Tooltip("Ground idle RPM of the turbofan N1 rotor.")]
    public float idleRPM = 1050f;

    [Tooltip("Full Takeoff (TOGA) max RPM of the turbofan N1 rotor.")]
    public float maxRPM = 4850f;

    [Tooltip("Windmilling RPM caused by ambient airflow when engines are shut down.")]
    public float windmillingRPM = 18f;

    [Header("Physics & Spool Dynamics")]
    [Range(0.05f, 2f)]
    [Tooltip("Spool-up acceleration rate (real high-bypass jet engines take several seconds to spool).")]
    public float spoolAcceleration = 0.28f;

    [Range(0.05f, 2f)]
    [Tooltip("Spool-down deceleration rate with rotational inertia.")]
    public float spoolDeceleration = 0.22f;

    [Tooltip("Simulates slight micro-frequency drift between Left and Right engines for realistic dual-engine acoustic beating.")]
    public bool enableMicroDrift = true;

    [Tooltip("Clockwise rotation when viewed from the front of the aircraft nacelle (Rolls-Royce / GE standard).")]
    public bool clockwiseFromFront = true;

    [Tooltip("Local spin axis inside the fan's local transform space (along engine thrust centerline).")]
    public Vector3 spinAxis = Vector3.up;

    [Header("Rotor Hierarchy References")]
    [Tooltip("Left engine fan rotor assembly.")]
    public Transform leftTurbofanRotor;

    [Tooltip("Right engine fan rotor assembly.")]
    public Transform rightTurbofanRotor;

    [Header("Audio & Acoustic Feedback")]
    public bool enableAudio = true;
    public AudioClip turbofanAudioClip;
    [Range(0f, 1f)] public float masterAudioVolume = 0.75f;

    [Header("Visual Effects")]
    [Tooltip("Fade in high-RPM motion blur disc at speeds above 1,800 RPM to avoid visual wagon-wheel strobing.")]
    public bool enableMotionBlurDisc = true;

    [Header("Live Flight Deck Readouts")]
    [SerializeField] private float leftRPM = 1050f;
    [SerializeField] private float rightRPM = 1050f;
    [SerializeField] private float currentN1Percent = 21.6f;

    public float LeftRPM => leftRPM;
    public float RightRPM => rightRPM;
    public float N1Percent => currentN1Percent;

    [Header("Keyboard Controls (Play Mode)")]
    public bool enableKeyboardShortcuts = true;

    // Internal Audio Sources
    private AudioSource leftAudioSource;
    private AudioSource rightAudioSource;

    // Motion blur renderers
    private Renderer leftBlurDiscRenderer;
    private Renderer rightBlurDiscRenderer;

    private float leftAngle = 0f;
    private float rightAngle = 0f;
    private float microDriftTimer = 0f;

    private void Awake()
    {
        EnsureRotorsConfigured();
        SetupAudio();
        SetupMotionBlurDiscs();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            ApplyStateThrottle(engineState);
        }
    }

    private void Update()
    {
        HandleKeyboardInput();
        UpdateEnginePhysics();
        ApplyRotorRotation();
        UpdateAudio();
        UpdateMotionBlur();
    }

    // =========================================================================
    // 1. ROTOR AUTO-CONFIGURATION & MESH EXTRACTION FOR 'Material2.010'
    // =========================================================================

    [ContextMenu("Auto-Configure or Extract Rotors")]
    public void EnsureRotorsConfigured()
    {
        // 1. Check if child rotors already exist
        if (leftTurbofanRotor == null)
        {
            Transform found = transform.Find("Left_Turbofan_Rotor");
            if (found != null) leftTurbofanRotor = found;
        }
        if (rightTurbofanRotor == null)
        {
            Transform found = transform.Find("Right_Turbofan_Rotor");
            if (found != null) rightTurbofanRotor = found;
        }

        if (leftTurbofanRotor != null && rightTurbofanRotor != null)
        {
            return;
        }

        // 2. Check if we are attached to or near 'Material2.010'
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            // Search children or siblings
            Transform target010 = transform.Find("Material2.010");
            if (target010 == null && transform.parent != null)
            {
                target010 = transform.parent.Find("Material2.010");
            }
            if (target010 != null)
            {
                mf = target010.GetComponent<MeshFilter>();
            }
        }

        if (mf != null && mf.sharedMesh != null)
        {
            ExtractAndBuildTurbofans(mf);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoadedAutoSetup()
    {
        var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (var t in allTransforms)
        {
            if (t.name == "Material2.010" && t.GetComponent<FlightEngineFanController>() == null)
            {
                var ctrl = t.gameObject.AddComponent<FlightEngineFanController>();
                ctrl.EnsureRotorsConfigured();
                Debug.Log("<color=#00e676><b>[FlightEngineFanController]</b></color> Automatically detected Material2.010 on scene load and initiated flight engine fan simulation!");
            }
        }
    }

    public void ExtractAndBuildTurbofans(MeshFilter sourceMf)
    {
        GameObject sourceGo = sourceMf.gameObject;
        Mesh srcMesh = sourceMf.sharedMesh;
        MeshRenderer srcRenderer = sourceGo.GetComponent<MeshRenderer>();
        Material fanMat = srcRenderer != null ? srcRenderer.sharedMaterial : null;

        // Check for sibling overflow left blades (Material2.011) and cones (Material3.013)
        Transform parentT = sourceGo.transform.parent != null ? sourceGo.transform.parent : sourceGo.transform;
        Transform sib011 = parentT.Find("Material2.011");
        Transform sib013 = parentT.Find("Material3.013");

        Mesh mesh011 = sib011 != null && sib011.GetComponent<MeshFilter>() != null ? sib011.GetComponent<MeshFilter>().sharedMesh : null;
        Mesh mesh013 = sib013 != null && sib013.GetComponent<MeshFilter>() != null ? sib013.GetComponent<MeshFilter>().sharedMesh : null;

        Vector3 leftCenter = new Vector3(3.33858f, 3.11740f, 0.23506f);
        Vector3 rightCenter = new Vector3(4.91749f, 3.11740f, 0.24123f);

        Mesh leftMesh = null;
        Mesh rightMesh = null;

#if UNITY_EDITOR
        leftMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/GeneratedModels/Boeing787_LeftTurbofan.obj");
        rightMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/GeneratedModels/Boeing787_RightTurbofan.obj");
#endif

        if (leftMesh == null || rightMesh == null)
        {
            rightMesh = ExtractSubMeshAssembly(srcMesh, true, null, mesh013, out rightCenter);
            rightMesh.name = "Boeing787_RightTurbofan_Mesh";

            leftMesh = ExtractSubMeshAssembly(srcMesh, false, mesh011, mesh013, out leftCenter);
            leftMesh.name = "Boeing787_LeftTurbofan_Mesh";
        }

        // Create or configure Left Rotor GameObject
        if (leftTurbofanRotor == null)
        {
            GameObject leftGo = new GameObject("Left_Turbofan_Rotor");
            leftGo.transform.SetParent(sourceGo.transform, false);
            leftGo.transform.localPosition = leftCenter;
            leftGo.transform.localRotation = Quaternion.identity;
            leftGo.transform.localScale = Vector3.one;

            MeshFilter lmf = leftGo.AddComponent<MeshFilter>();
            lmf.sharedMesh = leftMesh;

            MeshRenderer lmr = leftGo.AddComponent<MeshRenderer>();
            lmr.sharedMaterial = fanMat;

            leftTurbofanRotor = leftGo.transform;
        }

        // Create or configure Right Rotor GameObject
        if (rightTurbofanRotor == null)
        {
            GameObject rightGo = new GameObject("Right_Turbofan_Rotor");
            rightGo.transform.SetParent(sourceGo.transform, false);
            rightGo.transform.localPosition = rightCenter;
            rightGo.transform.localRotation = Quaternion.identity;
            rightGo.transform.localScale = Vector3.one;

            MeshFilter rmf = rightGo.AddComponent<MeshFilter>();
            rmf.sharedMesh = rightMesh;

            MeshRenderer rmr = rightGo.AddComponent<MeshRenderer>();
            rmr.sharedMaterial = fanMat;

            rightTurbofanRotor = rightGo.transform;
        }

        // Hide original static combined renderers so there's no ghosting
        if (srcRenderer != null) srcRenderer.enabled = false;
        if (sib011 != null && sib011.GetComponent<MeshRenderer>() != null) sib011.GetComponent<MeshRenderer>().enabled = false;
        if (sib013 != null && sib013.GetComponent<MeshRenderer>() != null) sib013.GetComponent<MeshRenderer>().enabled = false;

        Debug.Log("<color=#00e676><b>[FlightEngineFanController]</b></color> Successfully extracted and centered Left & Right turbofans from Material2.010!");
    }

    private static Mesh ExtractSubMeshAssembly(Mesh primaryMesh, bool isRightSide, Mesh overflowMesh, Mesh coneMesh, out Vector3 center)
    {
        float splitThreshold = 4.128f; // Middle boundary between Left and Right engine nacelles
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> tris = new List<int>();

        Vector3[] pVerts = primaryMesh.vertices;
        Vector3[] pNorms = primaryMesh.normals;
        int[] pTris = primaryMesh.triangles;

        // Remap dictionary for primary mesh
        Dictionary<int, int> remap = new Dictionary<int, int>();

        for (int i = 0; i < pTris.Length; i += 3)
        {
            int i0 = pTris[i];
            int i1 = pTris[i + 1];
            int i2 = pTris[i + 2];

            bool match0 = isRightSide ? (pVerts[i0].x >= splitThreshold) : (pVerts[i0].x < splitThreshold);
            bool match1 = isRightSide ? (pVerts[i1].x >= splitThreshold) : (pVerts[i1].x < splitThreshold);
            bool match2 = isRightSide ? (pVerts[i2].x >= splitThreshold) : (pVerts[i2].x < splitThreshold);

            if (match0 && match1 && match2)
            {
                int new0 = GetOrAddVertex(i0, pVerts, pNorms, verts, normals, remap);
                int new1 = GetOrAddVertex(i1, pVerts, pNorms, verts, normals, remap);
                int new2 = GetOrAddVertex(i2, pVerts, pNorms, verts, normals, remap);

                tris.Add(new0);
                tris.Add(new1);
                tris.Add(new2);
            }
        }

        // Add overflow mesh (for Left engine, Material2.011)
        if (!isRightSide && overflowMesh != null)
        {
            int vertOffset = verts.Count;
            Vector3[] oVerts = overflowMesh.vertices;
            Vector3[] oNorms = overflowMesh.normals;
            int[] oTris = overflowMesh.triangles;

            verts.AddRange(oVerts);
            if (oNorms != null && oNorms.Length == oVerts.Length) normals.AddRange(oNorms);
            else for (int v = 0; v < oVerts.Length; v++) normals.Add(Vector3.up);

            for (int t = 0; t < oTris.Length; t++)
            {
                tris.Add(vertOffset + oTris[t]);
            }
        }

        // Add cone mesh (Material3.013)
        if (coneMesh != null)
        {
            Vector3[] cVerts = coneMesh.vertices;
            Vector3[] cNorms = coneMesh.normals;
            int[] cTris = coneMesh.triangles;
            Dictionary<int, int> coneRemap = new Dictionary<int, int>();

            for (int i = 0; i < cTris.Length; i += 3)
            {
                int i0 = cTris[i];
                int i1 = cTris[i + 1];
                int i2 = cTris[i + 2];

                bool match0 = isRightSide ? (cVerts[i0].x >= splitThreshold) : (cVerts[i0].x < splitThreshold);
                bool match1 = isRightSide ? (cVerts[i1].x >= splitThreshold) : (cVerts[i1].x < splitThreshold);
                bool match2 = isRightSide ? (cVerts[i2].x >= splitThreshold) : (cVerts[i2].x < splitThreshold);

                if (match0 && match1 && match2)
                {
                    int new0 = GetOrAddVertex(i0, cVerts, cNorms, verts, normals, coneRemap);
                    int new1 = GetOrAddVertex(i1, cVerts, cNorms, verts, normals, coneRemap);
                    int new2 = GetOrAddVertex(i2, cVerts, cNorms, verts, normals, coneRemap);

                    tris.Add(new0);
                    tris.Add(new1);
                    tris.Add(new2);
                }
            }
        }

        // Calculate exact bounding box and center
        Vector3 min = verts[0];
        Vector3 max = verts[0];
        for (int v = 1; v < verts.Count; v++)
        {
            min = Vector3.Min(min, verts[v]);
            max = Vector3.Max(max, verts[v]);
        }
        center = (min + max) * 0.5f;

        // Shift all vertices so center is exact (0, 0, 0)
        Vector3[] centeredVerts = new Vector3[verts.Count];
        for (int v = 0; v < verts.Count; v++)
        {
            centeredVerts[v] = verts[v] - center;
        }

        Mesh result = new Mesh();
        result.indexFormat = IndexFormat.UInt32;
        result.vertices = centeredVerts;
        result.normals = normals.ToArray();
        result.triangles = tris.ToArray();
        result.RecalculateBounds();
        return result;
    }

    private static int GetOrAddVertex(int oldIdx, Vector3[] srcVerts, Vector3[] srcNorms, List<Vector3> verts, List<Vector3> norms, Dictionary<int, int> remap)
    {
        if (remap.TryGetValue(oldIdx, out int newIdx))
        {
            return newIdx;
        }
        newIdx = verts.Count;
        remap[oldIdx] = newIdx;
        verts.Add(srcVerts[oldIdx]);
        if (srcNorms != null && srcNorms.Length > oldIdx) norms.Add(srcNorms[oldIdx]);
        else norms.Add(Vector3.up);
        return newIdx;
    }

    // =========================================================================
    // 2. REALISTIC TURBOFAN PHYSICS & SPOOLING DYNAMICS
    // =========================================================================

    private void UpdateEnginePhysics()
    {
        float targetRPM = 0f;

        switch (engineState)
        {
            case EngineState.Cutoff:
                targetRPM = windmillingRPM;
                break;
            case EngineState.Starting:
                targetRPM = idleRPM;
                break;
            case EngineState.Idle:
                targetRPM = idleRPM;
                break;
            case EngineState.Taxi:
                targetRPM = Mathf.Lerp(idleRPM, maxRPM, 0.25f);
                break;
            case EngineState.Cruise:
                targetRPM = Mathf.Lerp(idleRPM, maxRPM, 0.75f);
                break;
            case EngineState.TakeoffTOGA:
                targetRPM = maxRPM;
                break;
            case EngineState.ShuttingDown:
                targetRPM = windmillingRPM;
                break;
        }

        // Allow manual throttle blending when in dynamic flight modes
        if (engineState != EngineState.Cutoff && engineState != EngineState.ShuttingDown)
        {
            float throttleRPM = Mathf.Lerp(idleRPM, maxRPM, throttle);
            targetRPM = Mathf.Max(targetRPM, throttleRPM);
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) dt = 0.016f;

        // Independent spooling for Left and Right engines with micro-drift
        microDriftTimer += dt * 0.45f;
        float drift = enableMicroDrift ? Mathf.Sin(microDriftTimer) * 0.018f : 0f;

        float targetLeft = targetRPM * (1f + drift);
        float targetRight = targetRPM * (1f - drift);

        // Smooth aerodynamic spool acceleration / deceleration
        float leftRate = targetLeft > leftRPM ? spoolAcceleration : spoolDeceleration;
        float rightRate = targetRight > rightRPM ? spoolAcceleration : spoolDeceleration;

        leftRPM = Mathf.Lerp(leftRPM, targetLeft, dt * leftRate * 4f);
        rightRPM = Mathf.Lerp(rightRPM, targetRight, dt * rightRate * 4f);

        // Live % N1 readout (Idle = ~22%, TOGA = 100%)
        currentN1Percent = Mathf.Clamp((leftRPM / maxRPM) * 100f, 0f, 105f);
    }

    private void ApplyRotorRotation()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) dt = 0.016f;

        float dir = clockwiseFromFront ? -1f : 1f;

        // Convert RPM to Degrees Per Second: (RPM / 60) * 360 = RPM * 6
        float leftDelta = leftRPM * 6f * dt * dir;
        float rightDelta = rightRPM * 6f * dt * dir;

        Vector3 axis = spinAxis.normalized;

        if (leftTurbofanRotor != null)
        {
            leftTurbofanRotor.Rotate(axis, leftDelta, Space.Self);
        }
        if (rightTurbofanRotor != null)
        {
            rightTurbofanRotor.Rotate(axis, rightDelta, Space.Self);
        }
    }

    // =========================================================================
    // 3. MOTION BLUR DISC VISUALS
    // =========================================================================

    private void SetupMotionBlurDiscs()
    {
        if (!enableMotionBlurDisc) return;

        // Create or find blurred disc quad on Left fan
        if (leftTurbofanRotor != null && leftBlurDiscRenderer == null)
        {
            Transform existing = leftTurbofanRotor.Find("Turbofan_Blur_Disc");
            if (existing != null) leftBlurDiscRenderer = existing.GetComponent<Renderer>();
            else leftBlurDiscRenderer = CreateBlurDisc(leftTurbofanRotor, "Left");
        }

        // Create or find blurred disc quad on Right fan
        if (rightTurbofanRotor != null && rightBlurDiscRenderer == null)
        {
            Transform existing = rightTurbofanRotor.Find("Turbofan_Blur_Disc");
            if (existing != null) rightBlurDiscRenderer = existing.GetComponent<Renderer>();
            else rightBlurDiscRenderer = CreateBlurDisc(rightTurbofanRotor, "Right");
        }
    }

    private Renderer CreateBlurDisc(Transform parent, string side)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        disc.name = "Turbofan_Blur_Disc";
        disc.transform.SetParent(parent, false);

        // Orient perpendicular to the spin axis (Y)
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        disc.transform.localPosition = new Vector3(0f, 0.015f, 0f); // Just in front of blade disc
        disc.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);

        Collider col = disc.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        // Load blur disc texture
        Texture2D blurTex = Resources.Load<Texture2D>("Turbofan_Motion_Blur_Disc");
#if UNITY_EDITOR
        if (blurTex == null)
        {
            blurTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/planetextures/Turbofan_Motion_Blur_Disc.png");
        }
#endif
        Shader unlitTransparent = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material blurMat = new Material(unlitTransparent);
        blurMat.name = $"Mat_TurbofanBlur_{side}";

        if (blurTex != null)
        {
            blurMat.mainTexture = blurTex;
            blurMat.SetTexture("_BaseMap", blurTex);
        }

        // Enable transparency
        blurMat.SetFloat("_Surface", 1); // Transparent in URP
        blurMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        blurMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        blurMat.SetInt("_ZWrite", 0);
        blurMat.renderQueue = 3000;

        Renderer r = disc.GetComponent<Renderer>();
        r.sharedMaterial = blurMat;
        r.enabled = false;
        return r;
    }

    private void UpdateMotionBlur()
    {
        if (!enableMotionBlurDisc) return;

        // Disc opacity transitions from 0 at 1,500 RPM to full at 3,200 RPM
        float blurFactor = Mathf.InverseLerp(1500f, 3200f, (leftRPM + rightRPM) * 0.5f);
        bool showBlur = blurFactor > 0.05f;

        if (leftBlurDiscRenderer != null)
        {
            leftBlurDiscRenderer.enabled = showBlur;
            if (showBlur && leftBlurDiscRenderer.sharedMaterial != null)
            {
                Color c = Color.white;
                c.a = blurFactor * 0.85f;
                leftBlurDiscRenderer.sharedMaterial.color = c;
            }
        }

        if (rightBlurDiscRenderer != null)
        {
            rightBlurDiscRenderer.enabled = showBlur;
            if (showBlur && rightBlurDiscRenderer.sharedMaterial != null)
            {
                Color c = Color.white;
                c.a = blurFactor * 0.85f;
                rightBlurDiscRenderer.sharedMaterial.color = c;
            }
        }
    }

    // =========================================================================
    // 4. 3D SPATIAL TURBINE AUDIO
    // =========================================================================

    private void SetupAudio()
    {
        if (!enableAudio) return;

        if (turbofanAudioClip == null)
        {
#if UNITY_EDITOR
            turbofanAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Turbofan_Engine_Loop.wav");
#endif
        }

        if (leftTurbofanRotor != null && leftAudioSource == null)
        {
            leftAudioSource = leftTurbofanRotor.GetComponent<AudioSource>() ?? leftTurbofanRotor.gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(leftAudioSource);
        }

        if (rightTurbofanRotor != null && rightAudioSource == null)
        {
            rightAudioSource = rightTurbofanRotor.GetComponent<AudioSource>() ?? rightTurbofanRotor.gameObject.AddComponent<AudioSource>();
            ConfigureAudioSource(rightAudioSource);
        }
    }

    private void ConfigureAudioSource(AudioSource src)
    {
        src.clip = turbofanAudioClip;
        src.loop = true;
        src.spatialBlend = 1.0f; // Full 3D spatialization
        src.minDistance = 3f;
        src.maxDistance = 120f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.playOnAwake = false;

        if (Application.isPlaying && turbofanAudioClip != null && !src.isPlaying)
        {
            src.Play();
        }
    }

    private void UpdateAudio()
    {
        if (!enableAudio || !Application.isPlaying) return;

        float avgRPM = (leftRPM + rightRPM) * 0.5f;

        // Dynamic pitch based on RPM spool (0.55x at low idle to 1.85x at max TOGA)
        float targetPitch = Mathf.Lerp(0.55f, 1.85f, avgRPM / maxRPM);
        float targetVol = Mathf.Lerp(0.05f, masterAudioVolume, Mathf.InverseLerp(windmillingRPM, maxRPM, avgRPM));

        if (leftAudioSource != null)
        {
            leftAudioSource.pitch = targetPitch;
            leftAudioSource.volume = targetVol;
            if (!leftAudioSource.isPlaying && turbofanAudioClip != null) leftAudioSource.Play();
        }

        if (rightAudioSource != null)
        {
            rightAudioSource.pitch = targetPitch;
            rightAudioSource.volume = targetVol;
            if (!rightAudioSource.isPlaying && turbofanAudioClip != null) rightAudioSource.Play();
        }
    }

    // =========================================================================
    // 5. PUBLIC FLIGHT DECK INTERFACE & CONTROLS
    // =========================================================================

    public void SetThrottle(float t)
    {
        throttle = Mathf.Clamp01(t);
        if (throttle > 0.85f) engineState = EngineState.TakeoffTOGA;
        else if (throttle > 0.55f) engineState = EngineState.Cruise;
        else if (throttle > 0.30f) engineState = EngineState.Taxi;
        else if (throttle > 0.05f) engineState = EngineState.Idle;
        else engineState = EngineState.Cutoff;
    }

    public void StartEngines()
    {
        engineState = EngineState.Idle;
        throttle = 0.22f;
    }

    public void FullTakeoffThrust()
    {
        engineState = EngineState.TakeoffTOGA;
        throttle = 1.0f;
    }

    public void TaxiPower()
    {
        engineState = EngineState.Taxi;
        throttle = 0.40f;
    }

    public void CutEngines()
    {
        engineState = EngineState.ShuttingDown;
        throttle = 0f;
    }

    private void ApplyStateThrottle(EngineState state)
    {
        switch (state)
        {
            case EngineState.Cutoff: throttle = 0f; break;
            case EngineState.Idle: throttle = 0.22f; break;
            case EngineState.Taxi: throttle = 0.40f; break;
            case EngineState.Cruise: throttle = 0.75f; break;
            case EngineState.TakeoffTOGA: throttle = 1.0f; break;
            case EngineState.ShuttingDown: throttle = 0f; break;
        }
    }

    private void HandleKeyboardInput()
    {
        if (!enableKeyboardShortcuts || !Application.isPlaying) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (engineState == EngineState.Cutoff || engineState == EngineState.ShuttingDown) StartEngines();
            else CutEngines();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1)) CutEngines();
        else if (Input.GetKeyDown(KeyCode.Alpha2)) StartEngines();
        else if (Input.GetKeyDown(KeyCode.Alpha3)) TaxiPower();
        else if (Input.GetKeyDown(KeyCode.Alpha4)) FullTakeoffThrust();

        // Increment / Decrement throttle with + and - keys
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
        {
            SetThrottle(throttle + Time.deltaTime * 0.25f);
        }
        else if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            SetThrottle(throttle - Time.deltaTime * 0.25f);
        }
    }
}
