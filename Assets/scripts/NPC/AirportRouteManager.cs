using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Master repository of verified airport flow waypoints.
/// Controls the path nodes for every stage of the passenger journey:
/// Curbside -> Entrance -> Check-in -> Security -> Immigration -> Lounge -> Gate -> Aircraft.
/// </summary>
public class AirportRouteManager : MonoBehaviour
{
    [Header("0. Curbside Arrival (Spawn Outside)")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("1. Terminal Entrance")]
    public List<Transform> entryPoints = new List<Transform>();

    [Header("2. Check-In Counters")]
    public List<Transform> checkInPoints = new List<Transform>();

    [Header("3. Security Screening Checkpoints")]
    public List<Transform> securityPoints = new List<Transform>();

    [Header("4. Immigration / Passport Control")]
    public List<Transform> immigrationPoints = new List<Transform>();

    [Header("5. Departure Lounge & Waiting Area")]
    public List<Transform> loungePoints = new List<Transform>();

    [Header("6. Departure Gates (Gate A12)")]
    public List<Transform> gatePoints = new List<Transform>();

    [Header("7. Jetway & Aircraft Boarding")]
    public List<Transform> boardingPoints = new List<Transform>();

    [Header("8. Seated in Aircraft Cabin (Optional)")]
    public List<Transform> aircraftSeatPoints = new List<Transform>();

    public static AirportRouteManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        CleanupLists();
    }

    private void CleanupLists()
    {
        entryPoints.RemoveAll(p => p == null);
        checkInPoints.RemoveAll(p => p == null);
        securityPoints.RemoveAll(p => p == null);
        immigrationPoints.RemoveAll(p => p == null);
        loungePoints.RemoveAll(p => p == null);
        gatePoints.RemoveAll(p => p == null);
        boardingPoints.RemoveAll(p => p == null);
        aircraftSeatPoints.RemoveAll(p => p == null);
    }

    private void Start()
    {
        EnsureValidWaypoints();
    }

    public void EnsureValidWaypoints()
    {
        CleanupLists();

        // Check if waypoints are empty or contaminated with sky/car barrier points
        bool needsSetup = (entryPoints.Count == 0 || checkInPoints.Count == 0 || securityPoints.Count == 0 ||
                           immigrationPoints.Count == 0 || gatePoints.Count == 0 || boardingPoints.Count == 0);

        if (!needsSetup)
        {
            foreach (var cp in checkInPoints)
            {
                if (cp != null && (cp.position.y > 10f || cp.name.Contains("3D_Sign")))
                {
                    needsSetup = true;
                    break;
                }
            }
        }

        if (needsSetup)
        {
            Debug.Log("[AirportRouteManager] Initializing verified physical airport flow waypoints...");
            BuildRuntimeWaypoints();
        }

        // Decommission disruptive scripts
        var spawner = FindObjectOfType<OutsideNPCSpawner>();
        if (spawner != null && spawner.enabled)
        {
            spawner.enabled = false;
            Debug.Log("[AirportRouteManager] Deactivated disruptive OutsideNPCSpawner.");
        }

        var wanderers = FindObjectsOfType<WanderingAI>();
        foreach (var w in wanderers)
        {
            if (w != null && w.enabled) w.enabled = false;
        }

        // Ensure AirportTrafficManager is running
        if (GetComponent<AirportTrafficManager>() == null)
        {
            gameObject.AddComponent<AirportTrafficManager>();
        }
    }

    private void BuildRuntimeWaypoints()
    {
        GameObject existing = GameObject.Find("Airport_Flow_Waypoints");
        if (existing != null) Destroy(existing);

        GameObject root = new GameObject("Airport_Flow_Waypoints");
        root.transform.position = Vector3.zero;

        spawnPoints.Clear();
        entryPoints.Clear();
        checkInPoints.Clear();
        securityPoints.Clear();
        immigrationPoints.Clear();
        loungePoints.Clear();
        gatePoints.Clear();
        boardingPoints.Clear();
        aircraftSeatPoints.Clear();

        // 0. Curbside Spawn
        Transform spawnG = new GameObject("00_Curbside_Arrivals").transform;
        spawnG.SetParent(root.transform, false);
        spawnPoints.Add(CreateWaypointNode("Spawn_Curbside_01", spawnG, new Vector3(38.0f, -1.08f, -41.1f), Quaternion.Euler(0, -90f, 0)));
        spawnPoints.Add(CreateWaypointNode("Spawn_Curbside_02", spawnG, new Vector3(37.0f, -1.08f, -44.0f), Quaternion.Euler(0, -90f, 0)));

        // 1. Entrance Doors
        Transform entryG = new GameObject("01_Terminal_Entrance").transform;
        entryG.SetParent(root.transform, false);
        entryPoints.Add(CreateWaypointNode("Entrance_Main_Door", entryG, new Vector3(31.50f, -1.08f, -41.12f), Quaternion.Euler(0, -90f, 0)));
        entryPoints.Add(CreateWaypointNode("Entrance_Concourse_In", entryG, new Vector3(25.0f, -1.08f, -41.12f), Quaternion.Euler(0, -90f, 0)));

        // 2. Check-In Hall
        Transform checkG = new GameObject("02_CheckIn_Hall").transform;
        checkG.SetParent(root.transform, false);
        Transform checkDesk = CreateWaypointNode("CheckIn_Counter_01", checkG, new Vector3(2.21f, -1.11f, -58.41f), Quaternion.Euler(0, 0f, 0));
        var q1 = checkDesk.gameObject.AddComponent<QueueStation>();
        q1.spacing = 1.4f;
        q1.customQueueDirection = new Vector3(0, 0, 1f);
        checkInPoints.Add(checkDesk);

        // 3. Security Screening Arch
        Transform secG = new GameObject("03_Security_Screening").transform;
        secG.SetParent(root.transform, false);
        Transform secDesk = CreateWaypointNode("Security_MetalDetector_Lane", secG, new Vector3(17.70f, -1.11f, -9.92f), Quaternion.Euler(0, 0f, 0));
        var q2 = secDesk.gameObject.AddComponent<QueueStation>();
        q2.spacing = 1.4f;
        q2.customQueueDirection = new Vector3(0, 0, -1f);
        securityPoints.Add(secDesk);

        // 4. Immigration / Passport Control
        Transform immG = new GameObject("04_Passport_Control").transform;
        immG.SetParent(root.transform, false);
        Transform immDesk = CreateWaypointNode("Immigration_Booth_01", immG, new Vector3(1.90f, -1.11f, 3.69f), Quaternion.Euler(0, -90f, 0));
        var q3 = immDesk.gameObject.AddComponent<QueueStation>();
        q3.spacing = 1.4f;
        q3.customQueueDirection = new Vector3(1f, 0, 0);
        immigrationPoints.Add(immDesk);

        // 5. Departure Lounge
        Transform loungeG = new GameObject("05_Departure_Lounge").transform;
        loungeG.SetParent(root.transform, false);
        loungePoints.Add(CreateWaypointNode("Lounge_Seating_01", loungeG, new Vector3(-12.52f, -0.70f, 4.30f), Quaternion.Euler(0, 90f, 0)));
        loungePoints.Add(CreateWaypointNode("Lounge_Seating_02", loungeG, new Vector3(-10.50f, -0.70f, 6.20f), Quaternion.Euler(0, 90f, 0)));
        loungePoints.Add(CreateWaypointNode("Lounge_Browsing_FIDS", loungeG, new Vector3(-14.20f, -0.70f, 1.80f), Quaternion.Euler(0, 0f, 0)));

        // 6. Departure Gate A12
        Transform gateG = new GameObject("06_Gate_A12").transform;
        gateG.SetParent(root.transform, false);
        Transform gateDesk = CreateWaypointNode("Gate_A12_Podium", gateG, new Vector3(-14.50f, -0.40f, 2.00f), Quaternion.Euler(0, 90f, 0));
        var q4 = gateDesk.gameObject.AddComponent<QueueStation>();
        q4.spacing = 1.4f;
        q4.customQueueDirection = new Vector3(0, 0, 1f);
        gatePoints.Add(gateDesk);

        // 7. Jetway & Cabin
        Transform jetG = new GameObject("07_Jetway_Boarding").transform;
        jetG.SetParent(root.transform, false);
        boardingPoints.Add(CreateWaypointNode("Jetway_Entrance", jetG, new Vector3(-11.5f, 0.40f, 4.5f), Quaternion.Euler(0, 45f, 0)));
        boardingPoints.Add(CreateWaypointNode("Jetway_Threshold", jetG, new Vector3(-5.0f, 0.60f, 11.2f), Quaternion.Euler(0, 45f, 0)));

        // 8. Aircraft Cabin
        Transform planeG = new GameObject("08_Aircraft_Cabin").transform;
        planeG.SetParent(root.transform, false);
        aircraftSeatPoints.Add(CreateWaypointNode("Cabin_Seat_12A", planeG, new Vector3(-3.20f, 0.60f, 15.00f), Quaternion.Euler(0, 0f, 0)));
        aircraftSeatPoints.Add(CreateWaypointNode("Cabin_Seat_12B", planeG, new Vector3(-2.80f, 0.60f, 15.00f), Quaternion.Euler(0, 0f, 0)));
    }

    private Transform CreateWaypointNode(string name, Transform parent, Vector3 rawPos, Quaternion rot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = SnapToNavMesh(rawPos, 4f);
        go.transform.rotation = rot;
        return go.transform;
    }

    public Transform GetRandomPoint(List<Transform> list)
    {
        if (list == null || list.Count == 0) return null;
        int idx = Random.Range(0, list.Count);
        return list[idx];
    }

    /// <summary>
    /// Intelligently picks the station with the shortest queue line.
    /// </summary>
    public Transform GetLeastBusyStation(List<Transform> list)
    {
        if (list == null || list.Count == 0) return null;

        Transform best = null;
        int minQueue = int.MaxValue;

        foreach (var t in list)
        {
            if (t == null) continue;
            var qs = t.GetComponent<QueueStation>();
            int qCount = (qs != null) ? qs.QueueCount : 0;
            if (qCount < minQueue)
            {
                minQueue = qCount;
                best = t;
            }
        }

        return best ?? GetRandomPoint(list);
    }

    /// <summary>
    /// Safely snaps a world position to the nearest NavMesh point.
    /// </summary>
    public Vector3 SnapToNavMesh(Vector3 targetPos, float maxDistance = 4f)
    {
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return targetPos;
    }
}
