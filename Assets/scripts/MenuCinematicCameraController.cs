using System;
using UnityEngine;

/// <summary>
/// AeroSim VR — Cinematic Menu Camera Controller.
/// Provides smooth idle cinematic drifting, interactive mouse/gaze parallax,
/// and multiple preset camera angles showcasing the live 3D aircraft and airport apron.
/// </summary>
public class MenuCinematicCameraController : MonoBehaviour
{
    [System.Serializable]
    public struct CameraShot
    {
        public string shotName;
        public Vector3 position;
        public Vector3 eulerRotation;
        public float fieldOfView;
    }

    [Header("Camera Shots")]
    public CameraShot[] shots;
    public int currentShotIndex = 0;

    [Header("Transition")]
    public float transitionSpeed = 2.5f;

    [Header("Cinematic Idle Drift")]
    public bool enableIdleDrift = true;
    public float driftAmplitude = 0.35f;
    public float driftSpeed = 0.3f;

    [Header("Mouse Parallax")]
    public bool enableParallax = true;
    public float parallaxStrength = 0.6f;
    public float maxParallaxAngle = 2.0f;

    private Camera cam;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetFOV;
    private Vector3 currentVelocity;
    private Vector2 smoothMouseOffset;

    public event Action<int, string> OnCameraShotChanged;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        InitializeDefaultShots();
        ApplyShot(currentShotIndex, instant: true);
    }

    private void InitializeDefaultShots()
    {
        if (shots != null && shots.Length > 0) return;

        shots = new CameraShot[]
        {
            // Shot 0: Gate 2 Panoramic Lounge Vista (Overlooking Gate 2, A350 connected to jetway, shuttle bus)
            new CameraShot
            {
                shotName = "GATE 2 PANORAMIC VISTA",
                position = new Vector3(14.0f, 3.6f, 36.0f),
                eulerRotation = new Vector3(2.0f, -28.0f, 0f),
                fieldOfView = 52f
            },
            // Shot 1: Hero Apron Turbine & Nose (Low angle tarmac view of Trent XWB turbofans & nose)
            new CameraShot
            {
                shotName = "HERO APRON TURBINE & NOSE",
                position = new Vector3(-6.0f, 3.2f, 48.0f),
                eulerRotation = new Vector3(4.5f, 42.0f, 0f),
                fieldOfView = 48f
            },
            // Shot 2: Terminal & Apron Overview (Mezzanine perspective across international gate concourse)
            new CameraShot
            {
                shotName = "TERMINAL & APRON OVERVIEW",
                position = new Vector3(32.0f, 8.5f, 18.0f),
                eulerRotation = new Vector3(12.0f, -45.0f, 0f),
                fieldOfView = 55f
            }
        };
    }

    private void Update()
    {
        // ── 1. Update Target Transform from Current Shot ──
        if (shots != null && shots.Length > 0)
        {
            var shot = shots[Mathf.Clamp(currentShotIndex, 0, shots.Length - 1)];
            targetPosition = shot.position;
            targetRotation = Quaternion.Euler(shot.eulerRotation);
            targetFOV = shot.fieldOfView;
        }

        // ── 2. Add Cinematic Idle Drift ──
        Vector3 driftOffset = Vector3.zero;
        if (enableIdleDrift)
        {
            float t = Time.time * driftSpeed;
            float driftX = Mathf.Sin(t * 1.1f) * driftAmplitude;
            float driftY = Mathf.Sin(t * 0.7f + 1.2f) * (driftAmplitude * 0.5f);
            float driftZ = Mathf.Cos(t * 0.9f) * (driftAmplitude * 0.4f);
            driftOffset = new Vector3(driftX, driftY, driftZ);
        }

        // ── 3. Add Smooth Mouse Parallax ──
        Vector3 parallaxEuler = Vector3.zero;
        if (enableParallax)
        {
            Vector2 mouseNorm = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                mouseNorm = new Vector2(
                    (mousePos.x / Screen.width) * 2f - 1f,
                    (mousePos.y / Screen.height) * 2f - 1f
                );
            }
#else
            mouseNorm = new Vector2(
                (Input.mousePosition.x / Screen.width) * 2f - 1f,
                (Input.mousePosition.y / Screen.height) * 2f - 1f
            );
#endif
            smoothMouseOffset = Vector2.Lerp(smoothMouseOffset, mouseNorm, Time.deltaTime * 3.5f);
            parallaxEuler = new Vector3(
                -smoothMouseOffset.y * maxParallaxAngle,
                smoothMouseOffset.x * maxParallaxAngle * 1.6f,
                -smoothMouseOffset.x * 0.4f
            );
        }

        // ── 4. Smooth Camera Motion ──
        Vector3 finalPos = targetPosition + driftOffset;
        Quaternion finalRot = targetRotation * Quaternion.Euler(parallaxEuler);

        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref currentVelocity, 1f / transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, Time.deltaTime * transitionSpeed * 1.4f);

        if (cam != null && targetFOV > 0f)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * transitionSpeed);
        }

        // Keyboard Shortcut: C key to cycle camera views
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
        {
            CycleCameraView();
        }
#else
        if (Input.GetKeyDown(KeyCode.C))
        {
            CycleCameraView();
        }
#endif
    }

    public void CycleCameraView()
    {
        if (shots == null || shots.Length == 0) return;
        currentShotIndex = (currentShotIndex + 1) % shots.Length;
        ApplyShot(currentShotIndex, instant: false);
    }

    public void SetCameraView(int index)
    {
        if (shots == null || shots.Length == 0) return;
        currentShotIndex = Mathf.Clamp(index, 0, shots.Length - 1);
        ApplyShot(currentShotIndex, instant: false);
    }

    private void ApplyShot(int index, bool instant)
    {
        if (shots == null || shots.Length == 0) return;
        var shot = shots[Mathf.Clamp(index, 0, shots.Length - 1)];

        targetPosition = shot.position;
        targetRotation = Quaternion.Euler(shot.eulerRotation);
        targetFOV = shot.fieldOfView;

        if (instant)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            if (cam != null) cam.fieldOfView = targetFOV;
        }

        OnCameraShotChanged?.Invoke(index, shot.shotName);
        Debug.Log($"[AeroSim VR] Camera switched to: {shot.shotName}");
    }

    public string GetCurrentShotName()
    {
        if (shots == null || shots.Length == 0) return "DEFAULT";
        return shots[Mathf.Clamp(currentShotIndex, 0, shots.Length - 1)].shotName;
    }
}
