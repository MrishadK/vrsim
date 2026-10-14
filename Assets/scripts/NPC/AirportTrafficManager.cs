using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Continuous, organic passenger traffic manager for the airport.
/// Maintains an active population of passengers distributed across the airport stations.
/// When passengers board the flight and leave, new arriving passengers pull up curbside.
/// </summary>
public class AirportTrafficManager : MonoBehaviour
{
    public static AirportTrafficManager Instance { get; private set; }

    [Header("Crowd Settings")]
    [Tooltip("Maximum passengers simultaneously active in the airport.")]
    [SerializeField, Range(3, 14)] private int maxActivePassengers = 7;

    [Tooltip("Time between curbside arrivals (seconds).")]
    [SerializeField] private float spawnInterval = 14f;

    [Tooltip("Distribute initial passengers across airport stations on startup so the airport feels alive immediately.")]
    [SerializeField] private bool prePopulateAirport = true;

    [Header("Model & Animation Assets")]
    [SerializeField] private string[] passengerModelPaths = new string[]
    {
        "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx",
        "Assets/Kevin Iglesias/Human Animations/Models/HumanF_Model.fbx"
    };

    [SerializeField] private string animatorControllerPath = "Assets/Animations/NPC_Locomotion.controller";
    [SerializeField] private string luggageModelPath = "Assets/Models/Airport_Luggage_Baggage.obj";

    private readonly List<AirportNPC> activePassengers = new List<AirportNPC>();
    private float nextSpawnTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (prePopulateAirport)
        {
            PrePopulateCrowd();
        }

        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        // Clean nulls
        activePassengers.RemoveAll(p => p == null || p.gameObject == null);

        // Periodically spawn new arriving passengers if under limit
        if (activePassengers.Count < maxActivePassengers && Time.time >= nextSpawnTime)
        {
            SpawnArrivingPassenger();
            nextSpawnTime = Time.time + spawnInterval + Random.Range(-3f, 4f);
        }
    }

    /// <summary>
    /// Distributes initial passengers across different airport stages so the terminal is already humming with activity.
    /// </summary>
    private void PrePopulateCrowd()
    {
        var rm = AirportRouteManager.Instance;
        if (rm == null) return;

        AirportNPC.AirportStage[] initialStages = new AirportNPC.AirportStage[]
        {
            AirportNPC.AirportStage.EnteringTerminal,
            AirportNPC.AirportStage.QueueingCheckIn,
            AirportNPC.AirportStage.QueueingSecurity,
            AirportNPC.AirportStage.QueueingImmigration,
            AirportNPC.AirportStage.WaitingAtLounge,
            AirportNPC.AirportStage.WaitingAtLounge,
            AirportNPC.AirportStage.QueueingGate
        };

        for (int i = 0; i < Mathf.Min(maxActivePassengers, initialStages.Length); i++)
        {
            AirportNPC.AirportStage stage = initialStages[i];
            Vector3 spawnPos = ResolveStageSpawnPosition(stage);

            if (spawnPos != Vector3.zero)
            {
                bool attachLuggage = (stage <= AirportNPC.AirportStage.QueueingCheckIn);
                AirportNPC npc = CreatePassengerObject(spawnPos, stage, attachLuggage);
                if (npc != null)
                {
                    activePassengers.Add(npc);
                }
            }
        }

        Debug.Log($"[AirportTrafficManager] Pre-populated airport with {activePassengers.Count} passengers across all stations.");
    }

    private Vector3 ResolveStageSpawnPosition(AirportNPC.AirportStage stage)
    {
        var rm = AirportRouteManager.Instance;
        if (rm == null) return Vector3.zero;

        Transform target = null;
        switch (stage)
        {
            case AirportNPC.AirportStage.ArrivingOutside:
                target = rm.GetRandomPoint(rm.spawnPoints) ?? rm.GetRandomPoint(rm.entryPoints);
                break;
            case AirportNPC.AirportStage.EnteringTerminal:
                target = rm.GetRandomPoint(rm.entryPoints);
                break;
            case AirportNPC.AirportStage.QueueingCheckIn:
                target = rm.GetLeastBusyStation(rm.checkInPoints);
                break;
            case AirportNPC.AirportStage.QueueingSecurity:
                target = rm.GetLeastBusyStation(rm.securityPoints);
                break;
            case AirportNPC.AirportStage.QueueingImmigration:
                target = rm.GetLeastBusyStation(rm.immigrationPoints);
                break;
            case AirportNPC.AirportStage.WaitingAtLounge:
                target = rm.GetRandomPoint(rm.loungePoints);
                break;
            case AirportNPC.AirportStage.QueueingGate:
                target = rm.GetLeastBusyStation(rm.gatePoints);
                break;
            default:
                target = rm.GetRandomPoint(rm.entryPoints);
                break;
        }

        if (target != null)
        {
            Vector3 pos = target.position + (Random.insideUnitSphere * 1.5f);
            pos.y = target.position.y;
            return rm.SnapToNavMesh(pos, 5f);
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Spawns a fresh passenger outside at the curbside drop-off with a suitcase in hand.
    /// </summary>
    public AirportNPC SpawnArrivingPassenger()
    {
        var rm = AirportRouteManager.Instance;
        Transform spawnT = (rm != null) ? (rm.GetRandomPoint(rm.spawnPoints) ?? rm.GetRandomPoint(rm.entryPoints)) : null;

        Vector3 spawnPos = spawnT != null ? spawnT.position : new Vector3(36f, -1.08f, -41.1f);
        spawnPos += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1.5f, 1.5f));

        if (rm != null)
            spawnPos = rm.SnapToNavMesh(spawnPos, 5f);

        AirportNPC npc = CreatePassengerObject(spawnPos, AirportNPC.AirportStage.ArrivingOutside, true);
        if (npc != null)
        {
            activePassengers.Add(npc);
        }

        return npc;
    }

    private AirportNPC CreatePassengerObject(Vector3 position, AirportNPC.AirportStage stage, bool attachLuggage)
    {
        string selectedPath = passengerModelPaths[Random.Range(0, passengerModelPaths.Length)];
        GameObject modelPrefab = null;

#if UNITY_EDITOR
        modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
#endif

        GameObject npcObj = null;
        if (modelPrefab != null)
        {
            if (Application.isPlaying)
            {
                npcObj = Instantiate(modelPrefab, position, Quaternion.identity);
            }
            else
            {
#if UNITY_EDITOR
                npcObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(modelPrefab);
                if (npcObj != null)
                {
                    UnityEditor.PrefabUtility.UnpackPrefabInstance(npcObj, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
                    npcObj.transform.position = position;
                }
#else
                npcObj = Instantiate(modelPrefab, position, Quaternion.identity);
#endif
            }
        }

        if (npcObj == null)
        {
            npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcObj.transform.position = position;
        }

        int id = activePassengers.Count + 1;
        npcObj.name = $"Passenger_{id:D2}_{stage}";

        // NavMeshAgent - explicitly check and add
        NavMeshAgent agent = npcObj.GetComponent<NavMeshAgent>();
        if (agent == null) agent = npcObj.AddComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = Random.Range(1.15f, 1.38f);
            agent.angularSpeed = 180f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.4f;
            agent.radius = 0.3f;
            agent.height = 1.75f;
            agent.avoidancePriority = Random.Range(30, 70);
            agent.updateRotation = false;
        }

        // CapsuleCollider + Rigidbody
        CapsuleCollider col = npcObj.GetComponent<CapsuleCollider>();
        if (col == null) col = npcObj.AddComponent<CapsuleCollider>();
        if (col != null)
        {
            col.height = 1.75f;
            col.radius = 0.3f;
            col.center = new Vector3(0, 0.88f, 0);
        }

        Rigidbody rb = npcObj.GetComponent<Rigidbody>();
        if (rb == null) rb = npcObj.AddComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Animator setup
        Animator anim = npcObj.GetComponent<Animator>();
        if (anim == null) anim = npcObj.AddComponent<Animator>();
#if UNITY_EDITOR
        RuntimeAnimatorController controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);
        if (controller != null && anim != null) anim.runtimeAnimatorController = controller;
#endif
        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.speed = Random.Range(0.92f, 1.15f);
        }

        // Attach Luggage to right hand
        if (attachLuggage && anim != null)
        {
            Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null)
            {
#if UNITY_EDITOR
                GameObject luggagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(luggageModelPath);
                if (luggagePrefab != null)
                {
                    GameObject luggage = Application.isPlaying
                        ? Instantiate(luggagePrefab)
                        : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(luggagePrefab);
                    if (luggage != null)
                    {
                        luggage.name = "NPC_Luggage";
                        luggage.transform.SetParent(hand, false);
                        luggage.transform.localPosition = new Vector3(-0.02f, 0.05f, 0.02f);
                        luggage.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);
                        luggage.transform.localScale = Vector3.one * 0.7f;

                        Renderer r = luggage.GetComponentInChildren<Renderer>();
                        if (r != null)
                        {
                            Material mat = new Material(r.sharedMaterial);
                            mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.3f, 0.85f);
                            r.material = mat;
                        }
                    }
                }
#endif
            }
        }

        // Procedural sitting for lounge
        ProceduralSit sit = npcObj.GetComponent<ProceduralSit>();
        if (sit == null) sit = npcObj.AddComponent<ProceduralSit>();
        if (sit != null) sit.enabled = false;

        // State Machine
        AirportNPC npcComp = npcObj.GetComponent<AirportNPC>();
        if (npcComp == null) npcComp = npcObj.AddComponent<AirportNPC>();
        if (npcComp != null) npcComp.SetStartingStage(stage);

        return npcComp;
    }

    /// <summary>
    /// Called when an NPC boards the aircraft and finishes their flight journey.
    /// </summary>
    public void NotifyPassengerBoarded(AirportNPC npc)
    {
        if (npc != null)
        {
            activePassengers.Remove(npc);
        }

        // Trigger a fresh arrival shortly after a passenger boards
        nextSpawnTime = Time.time + Random.Range(3f, 8f);
    }
}
