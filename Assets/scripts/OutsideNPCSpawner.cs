using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns NPCs at a designated spawn point (Cube 32) and sends them walking
/// towards the airport interior. They never return.
/// 
/// ALL NPCs stop and resume at the SAME TIME on a synchronized timer.
/// When stopped, each NPC performs a random idle activity:
///   - Checking phone (head tilted down, hand raised to face)
///   - Making a phone call (hand up to ear, subtle body sway)
///   - Looking around (head turns left/right)
/// 
/// Attach this to the Cube (32) GameObject or any empty object.
/// Set spawnPoint to Cube (32) and destinationPoint to a target deeper inside the terminal.
/// </summary>
public class OutsideNPCSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Where NPCs spawn. Assign Cube (32).")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Direction NPCs walk towards (inside the terminal). If null, uses PASSPORT CONTROL position.")]
    [SerializeField] private Transform destinationPoint;

    [Tooltip("Number of NPCs to maintain in the area.")]
    [SerializeField, Min(1)] private int maxNPCs = 8;

    [Tooltip("Seconds between each NPC spawn.")]
    [SerializeField] private float spawnInterval = 4f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float npcSpacing = 1.5f;

    [Header("Synchronized Stops")]
    [Tooltip("How often (seconds) ALL NPCs stop simultaneously.")]
    [SerializeField] private float stopInterval = 12f;

    [Tooltip("How long they stay stopped.")]
    [SerializeField] private float stopDuration = 5f;

    [Tooltip("Slight randomness added to the stop interval.")]
    [SerializeField] private float stopIntervalVariance = 3f;

    [Header("Idle Activities")]
    [Tooltip("Probability of 'checking phone' when stopped (0-1).")]
    [SerializeField] private float phoneCheckChance = 0.5f;

    [Tooltip("Probability of 'phone call' when stopped (0-1).")]
    [SerializeField] private float phoneCallChance = 0.3f;

    // Runtime
    private readonly List<NPCWalker> activeNPCs = new List<NPCWalker>();
    private bool allStopped;
    private float stopTimer;
    private float nextStopTime;
    private Vector3 insideDirection;
    private Vector3 despawnPoint;

    private void Start()
    {
        // Auto-find spawn point if not set
        if (spawnPoint == null)
        {
            var cube32 = GameObject.Find("Cube (32)");
            if (cube32 != null) spawnPoint = cube32.transform;
            else spawnPoint = transform;
        }

        // Determine "inside" direction — towards PASSPORT CONTROL or a set destination
        if (destinationPoint != null)
        {
            insideDirection = (destinationPoint.position - spawnPoint.position).normalized;
        }
        else
        {
            var passport = GameObject.Find("PASSPORT CONTROL");
            if (passport != null)
                insideDirection = (passport.transform.position - spawnPoint.position).normalized;
            else
                insideDirection = -Vector3.right; // Fallback: walk towards -X
        }

        // Despawn point is far inside so NPCs walk off and get destroyed
        despawnPoint = spawnPoint.position + insideDirection * 80f;

        // Initialize stop timer
        nextStopTime = Time.time + stopInterval + Random.Range(-stopIntervalVariance, stopIntervalVariance);

        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        UpdateSynchronizedStops();
        CleanupDespawnedNPCs();
    }

    // ─────────────────────────────────────────────
    //  SYNCHRONIZED STOP SYSTEM
    // ─────────────────────────────────────────────

    private void UpdateSynchronizedStops()
    {
        if (!allStopped)
        {
            // Check if it's time for all NPCs to stop
            if (Time.time >= nextStopTime)
            {
                allStopped = true;
                stopTimer = stopDuration;

                // STOP all NPCs at the same time
                foreach (var npc in activeNPCs)
                {
                    if (npc != null && npc.gameObject != null)
                        npc.SynchronizedStop();
                }
            }
        }
        else
        {
            stopTimer -= Time.deltaTime;

            if (stopTimer <= 0f)
            {
                allStopped = false;
                nextStopTime = Time.time + stopInterval + Random.Range(-stopIntervalVariance, stopIntervalVariance);

                // RESUME all NPCs at the same time
                foreach (var npc in activeNPCs)
                {
                    if (npc != null && npc.gameObject != null)
                        npc.SynchronizedResume();
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    //  SPAWNING
    // ─────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(1f); // Let scene settle

        while (true)
        {
            // Clean nulls
            activeNPCs.RemoveAll(n => n == null || n.gameObject == null);

            if (activeNPCs.Count < maxNPCs)
            {
                SpawnNPC();
            }

            yield return new WaitForSeconds(spawnInterval + Random.Range(-1f, 1f));
        }
    }

    private void SpawnNPC()
    {
        Vector3 basePos = spawnPoint.position;

        // Spread NPCs along the spawn cube's Z-axis (it's 15.6 units long)
        float zSpread = spawnPoint.localScale.z * 0.4f;
        Vector3 offset = new Vector3(
            Random.Range(-0.5f, 0.5f),
            0f,
            Random.Range(-zSpread, zSpread)
        );

        Vector3 spawnPos = basePos + offset;

        // Snap to NavMesh
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            spawnPos = hit.position;

        // Determine NPC destination — a point far inside, slightly randomized
        Vector3 npcDest = despawnPoint + new Vector3(
            Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));

        // Load model
        GameObject npc = CreateNPCModel(spawnPos);
        if (npc == null) return;

        int idx = activeNPCs.Count;
        npc.name = $"OutsideNPC_{idx:D2}";

        // NavMeshAgent
        NavMeshAgent agent = npc.AddComponent<NavMeshAgent>();
        agent.speed = walkSpeed + Random.Range(-0.2f, 0.3f);
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.5f;
        agent.radius = 0.3f;
        agent.height = 1.7f;
        agent.baseOffset = 0f;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.updateRotation = false; // Manual rotation for smooth blending

        // Collider + Rigidbody
        CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
        col.height = 1.7f;
        col.radius = 0.3f;
        col.center = new Vector3(0f, 0.85f, 0f);

        Rigidbody rb = npc.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // Walker behavior
        NPCWalker walker = npc.AddComponent<NPCWalker>();
        walker.Initialize(npcDest, phoneCheckChance, phoneCallChance);

        // If currently in a stop phase, stop immediately
        if (allStopped)
            walker.SynchronizedStop();

        activeNPCs.Add(walker);

        // Auto-destroy after reaching destination
        Destroy(npc, 120f);
    }

    private GameObject CreateNPCModel(Vector3 position)
    {
        string[] modelPaths = new string[]
        {
            "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx",
            "Assets/Kevin Iglesias/Human Animations/Models/HumanF_Model.fbx"
        };

        string selected = modelPaths[Random.Range(0, modelPaths.Length)];
        GameObject prefab = null;
        GameObject npc = null;

#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(selected);
#endif

        if (prefab != null)
        {
            if (Application.isPlaying)
            {
                npc = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
#if UNITY_EDITOR
                npc = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                if (npc != null)
                {
                    UnityEditor.PrefabUtility.UnpackPrefabInstance(npc, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
                    npc.transform.position = position;
                }
#else
                npc = Instantiate(prefab, position, Quaternion.identity);
#endif
            }
            // Set up animator
            Animator animator = npc.GetComponent<Animator>();
            if (animator == null) animator = npc.AddComponent<Animator>();
#if UNITY_EDITOR
            animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/NPC_Locomotion.controller");
#endif
            animator.applyRootMotion = false;
            animator.speed = Random.Range(0.9f, 1.15f);

            // Random clothing color
            RandomizeClothing(npc);
        }
        else
        {
            // Fallback capsule
            npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.transform.position = position;
            npc.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);
            var capsuleCol = npc.GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Object.Destroy(capsuleCol);
        }

        // Face towards the interior
        npc.transform.rotation = Quaternion.LookRotation(insideDirection, Vector3.up);

        return npc;
    }

    private void RandomizeClothing(GameObject npc)
    {
        Renderer[] renderers = npc.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            string n = r.name.ToLower();
            if (n.Contains("jacket") || n.Contains("shirt") || n.Contains("body"))
            {
                Material mat = new Material(r.sharedMaterial);
                mat.color = Random.ColorHSV(0f, 1f, 0.2f, 0.6f, 0.3f, 0.7f);
                r.material = mat;
            }
            else if (n.Contains("jeans") || n.Contains("pants"))
            {
                Material mat = new Material(r.sharedMaterial);
                mat.color = Random.ColorHSV(0.55f, 0.7f, 0.3f, 0.7f, 0.15f, 0.35f);
                r.material = mat;
            }
        }
    }

    private void CleanupDespawnedNPCs()
    {
        for (int i = activeNPCs.Count - 1; i >= 0; i--)
        {
            var npc = activeNPCs[i];
            if (npc == null || npc.gameObject == null)
            {
                activeNPCs.RemoveAt(i);
                continue;
            }

            // If they've walked far enough inside, destroy them
            float dist = Vector3.Distance(npc.transform.position, spawnPoint.position);
            if (dist > 70f)
            {
                Destroy(npc.gameObject);
                activeNPCs.RemoveAt(i);
            }
        }
    }
}

/// <summary>
/// Individual NPC walker behavior.
/// Walks towards a target, responds to synchronized stop/resume commands.
/// When stopped, performs idle activities (phone check, phone call, or look around).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NPCWalker : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 destination;
    private bool isStopped;
    private float phoneCheckProb;
    private float phoneCallProb;

    // Animation
    private float currentSpeed;
    private float currentTurn;
    private float rotationSpeed = 5f;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int TurnParam = Animator.StringToHash("Turn");
    private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");
    private static readonly int InteractParam = Animator.StringToHash("Interact");

    // Idle activity
    private IdleActivity currentActivity = IdleActivity.None;
    private GameObject phoneObject;
    private Transform rightHand;
    private Transform head;
    private Quaternion originalHeadRotation;

    private enum IdleActivity
    {
        None,
        CheckingPhone,
        PhoneCall,
        LookAround
    }

    public void Initialize(Vector3 dest, float phoneCheckChance, float phoneCallChance)
    {
        destination = dest;
        phoneCheckProb = phoneCheckChance;
        phoneCallProb = phoneCallChance;
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Cache bone references
        if (animator != null)
        {
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) originalHeadRotation = head.localRotation;
        }

        // Start walking to destination
        if (agent != null && agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            else
                agent.SetDestination(destination);
        }
    }

    private void Update()
    {
        if (agent == null) return;

        UpdateAnimationAndRotation();
        UpdateIdleActivity();
    }

    private void LateUpdate()
    {
        // Apply procedural head/hand IK for idle activities AFTER animation
        ApplyProceduralIdle();
    }

    // ─────────────────────────────────────────────
    //  SYNCHRONIZED STOP / RESUME
    // ─────────────────────────────────────────────

    public void SynchronizedStop()
    {
        if (isStopped) return;
        isStopped = true;

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;

        // Pick an idle activity
        float roll = Random.value;
        if (roll < phoneCheckProb)
        {
            currentActivity = IdleActivity.CheckingPhone;
            CreatePhoneInHand();
        }
        else if (roll < phoneCheckProb + phoneCallProb)
        {
            currentActivity = IdleActivity.PhoneCall;
            CreatePhoneInHand();
        }
        else
        {
            currentActivity = IdleActivity.LookAround;
        }

        // Trigger interact animation if available
        if (animator != null)
            animator.SetTrigger(InteractParam);
    }

    public void SynchronizedResume()
    {
        if (!isStopped) return;
        isStopped = false;

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;

        // Clean up phone
        if (phoneObject != null)
        {
            Destroy(phoneObject);
            phoneObject = null;
        }

        currentActivity = IdleActivity.None;
    }

    // ─────────────────────────────────────────────
    //  ANIMATION BLENDING
    // ─────────────────────────────────────────────

    private void UpdateAnimationAndRotation()
    {
        if (animator == null) return;

        float targetSpeed = isStopped ? 0f : agent.velocity.magnitude;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);
        animator.SetFloat(SpeedParam, currentSpeed);
        animator.SetBool(IsWalkingParam, currentSpeed > 0.1f);

        // Smooth rotation towards movement direction
        if (!isStopped)
        {
            Vector3 desiredVelocity = agent.desiredVelocity;
            if (desiredVelocity.magnitude > 0.1f)
            {
                desiredVelocity.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity.normalized);

                float angle = Vector3.SignedAngle(transform.forward, desiredVelocity.normalized, Vector3.up);
                float targetTurn = Mathf.Clamp(angle / 45f, -1f, 1f);
                currentTurn = Mathf.Lerp(currentTurn, targetTurn, Time.deltaTime * 10f);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                currentTurn = Mathf.Lerp(currentTurn, 0f, Time.deltaTime * 10f);
            }

            animator.SetFloat(TurnParam, currentTurn);
        }
    }

    // ─────────────────────────────────────────────
    //  IDLE ACTIVITIES
    // ─────────────────────────────────────────────

    private void UpdateIdleActivity()
    {
        // No runtime update needed — procedural IK is applied in LateUpdate
    }

    private void ApplyProceduralIdle()
    {
        if (!isStopped || animator == null) return;

        switch (currentActivity)
        {
            case IdleActivity.CheckingPhone:
                ApplyPhoneCheckPose();
                break;
            case IdleActivity.PhoneCall:
                ApplyPhoneCallPose();
                break;
            case IdleActivity.LookAround:
                ApplyLookAroundPose();
                break;
        }
    }

    private void ApplyPhoneCheckPose()
    {
        // Tilt head downward to look at phone in hand
        if (head != null)
        {
            Quaternion tiltDown = originalHeadRotation * Quaternion.Euler(25f, 0f, 0f);
            head.localRotation = Quaternion.Slerp(head.localRotation, tiltDown, Time.deltaTime * 3f);
        }

        // Raise right hand towards chest level (holding phone)
        if (rightHand != null)
        {
            Vector3 phonePos = transform.position + transform.forward * 0.25f + Vector3.up * 1.2f;
            rightHand.position = Vector3.Lerp(rightHand.position, phonePos, Time.deltaTime * 4f);
        }

        // Subtle weight shift
        float sway = Mathf.Sin(Time.time * 0.8f) * 0.3f;
        transform.localRotation *= Quaternion.Euler(0f, 0f, sway * 0.1f);
    }

    private void ApplyPhoneCallPose()
    {
        // Raise right hand to ear
        if (rightHand != null)
        {
            Vector3 earPos = transform.position + Vector3.up * 1.65f +
                             transform.right * 0.15f + transform.forward * 0.05f;
            rightHand.position = Vector3.Lerp(rightHand.position, earPos, Time.deltaTime * 4f);

            // Tilt hand to hold phone against ear
            Quaternion earRot = Quaternion.LookRotation(transform.up, -transform.right);
            rightHand.rotation = Quaternion.Slerp(rightHand.rotation, earRot, Time.deltaTime * 3f);
        }

        // Subtle body sway while talking
        float sway = Mathf.Sin(Time.time * 1.2f) * 0.5f;
        float nod = Mathf.Sin(Time.time * 2.5f) * 0.15f;

        if (head != null)
        {
            Quaternion talkNod = originalHeadRotation * Quaternion.Euler(nod * 5f, sway * 2f, 0f);
            head.localRotation = Quaternion.Slerp(head.localRotation, talkNod, Time.deltaTime * 3f);
        }
    }

    private void ApplyLookAroundPose()
    {
        if (head == null) return;

        // Look left, then right, then center — smooth cycle
        float cycle = Mathf.Sin(Time.time * 0.5f); // Slow look cycle
        float yaw = cycle * 40f; // ±40 degrees
        float pitch = Mathf.Sin(Time.time * 0.3f) * 5f; // Slight up/down

        Quaternion lookRot = originalHeadRotation * Quaternion.Euler(pitch, yaw, 0f);
        head.localRotation = Quaternion.Slerp(head.localRotation, lookRot, Time.deltaTime * 2f);
    }

    // ─────────────────────────────────────────────
    //  PHONE PROP
    // ─────────────────────────────────────────────

    private void CreatePhoneInHand()
    {
        if (phoneObject != null) return;

        phoneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        phoneObject.name = "NPC_Phone";
        phoneObject.transform.localScale = new Vector3(0.04f, 0.08f, 0.005f);

        // Disable collider
        var col = phoneObject.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Dark phone material with screen glow
        var renderer = phoneObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.08f, 0.08f, 0.1f, 1f);
            renderer.material = mat;
        }

        // Screen
        var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.name = "PhoneScreen";
        screen.transform.SetParent(phoneObject.transform, false);
        screen.transform.localPosition = new Vector3(0f, 0f, -0.003f);
        screen.transform.localScale = new Vector3(0.85f, 0.8f, 0.3f);

        var screenCol = screen.GetComponent<Collider>();
        if (screenCol != null) screenCol.enabled = false;

        var screenRenderer = screen.GetComponent<Renderer>();
        if (screenRenderer != null)
        {
            var screenMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            screenMat.color = new Color(0.15f, 0.2f, 0.3f, 1f);
            screenMat.EnableKeyword("_EMISSION");
            screenMat.SetColor("_EmissionColor", new Color(0.1f, 0.15f, 0.25f, 1f));
            screenRenderer.material = screenMat;
        }

        // Parent to right hand if available
        if (rightHand != null)
        {
            phoneObject.transform.SetParent(rightHand, false);
            phoneObject.transform.localPosition = new Vector3(0.02f, 0.05f, 0.02f);
            phoneObject.transform.localRotation = Quaternion.Euler(-80f, 0f, 0f);
        }
        else
        {
            // Fallback: float in front of NPC
            phoneObject.transform.position = transform.position + transform.forward * 0.3f + Vector3.up * 1.3f;
        }
    }

    private void OnDestroy()
    {
        if (phoneObject != null)
            Destroy(phoneObject);
    }
}
