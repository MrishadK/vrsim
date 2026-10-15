using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// AeroSim VR — Professional airport VR experience controller.
/// Handles the complete passenger journey: entrance → terminal → gate → boarding → flight.
/// Developed by aitectra.
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class AirportVRExperience : MonoBehaviour
{
    private enum ExperienceState
    {
        Boot,
        Menu,
        Exploring,
        Boarding,
        Taxiing,
        Completed,
    }

    [Header("Experience Timing")]
    [SerializeField, Min(1f)] private float boardingCameraMoveDuration = 5f;
    [SerializeField, Min(0f)] private float departureDelay = 4f;
    [SerializeField, Min(0.5f)] private float taxiSpeed = 18f;
    [SerializeField, Min(1f)] private float takeoffSpeed = 88f;
    [SerializeField, Min(1f)] private float boardingRadius = 3f;
    
    [Header("Spawn Location — Airport Entrance")]
    [Tooltip("Position near the airport entrance gate. Adjust in Inspector if needed.")]
    [SerializeField] private Vector3 outsideStartPosition = new Vector3(-46f, 3.8f, -35.7f);
    [Tooltip("Rotation facing into the airport terminal.")]
    [SerializeField] private Vector3 outsideStartRotation = new Vector3(0f, -90f, 0f);

    [Header("Optimization Settings")]
    [Tooltip("Maximum number of NPCs in the airport. Optimized for single queues.")]
    [SerializeField, Min(0)] private int npcCount = 4;
    [Tooltip("Wander radius for NPCs.")]
    [SerializeField, Min(1f)] private float npcWanderRadius = 20f;

    [Header("UI Settings")]
    [Tooltip("Drag the 'Airport building 02-5690' GameObject here to mount the UI to it.")]
    [SerializeField] private Transform uiTargetPanel;

    private XROrigin xrOrigin;
    private Transform cameraOffset;
    private Camera headsetCamera;
    private AirportLocomotion locomotion;
    private Transform airplane;
    private SplineAnimate seatPathAnimator;
    private SplineContainer seatPath;
    private SplineAnimate airplaneSplineAnimator;

    private Vector3 initialRigPosition;
    private Quaternion initialRigRotation;
    private Vector3 initialPlanePosition;
    private Quaternion initialPlaneRotation;
    private Vector3 gatePosition;
    private GameObject gateMarker;
    private Transform externalViewAnchor;

    private Canvas canvas;
    private CanvasGroup menuGroup;
    private CanvasGroup settingsGroup;
    private CanvasGroup completionGroup;
    private CanvasGroup statusGroup;
    private CanvasGroup fadeGroup;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI comfortButtonText;
    private TextMeshProUGUI speedButtonText;
    private Coroutine statusRoutine;
    private ExperienceState state;
    private bool comfortMode = true;
    private bool fastLocomotion;

    // NPC management
    private readonly List<GameObject> spawnedNPCs = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // Only bootstrap in the game scene, not the MainMenu scene
        if (SceneManager.GetActiveScene().name == "MainMenu")
            return;

        if (FindObjectOfType<AirportVRExperience>() != null)
            return;

        new GameObject("AeroSim VR Experience").AddComponent<AirportVRExperience>();
    }

    private void Awake()
    {
        ResolveSceneReferences();
        CaptureInitialState();
        DisableLegacyAutoMotion();
        EnsureQuestUiInput();

        // Load saved preferences from Main Menu
        comfortMode = PlayerPrefs.GetInt("VR_Comfort", 1) == 1;
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        ApplyComfortSettings();
    }

    private IEnumerator Start()
    {
        // Wait one frame for XROrigin tracked-pose setup to finish
        yield return null;

        // CRITICAL: Teleport AFTER XROrigin has initialized, not in Awake
        TeleportToEntrance();
        ResetHeadsetOffset();
        AttachPassportToController();
        CreateInterface();
        CreateGateMarker();
        SpawnNPCCrowd();
        
        // Immediately start exploring. DO NOT show the in-game floating menu.
        StartExperience();
    }

    private void AttachPassportToController()
    {
        if (xrOrigin == null) return;
        
        var cameraOffset = xrOrigin.CameraFloorOffsetObject;
        if (cameraOffset != null)
        {
            var leftController = cameraOffset.transform.Find("Left Controller");
            if (leftController != null && leftController.GetComponent<VRHandPassport>() == null)
            {
                leftController.gameObject.AddComponent<VRHandPassport>();
            }
        }
    }

    /// <summary>
    /// Teleports the player to the airport entrance, well away from the airplane.
    /// Runs after XROrigin initialization to prevent being overridden.
    /// </summary>
    private void TeleportToEntrance()
    {
        if (xrOrigin != null)
        {
            // Spawn at the position set in the Editor instead of hardcoded coordinates
            Vector3 spawnPos = initialRigPosition;
            
            xrOrigin.transform.position = spawnPos;
            xrOrigin.transform.rotation = initialRigRotation;

            // Ensure CharacterController is at the right spot
            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                xrOrigin.transform.position = spawnPos;
                cc.enabled = true;
            }

            Debug.Log($"[AeroSim VR] Player teleported to editor entrance: {spawnPos}");
        }
    }

    private void Update()
    {
        if (state == ExperienceState.Exploring && headsetCamera != null && Vector3.Distance(headsetCamera.transform.position, gatePosition) <= boardingRadius)
            StartCoroutine(BoardingRoutine());

        if (gateMarker != null && gateMarker.activeSelf && headsetCamera != null)
        {
            var direction = headsetCamera.transform.position - gateMarker.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                gateMarker.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && state == ExperienceState.Menu && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            StartExperience();
#endif
    }

    private void ResolveSceneReferences()
    {
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            cameraOffset = xrOrigin.CameraFloorOffsetObject != null ? xrOrigin.CameraFloorOffsetObject.transform : null;
            headsetCamera = xrOrigin.Camera;
        }

        if (headsetCamera == null)
            headsetCamera = Camera.main;

        airplane = GameObject.Find("boeing 737-800")?.transform;
        if (cameraOffset != null)
            seatPathAnimator = cameraOffset.GetComponent<SplineAnimate>();
        if (seatPathAnimator != null)
            seatPath = seatPathAnimator.Container;
        if (seatPath == null)
            seatPath = GameObject.Find("to seat")?.GetComponent<SplineContainer>();
        if (airplane != null)
            airplaneSplineAnimator = airplane.GetComponent<SplineAnimate>();

        if (xrOrigin != null)
        {
            locomotion = xrOrigin.GetComponent<AirportLocomotion>() ?? xrOrigin.gameObject.AddComponent<AirportLocomotion>();

            var footstepAudio = xrOrigin.GetComponent<PlayerFootstepAudio>() ?? xrOrigin.gameObject.AddComponent<PlayerFootstepAudio>();
            footstepAudio.BindLocomotion(locomotion);

            var bodyAvatar = xrOrigin.GetComponent<PlayerBodyAvatar>() ?? xrOrigin.gameObject.AddComponent<PlayerBodyAvatar>();
            bodyAvatar.InitializeAvatar();

            var handsMgr = xrOrigin.GetComponent<VRPovHandsManager>() ?? xrOrigin.gameObject.AddComponent<VRPovHandsManager>();
            handsMgr.InitializePOVHands();
        }
    }

    private void CaptureInitialState()
    {
        if (xrOrigin != null)
        {
            initialRigPosition = xrOrigin.transform.position;
            initialRigRotation = xrOrigin.transform.rotation;
        }

        if (airplane != null)
        {
            initialPlanePosition = airplane.position;
            initialPlaneRotation = airplane.rotation;
        }
    }

    private void DisableLegacyAutoMotion()
    {
        if (xrOrigin != null)
        {
            var legacyAutoMove = xrOrigin.GetComponent<AutoMoveNav>();
            if (legacyAutoMove != null)
                legacyAutoMove.enabled = false;

            var navMeshAgent = xrOrigin.GetComponent<NavMeshAgent>();
            if (navMeshAgent != null)
                navMeshAgent.enabled = false;
        }

        if (seatPathAnimator != null)
        {
            seatPathAnimator.PlayOnAwake = false;
            seatPathAnimator.Pause();
            seatPathAnimator.enabled = false;
        }

        if (cameraOffset != null)
        {
            cameraOffset.localPosition = Vector3.zero;
        }

        if (airplaneSplineAnimator != null)
        {
            airplaneSplineAnimator.PlayOnAwake = false;
            airplaneSplineAnimator.Pause();
            airplaneSplineAnimator.enabled = false;
        }

        if (airplane != null)
        {
            var legacySpeedController = airplane.GetComponent<SplineSpeedBooster>();
            if (legacySpeedController != null)
                legacySpeedController.enabled = false;
        }

        if (locomotion != null)
            locomotion.SetLocked(true);
    }

    private void EnsureQuestUiInput()
    {
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
            eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();

        var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule != null)
            standaloneModule.enabled = false;

        var xrUiInput = eventSystem.GetComponent<XRUIInputModule>();
        if (xrUiInput == null)
            xrUiInput = eventSystem.gameObject.AddComponent<XRUIInputModule>();
        xrUiInput.enabled = true;
    }

    private void ResetHeadsetOffset()
    {
        if (xrOrigin == null || cameraOffset == null)
            return;

        xrOrigin.CameraYOffset = 1.6f;
        cameraOffset.localPosition = new Vector3(0f, xrOrigin.CameraYOffset, 0f);
        cameraOffset.localRotation = Quaternion.identity;
        cameraOffset.localScale = Vector3.one;

        if (headsetCamera != null)
        {
            headsetCamera.transform.localScale = Vector3.one;
            headsetCamera.transform.localRotation = Quaternion.identity;
        }
    }

    // ─────────────────────────────────────────────
    //  NPC CROWD SYSTEM
    // ─────────────────────────────────────────────

    private void SpawnNPCCrowd()
    {
        // If AirportTrafficManager is present, let it drive the complete realistic passenger flow
        if (AirportTrafficManager.Instance != null || FindObjectOfType<AirportTrafficManager>() != null)
        {
            Debug.Log("[AeroSim VR] AirportTrafficManager active — delegating passenger flow simulation.");
            return;
        }

        if (npcCount <= 0) return;

        List<Transform> entryPoints = (AirportRouteManager.Instance != null) ? AirportRouteManager.Instance.entryPoints : null;

        // Fallback spawn positions if no entry points are defined
        Vector3[] fallbackZones = new Vector3[]
        {
            new Vector3(-9f, 4.67f, -15f),   
            new Vector3(-9f, 4.67f, -25f)
        };

        for (int i = 0; i < npcCount; i++)
        {
            Vector3 basePos;
            Vector3 offset;

            if (entryPoints != null && entryPoints.Count > 0)
            {
                Transform entry = entryPoints[i % entryPoints.Count];
                basePos = entry.position;
                // Form a line behind the entry point (1.5 meters apart per NPC)
                // Add a tiny random side offset so they don't walk perfectly inside each other
                offset = -entry.forward * (i * 1.5f) + (entry.right * UnityEngine.Random.Range(-0.5f, 0.5f));
            }
            else
            {
                basePos = fallbackZones[i % fallbackZones.Length];
                offset = new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f));
            }

            Vector3 spawnPos = basePos + offset;

            // 1. Raycast downwards to find the actual floor geometry, ignoring roofs
            if (Physics.Raycast(spawnPos + (Vector3.up * 2f), Vector3.down, out RaycastHit hitInfo, 20f))
            {
                spawnPos = hitInfo.point;
            }

            // 2. Snap to the nearest NavMesh using a tighter 1.5f radius
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject npc = CreateNPCAtPosition(spawnPos, i);
            if (npc != null) spawnedNPCs.Add(npc);
        }

        Debug.Log($"[AeroSim VR] Spawned {spawnedNPCs.Count} NPCs in terminal");
    }

    private GameObject CreateNPCAtPosition(Vector3 position, int index)
    {
        string[] prefabs = new string[] 
        {
            "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx",
            "Assets/Kevin Iglesias/Human Animations/Models/HumanF_Model.fbx"
        };
        
        string selectedPrefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
#if UNITY_EDITOR
        GameObject prefabObj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(selectedPrefab);
#else
        // If in build, you'd load from Resources/Addressables. Assuming Editor for now.
        GameObject prefabObj = null; 
#endif
        GameObject npc;
        
        if (prefabObj != null)
        {
#if UNITY_EDITOR
            npc = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabObj);
            npc.transform.position = position;
#else
            npc = Instantiate(prefabObj, position, Quaternion.identity);
#endif
        }
        else
        {
            // Fallback capsule
            npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.transform.position = position;
            npc.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);
            var capsuleCollider = npc.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null) Destroy(capsuleCollider);
        }

        npc.name = $"NPC_Passenger_{index:D2}";
        
        NavMeshAgent agent = npc.AddComponent<NavMeshAgent>();
        agent.speed = UnityEngine.Random.Range(0.8f, 1.8f);
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.5f;
        agent.radius = 0.3f;
        agent.height = 1.7f; // Standard human height
        
        // Push visual up if it's the capsule fallback, otherwise keep at 0
        agent.baseOffset = (prefabObj == null) ? 0.9f : 0f; 
        agent.avoidancePriority = UnityEngine.Random.Range(30, 70);

        // Add physical collisions for VR player interaction
        CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
        col.height = 1.7f;
        col.radius = 0.3f;
        col.center = new Vector3(0f, 0.85f, 0f);

        Rigidbody rb = npc.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Let NavMesh control movement, but still register collisions

        // Add Animator Controller and Luggage if we loaded the model
        if (prefabObj != null)
        {
            Animator animator = npc.GetComponent<Animator>();
            if (animator == null) animator = npc.AddComponent<Animator>();
#if UNITY_EDITOR
            animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/NPC_Locomotion.controller");
#endif
            animator.applyRootMotion = false;
            
            // Randomize animation speed for variety
            animator.speed = UnityEngine.Random.Range(0.85f, 1.25f);
            
            // Spawn Luggage
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand != null)
            {
#if UNITY_EDITOR
                GameObject luggagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Airport_Luggage_Baggage.obj");
                if (luggagePrefab != null)
                {
                    GameObject luggage = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(luggagePrefab);
                    luggage.transform.SetParent(rightHand, false);
                    luggage.name = "NPC_Luggage";
                    
                    // The handle needs to sit in the hand
                    luggage.transform.localPosition = new Vector3(-0.02f, 0.05f, 0.02f);
                    luggage.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);
                    luggage.transform.localScale = Vector3.one * 0.7f; // slightly smaller
                    
                    // Give it a random color material
                    Renderer r = luggage.GetComponentInChildren<Renderer>();
                    if (r != null)
                    {
                        Material mat = new Material(r.sharedMaterial);
                        mat.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.3f, 0.8f);
                        r.material = mat;
                    }
                }
#endif
            }
        }

        AirportNPC npcBehavior = npc.AddComponent<AirportNPC>();
        npcBehavior.wanderRadius = npcWanderRadius;
        npcBehavior.minIdleTime = UnityEngine.Random.Range(1f, 4f);
        npcBehavior.maxIdleTime = UnityEngine.Random.Range(5f, 12f);
        
        // Add ProceduralSit for gate area
        npc.AddComponent<ProceduralSit>().enabled = false;

        return npc;
    }

    // ─────────────────────────────────────────────
    //  UI — PROFESSIONAL "AeroSim VR" INTERFACE
    // ─────────────────────────────────────────────

    private void CreateInterface()
    {
        if (headsetCamera == null)
            return;

        var canvasObject = new GameObject("AeroSim VR Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = headsetCamera;
        canvas.sortingOrder = 100;

        var root = canvas.GetComponent<RectTransform>();
        
        if (uiTargetPanel != null)
        {
            // Pin UI to the specific physical panel in the scene
            root.SetParent(uiTargetPanel, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1200f, 800f);
            
            // Push it slightly forward on the local Z-axis (-0.01f) to prevent Z-fighting with the mesh
            root.localPosition = new Vector3(0f, 0f, -0.01f); 
            root.localRotation = Quaternion.identity; // Align strictly with the panel's rotation
            
            // Adjust this scale to fit perfectly within the boundaries of your mesh
            root.localScale = Vector3.one * 0.0015f; 
        }
        else
        {
            // Fallback: Pin UI to the XR Origin (floating mode)
            root.SetParent(xrOrigin != null ? xrOrigin.transform : headsetCamera.transform, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1200f, 800f);
            root.localPosition = new Vector3(0f, 1.5f, 2.5f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * 0.00115f;
        }

        menuGroup = CreatePanel("Main Menu", root, new Color(0.012f, 0.03f, 0.07f, 0.96f));
        settingsGroup = CreatePanel("Settings", root, new Color(0.012f, 0.03f, 0.07f, 0.96f));
        completionGroup = CreatePanel("Completion", root, new Color(0.012f, 0.03f, 0.07f, 0.96f));
        statusGroup = CreatePanel("Status", root, new Color(0.01f, 0.02f, 0.05f, 0.90f));
        fadeGroup = CreatePanel("Fade", root, Color.black);
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        BuildMainMenu(menuGroup.transform);
        BuildSettings(settingsGroup.transform);
        BuildCompletion(completionGroup.transform);
        BuildStatus(statusGroup.transform);

        SetVisible(menuGroup, false);
        SetVisible(settingsGroup, false);
        SetVisible(completionGroup, false);
        SetVisible(statusGroup, false);
        fadeGroup.alpha = 0f;
    }

    private void BuildMainMenu(Transform parent)
    {
        // ── Professional Title Block ──
        CreateText(parent, "AEROSIM", 96f, new Vector2(0f, 260f), new Vector2(980f, 110f),
            new Color(0.92f, 0.96f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateText(parent, "V  R", 54f, new Vector2(0f, 195f), new Vector2(980f, 60f),
            new Color(0.35f, 0.72f, 0.95f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);

        // ── Decorative line ──
        var lineGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        lineGo.transform.SetParent(parent, false);
        var lineRect = lineGo.GetComponent<RectTransform>();
        lineRect.anchorMin = lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = new Vector2(0f, 155f);
        lineRect.sizeDelta = new Vector2(420f, 2f);
        lineGo.GetComponent<Image>().color = new Color(0.35f, 0.72f, 0.95f, 0.6f);

        // ── Subtitle ──
        CreateText(parent, "PASSENGER JOURNEY & DEPARTURE SIMULATION", 22f, new Vector2(0f, 120f), new Vector2(920f, 40f),
            new Color(0.55f, 0.72f, 0.88f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);

        // ── Description ──
        CreateText(parent, "Begin your journey at the airport entrance.\nExplore the terminal, then proceed to Gate A12 for boarding.\nExperience the full taxi and takeoff sequence.", 24f, new Vector2(0f, 45f), new Vector2(920f, 110f),
            new Color(0.75f, 0.82f, 0.90f, 0.85f), TextAlignmentOptions.Center, FontStyles.Normal);

        // ── Buttons ──
        CreateButton(parent, "▶  BEGIN EXPERIENCE", new Vector2(0f, -75f), new Color(0.05f, 0.38f, 0.68f, 1f), StartExperience, new Vector2(520f, 88f));
        CreateButton(parent, "⚙  SETTINGS", new Vector2(0f, -190f), new Color(0.08f, 0.14f, 0.26f, 1f), OpenSettings, new Vector2(520f, 78f));
        CreateButton(parent, "✕  EXIT", new Vector2(0f, -290f), new Color(0.25f, 0.06f, 0.09f, 1f), ExitSimulation, new Vector2(520f, 70f));

        // ── Controls hint ──
        CreateText(parent, "Left Stick = Move  ·  Right Stick = Turn  ·  Space = Start (Desktop)", 18f, new Vector2(0f, -380f), new Vector2(950f, 36f),
            new Color(0.45f, 0.58f, 0.72f, 0.7f), TextAlignmentOptions.Center, FontStyles.Italic);

        // ── Developer credit ──
        CreateText(parent, "Developed by  a i t e c t r a", 17f, new Vector2(0f, -420f), new Vector2(600f, 32f),
            new Color(0.4f, 0.55f, 0.72f, 0.55f), TextAlignmentOptions.Center, FontStyles.Normal);
    }

    private void BuildSettings(Transform parent)
    {
        CreateText(parent, "SETTINGS", 72f, new Vector2(0f, 235f), new Vector2(920f, 92f),
            new Color(0.85f, 0.95f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateText(parent, "Configure your comfort profile.", 26f, new Vector2(0f, 165f), new Vector2(900f, 46f),
            new Color(0.60f, 0.75f, 0.88f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);

        var comfortButton = CreateButton(parent, "", new Vector2(0f, 50f), new Color(0.08f, 0.14f, 0.26f, 1f), ToggleComfortMode, new Vector2(520f, 82f));
        comfortButtonText = comfortButton.GetComponentInChildren<TextMeshProUGUI>();
        var speedButton = CreateButton(parent, "", new Vector2(0f, -60f), new Color(0.08f, 0.14f, 0.26f, 1f), ToggleMoveSpeed, new Vector2(520f, 82f));
        speedButtonText = speedButton.GetComponentInChildren<TextMeshProUGUI>();
        CreateButton(parent, "← BACK", new Vector2(0f, -200f), new Color(0.05f, 0.38f, 0.68f, 1f), ShowMainMenu, new Vector2(520f, 78f));

        CreateText(parent, "Comfort mode reduces turn speed for a steadier VR experience.", 20f, new Vector2(0f, -320f), new Vector2(940f, 40f),
            new Color(0.50f, 0.65f, 0.78f, 0.7f), TextAlignmentOptions.Center, FontStyles.Italic);
        UpdateSettingsLabels();
    }

    private void BuildCompletion(Transform parent)
    {
        CreateText(parent, "FLIGHT COMPLETE", 76f, new Vector2(0f, 200f), new Vector2(1040f, 100f),
            new Color(0.90f, 0.97f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        CreateText(parent, "Your full airport journey is complete.\nThank you for flying with AeroSim VR.", 28f, new Vector2(0f, 100f), new Vector2(980f, 80f),
            new Color(0.52f, 0.78f, 0.95f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);
        CreateButton(parent, "↺  REPLAY", new Vector2(0f, -40f), new Color(0.05f, 0.38f, 0.68f, 1f), ReplayExperience, new Vector2(520f, 82f));
        CreateButton(parent, "⌂  MAIN MENU", new Vector2(0f, -160f), new Color(0.08f, 0.14f, 0.26f, 1f), ReplayExperience, new Vector2(520f, 78f));

        CreateText(parent, "Developed by aitectra", 18f, new Vector2(0f, -300f), new Vector2(600f, 32f),
            new Color(0.4f, 0.55f, 0.72f, 0.5f), TextAlignmentOptions.Center, FontStyles.Italic);
    }

    private void BuildStatus(Transform parent)
    {
        var panelRect = statusGroup.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -22f);
        panelRect.sizeDelta = new Vector2(1040f, 112f);
        statusText = CreateText(parent, string.Empty, 24f, Vector2.zero, new Vector2(980f, 82f),
            Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
        statusText.rectTransform.anchorMin = statusText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    }

    // ─────────────────────────────────────────────
    //  UI HELPERS
    // ─────────────────────────────────────────────

    private CanvasGroup CreatePanel(string name, RectTransform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel.GetComponent<CanvasGroup>();
    }

    private Button CreateButton(Transform parent, string label, Vector2 position, Color color, UnityEngine.Events.UnityAction onClick, Vector2 size)
    {
        var buttonObject = new GameObject(label.Length == 0 ? "Option Button" : label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var image = buttonObject.GetComponent<Image>();
        image.color = color;
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.3f, 1f);
        colors.pressedColor = new Color(0.75f, 0.85f, 1f, 1f);
        colors.selectedColor = new Color(1.1f, 1.1f, 1.2f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var text = CreateText(buttonObject.transform, label, 28f, Vector2.zero, new Vector2(size.x - 40f, size.y - 10f),
            Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        return button;
    }

    private TextMeshProUGUI CreateText(Transform parent, string value, float fontSize, Vector2 position, Vector2 size, Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    // ─────────────────────────────────────────────
    //  GATE MARKER
    // ─────────────────────────────────────────────

    private void CreateGateMarker()
    {
        gatePosition = ResolveSeatPathPoint(0f, out _);
        if (gatePosition == Vector3.zero && airplane != null)
            gatePosition = airplane.TransformPoint(new Vector3(7f, 1.5f, 0f));

        gateMarker = new GameObject("Gate A12 Boarding Marker");
        gateMarker.transform.position = gatePosition + Vector3.up * 2.2f;
        gateMarker.transform.localScale = Vector3.one * 0.23f;
        var markerText = gateMarker.AddComponent<TextMeshPro>();
        markerText.font = TMP_Settings.defaultFontAsset;
        markerText.text = "✈ GATE A12\nBOARDING";
        markerText.fontSize = 3.4f;
        markerText.alignment = TextAlignmentOptions.Center;
        markerText.color = new Color(0.32f, 0.82f, 1f, 1f);
        markerText.outlineWidth = 0.18f;
        markerText.raycastTarget = false;
        gateMarker.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  STATE TRANSITIONS
    // ─────────────────────────────────────────────

    private void StartExperience()
    {
        if (state != ExperienceState.Menu && state != ExperienceState.Boot)
            return;

        SetVisible(menuGroup, false);
        SetVisible(settingsGroup, false);

        if (locomotion != null)
            locomotion.SetLocked(false);
        if (gateMarker != null)
            gateMarker.SetActive(true);
        state = ExperienceState.Exploring;
        ShowStatus("Welcome to the airport. Explore the terminal and proceed to Gate A12 when ready.", 8f);
    }

    private void OpenSettings()
    {
        if (state != ExperienceState.Menu)
            return;

        SetVisible(menuGroup, false);
        SetVisible(settingsGroup, true);
    }

    private void ShowMainMenu()
    {
        state = ExperienceState.Menu;
        if (locomotion != null)
            locomotion.SetLocked(true);
        if (gateMarker != null)
            gateMarker.SetActive(false);
        SetVisible(settingsGroup, false);
        SetVisible(completionGroup, false);
        SetVisible(statusGroup, false);
        SetVisible(menuGroup, true);
    }

    private void ToggleComfortMode()
    {
        comfortMode = !comfortMode;
        ApplyComfortSettings();
    }

    private void ToggleMoveSpeed()
    {
        fastLocomotion = !fastLocomotion;
        ApplyComfortSettings();
    }

    private void ApplyComfortSettings()
    {
        if (locomotion != null)
        {
            locomotion.walkSpeed = fastLocomotion ? 1.5f : 1.0f;
            locomotion.sprintSpeed = fastLocomotion ? 3.5f : 2.5f;
            locomotion.turnSpeed = comfortMode ? 42f : 75f;
        }
        UpdateSettingsLabels();
    }

    private void UpdateSettingsLabels()
    {
        if (comfortButtonText != null)
            comfortButtonText.text = "Comfort Turning: " + (comfortMode ? "ON" : "OFF");
        if (speedButtonText != null)
            speedButtonText.text = "Movement Speed: " + (fastLocomotion ? "FAST" : "STANDARD");
    }

    // ─────────────────────────────────────────────
    //  BOARDING & FLIGHT
    // ─────────────────────────────────────────────

    private IEnumerator BoardingRoutine()
    {
        if (state != ExperienceState.Exploring)
            yield break;

        state = ExperienceState.Boarding;
        if (locomotion != null)
            locomotion.SetLocked(true);
        if (gateMarker != null)
            gateMarker.SetActive(false);
        ShowStatus("Boarding confirmed. Moving to your seat…", 0f);

        if (seatPathAnimator != null && seatPath != null)
        {
            seatPathAnimator.enabled = true;
            seatPathAnimator.PlayOnAwake = false;
            seatPathAnimator.Duration = boardingCameraMoveDuration;
            seatPathAnimator.NormalizedTime = 0f;
            seatPathAnimator.Play();

            var elapsed = 0f;
            while (seatPathAnimator.IsPlaying && elapsed < boardingCameraMoveDuration + 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            seatPathAnimator.Pause();
            seatPathAnimator.enabled = false;
        }
        else
        {
            yield return Fade(0f, 1f, 0.2f);
            yield return new WaitForSeconds(0.15f);
        }

        yield return Fade(0f, 1f, 0.18f);
        SeatPlayerInsideAircraft();
        yield return Fade(1f, 0f, 0.3f);
        ShowStatus("Seated. Cabin doors closing — departure in " + Mathf.CeilToInt(departureDelay) + "s.", 0f);
        yield return new WaitForSeconds(departureDelay);
        StartCoroutine(FlightRoutine());
    }

    private void SeatPlayerInsideAircraft()
    {
        if (xrOrigin == null || airplane == null)
            return;

        var seatPosition = ResolveSeatPathPoint(1f, out _);
        if (seatPosition == Vector3.zero)
            seatPosition = airplane.TransformPoint(new Vector3(-3.5f, 1.5f, 1.5f));

        ResetHeadsetOffset();
        var rigRotation = Quaternion.Euler(0f, airplane.eulerAngles.y, 0f);
        xrOrigin.transform.SetParent(null, true);
        xrOrigin.transform.rotation = rigRotation;
        xrOrigin.transform.position = seatPosition - rigRotation * new Vector3(0f, xrOrigin.CameraYOffset, 0f);
        xrOrigin.transform.SetParent(airplane, true);
    }

    private IEnumerator FlightRoutine()
    {
        state = ExperienceState.Taxiing;
        if (airplane == null)
        {
            CompleteExperience();
            yield break;
        }

        var groundY = initialPlanePosition.y;
        ShowStatus("Pushback complete. Taxiing to runway…", 0f);
        yield return MoveAircraftAlong(new[]
        {
            new Vector3(-73.2f, groundY, -20.6f),
            new Vector3(-52f, groundY, -44f),
            new Vector3(-31f, groundY, -85f),
            new Vector3(-31f, groundY, -180f),
        }, taxiSpeed);

        ShowStatus("Runway cleared. Takeoff commencing…", 0f);
        yield return new WaitForSeconds(0.8f);
        SwitchToExternalFlightView();
        ShowStatus("External flight view.", 2f);
        yield return MoveAircraftAlong(new[]
        {
            new Vector3(-31f, groundY + 7f, -340f),
            new Vector3(-31f, groundY + 42f, -640f),
            new Vector3(-31f, groundY + 135f, -1080f),
            new Vector3(-31f, groundY + 260f, -1540f),
        }, takeoffSpeed);

        CompleteExperience();
    }

    private IEnumerator MoveAircraftAlong(Vector3[] points, float speed)
    {
        if (airplane == null)
            yield break;

        foreach (var point in points)
        {
            while (Vector3.Distance(airplane.position, point) > 0.15f)
            {
                var direction = point - airplane.position;
                var flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
                if (flatDirection.sqrMagnitude > 0.01f)
                {
                    var desiredRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                    airplane.rotation = Quaternion.Slerp(airplane.rotation, desiredRotation, Time.deltaTime * 1.8f);
                }

                airplane.position = Vector3.MoveTowards(airplane.position, point, speed * Time.deltaTime);
                yield return null;
            }
        }
    }

    private void SwitchToExternalFlightView()
    {
        if (xrOrigin == null || airplane == null)
            return;

        if (externalViewAnchor == null)
        {
            externalViewAnchor = new GameObject("External Flight View Anchor").transform;
            externalViewAnchor.SetParent(airplane, false);
            externalViewAnchor.localPosition = new Vector3(0f, 4.5f, -18f);
            externalViewAnchor.localRotation = Quaternion.identity;
        }

        ResetHeadsetOffset();
        xrOrigin.transform.SetParent(externalViewAnchor, false);
        xrOrigin.transform.localPosition = Vector3.zero;
        xrOrigin.transform.localRotation = Quaternion.identity;
    }

    private void CompleteExperience()
    {
        state = ExperienceState.Completed;
        if (locomotion != null)
            locomotion.SetLocked(true);
        ShowStatus("Flight sequence complete.", 1.5f);
        SetVisible(completionGroup, true);
    }

    // ─────────────────────────────────────────────
    //  UTILITIES
    // ─────────────────────────────────────────────

    private Vector3 ResolveSeatPathPoint(float t, out Quaternion rotation)
    {
        rotation = airplane != null ? airplane.rotation : Quaternion.identity;
        if (seatPath == null)
            return Vector3.zero;

        if (!seatPath.Evaluate(Mathf.Clamp01(t), out var localPosition, out var localTangent, out var localUp))
            return Vector3.zero;

        var position = seatPath.transform.TransformPoint(new Vector3(localPosition.x, localPosition.y, localPosition.z));
        var tangent = seatPath.transform.TransformDirection(new Vector3(localTangent.x, localTangent.y, localTangent.z));
        var up = seatPath.transform.TransformDirection(new Vector3(localUp.x, localUp.y, localUp.z));
        if (tangent.sqrMagnitude > 0.001f)
            rotation = Quaternion.LookRotation(tangent.normalized, up.sqrMagnitude > 0.001f ? up.normalized : Vector3.up);
        return position;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeGroup == null)
            yield break;

        fadeGroup.alpha = from;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        fadeGroup.alpha = to;
    }

    private void ShowStatus(string message, float seconds)
    {
        if (statusGroup == null || statusText == null)
            return;

        if (statusRoutine != null)
            StopCoroutine(statusRoutine);
        statusText.text = message;
        SetVisible(statusGroup, true);
        if (seconds > 0f)
            statusRoutine = StartCoroutine(HideStatusAfter(seconds));
    }

    private IEnumerator HideStatusAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetVisible(statusGroup, false);
        statusRoutine = null;
    }

    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void ReplayExperience()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ExitSimulation()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}