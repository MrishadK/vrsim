using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Professional Editor tool to completely overhaul and configure the airport passenger flow.
/// Eliminates wandering, outside boom barriers, Y=261 sky waypoints, and inside-to-outside looping.
/// Configures exact physical waypoints for:
/// Curbside -> Entrance -> Check-in -> Security -> Immigration -> Lounge -> Gate A12 -> Jetway -> Boeing 737.
/// </summary>
public class SetupAirportNPCFlow
{
    [MenuItem("Tools/Setup Real Airport NPC Flow", false, 40)]
    public static void Execute()
    {
        Debug.Log("<color=#00FFCC>[AirportFlowSetup]</color> Beginning complete airport NPC flow overhaul...");

        // 1. Decommission conflicting systems
        DecommissionDisruptiveScripts();

        // 2. Setup or Find AirportRouteManager & TrafficManager
        AirportRouteManager routeManager = Object.FindObjectOfType<AirportRouteManager>();
        if (routeManager == null)
        {
            GameObject routeObj = GameObject.Find("AirportRouteManager");
            if (routeObj == null) routeObj = new GameObject("AirportRouteManager");
            routeManager = routeObj.AddComponent<AirportRouteManager>();
        }

        AirportTrafficManager trafficManager = routeManager.GetComponent<AirportTrafficManager>();
        if (trafficManager == null)
        {
            trafficManager = routeManager.gameObject.AddComponent<AirportTrafficManager>();
        }

        // 3. Create or Reset Waypoint Hierarchy
        Transform flowRoot = SetupFlowHierarchy();

        // 4. Build and assign calibrated waypoints
        PopulateCalibratedWaypoints(flowRoot, routeManager);

        // 5. Ensure Animator Controller exists
        EnsureAnimatorController();

        // 6. Save Scene
        EditorUtility.SetDirty(routeManager);
        EditorUtility.SetDirty(trafficManager);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#00FFCC>[AirportFlowSetup]</color> SUCCESS! Airport NPC Flow overhaul completed.");
        Debug.Log("<color=#00FFCC>[AirportFlowSetup]</color> Passengers now strictly follow one-way airport flow with zero random wandering and zero backward movement.");
    }

    private static void DecommissionDisruptiveScripts()
    {
        // 1. Disable OutsideNPCSpawner on Cube (32)
        var spawner = Object.FindObjectOfType<OutsideNPCSpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
            Debug.Log("[AirportFlowSetup] Disabled disruptive OutsideNPCSpawner on " + spawner.gameObject.name);
        }

        // 2. Remove rogue WanderingAI
        var wanderers = Object.FindObjectsOfType<WanderingAI>();
        foreach (var w in wanderers)
        {
            if (w != null)
            {
                w.enabled = false;
                Debug.Log("[AirportFlowSetup] Disabled legacy WanderingAI on " + w.gameObject.name);
            }
        }

        // 3. Clean pre-placed scene characters so they don't wander randomly
        string[] characterNames = new string[] { "Survival_Character_Var1", "Survival_Character_Var2", "Survival_Character_Var3" };
        foreach (var name in characterNames)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                var npcComp = go.GetComponent<AirportNPC>();
                if (npcComp != null)
                {
                    // Let traffic manager handle active passengers cleanly
                    go.SetActive(false);
                    Debug.Log($"[AirportFlowSetup] Deactivated static scene character '{name}' in favor of dynamic traffic manager.");
                }
            }
        }
    }

    private static Transform SetupFlowHierarchy()
    {
        GameObject existing = GameObject.Find("Airport_Flow_Waypoints");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject root = new GameObject("Airport_Flow_Waypoints");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        return root.transform;
    }

    private static void PopulateCalibratedWaypoints(Transform root, AirportRouteManager rm)
    {
        rm.spawnPoints = new List<Transform>();
        rm.entryPoints = new List<Transform>();
        rm.checkInPoints = new List<Transform>();
        rm.securityPoints = new List<Transform>();
        rm.immigrationPoints = new List<Transform>();
        rm.loungePoints = new List<Transform>();
        rm.gatePoints = new List<Transform>();
        rm.boardingPoints = new List<Transform>();
        rm.aircraftSeatPoints = new List<Transform>();

        // ─────────────────────────────────────────────
        // 0. Curbside Arrival (Outside Spawn)
        // ─────────────────────────────────────────────
        Transform spawnGroup = CreateGroup("00_Curbside_Arrivals", root);
        rm.spawnPoints.Add(CreateWaypoint("Spawn_Curbside_01", spawnGroup, new Vector3(38.0f, -1.08f, -41.1f), Quaternion.Euler(0, -90f, 0)));
        rm.spawnPoints.Add(CreateWaypoint("Spawn_Curbside_02", spawnGroup, new Vector3(37.0f, -1.08f, -44.0f), Quaternion.Euler(0, -90f, 0)));
        rm.spawnPoints.Add(CreateWaypoint("Spawn_Curbside_03", spawnGroup, new Vector3(39.0f, -1.08f, -38.5f), Quaternion.Euler(0, -90f, 0)));

        // ─────────────────────────────────────────────
        // 1. Terminal Entrance Doors
        // ─────────────────────────────────────────────
        Transform entryGroup = CreateGroup("01_Terminal_Entrance", root);
        rm.entryPoints.Add(CreateWaypoint("Entrance_Main_Door", entryGroup, new Vector3(31.50f, -1.08f, -41.12f), Quaternion.Euler(0, -90f, 0)));
        rm.entryPoints.Add(CreateWaypoint("Entrance_Concourse_In", entryGroup, new Vector3(25.0f, -1.08f, -41.12f), Quaternion.Euler(0, -90f, 0)));

        // ─────────────────────────────────────────────
        // 2. Check-In Counter & Baggage Scale
        // ─────────────────────────────────────────────
        Transform checkGroup = CreateGroup("02_CheckIn_Hall", root);
        // Desk counter facing south
        Transform checkDesk = CreateWaypoint("CheckIn_Counter_01", checkGroup, new Vector3(2.21f, -1.11f, -58.41f), Quaternion.Euler(0, 0f, 0));
        var checkQueue = checkDesk.gameObject.AddComponent<QueueStation>();
        checkQueue.spacing = 1.4f;
        checkQueue.customQueueDirection = new Vector3(0, 0, 1f); // Queue forms along +Z
        rm.checkInPoints.Add(checkDesk);

        // Second counter if needed
        Transform checkDesk2 = CreateWaypoint("CheckIn_Counter_02", checkGroup, new Vector3(4.8f, -1.11f, -58.41f), Quaternion.Euler(0, 0f, 0));
        var checkQueue2 = checkDesk2.gameObject.AddComponent<QueueStation>();
        checkQueue2.spacing = 1.4f;
        checkQueue2.customQueueDirection = new Vector3(0, 0, 1f);
        rm.checkInPoints.Add(checkDesk2);

        // ─────────────────────────────────────────────
        // 3. Security Screening (Metal Detector Arch)
        // ─────────────────────────────────────────────
        Transform secGroup = CreateGroup("03_Security_Screening", root);
        Transform secStation = CreateWaypoint("Security_MetalDetector_Lane", secGroup, new Vector3(17.70f, -1.11f, -9.92f), Quaternion.Euler(0, 0f, 0));
        var secQueue = secStation.gameObject.AddComponent<QueueStation>();
        secQueue.spacing = 1.4f;
        secQueue.customQueueDirection = new Vector3(0, 0, -1f); // Queue forms approaching from -Z
        rm.securityPoints.Add(secStation);

        // ─────────────────────────────────────────────
        // 4. Immigration / Passport Control
        // ─────────────────────────────────────────────
        Transform immGroup = CreateGroup("04_Passport_Control", root);
        Transform immStation = CreateWaypoint("Immigration_Booth_01", immGroup, new Vector3(1.90f, -1.11f, 3.69f), Quaternion.Euler(0, -90f, 0));
        var immQueue = immStation.gameObject.AddComponent<QueueStation>();
        immQueue.spacing = 1.4f;
        immQueue.customQueueDirection = new Vector3(1f, 0, 0); // Queue forms along +X
        rm.immigrationPoints.Add(immStation);

        // ─────────────────────────────────────────────
        // 5. Departure Lounge & Seating
        // ─────────────────────────────────────────────
        Transform loungeGroup = CreateGroup("05_Departure_Lounge", root);
        rm.loungePoints.Add(CreateWaypoint("Lounge_Seating_01", loungeGroup, new Vector3(-12.52f, -0.70f, 4.30f), Quaternion.Euler(0, 90f, 0)));
        rm.loungePoints.Add(CreateWaypoint("Lounge_Seating_02", loungeGroup, new Vector3(-10.50f, -0.70f, 6.20f), Quaternion.Euler(0, 90f, 0)));
        rm.loungePoints.Add(CreateWaypoint("Lounge_Browsing_FIDS", loungeGroup, new Vector3(-14.20f, -0.70f, 1.80f), Quaternion.Euler(0, 0f, 0)));
        rm.loungePoints.Add(CreateWaypoint("Lounge_Window_View", loungeGroup, new Vector3(-8.80f, -0.70f, 5.10f), Quaternion.Euler(0, -90f, 0)));

        // ─────────────────────────────────────────────
        // 6. Departure Gate A12
        // ─────────────────────────────────────────────
        Transform gateGroup = CreateGroup("06_Gate_A12", root);
        Transform gateDesk = CreateWaypoint("Gate_A12_Podium", gateGroup, new Vector3(-14.50f, -0.40f, 2.00f), Quaternion.Euler(0, 90f, 0));
        var gateQueue = gateDesk.gameObject.AddComponent<QueueStation>();
        gateQueue.spacing = 1.4f;
        gateQueue.customQueueDirection = new Vector3(0, 0, 1f); // Queue forms along +Z
        rm.gatePoints.Add(gateDesk);

        // ─────────────────────────────────────────────
        // 7. Telescopic Jetway Bridge
        // ─────────────────────────────────────────────
        Transform jetwayGroup = CreateGroup("07_Jetway_Boarding", root);
        rm.boardingPoints.Add(CreateWaypoint("Jetway_Entrance", jetwayGroup, new Vector3(-11.5f, 0.40f, 4.5f), Quaternion.Euler(0, 45f, 0)));
        rm.boardingPoints.Add(CreateWaypoint("Jetway_Midway", jetwayGroup, new Vector3(-7.5f, 0.50f, 8.5f), Quaternion.Euler(0, 45f, 0)));
        rm.boardingPoints.Add(CreateWaypoint("Jetway_Aircraft_Threshold", jetwayGroup, new Vector3(-5.0f, 0.60f, 11.2f), Quaternion.Euler(0, 45f, 0)));

        // ─────────────────────────────────────────────
        // 8. Boeing 737 Aircraft Cabin
        // ─────────────────────────────────────────────
        Transform planeGroup = CreateGroup("08_Aircraft_Cabin", root);
        rm.aircraftSeatPoints.Add(CreateWaypoint("Cabin_Aisle_Entry", planeGroup, new Vector3(-3.60f, 0.60f, 13.25f), Quaternion.Euler(0, 90f, 0)));
        rm.aircraftSeatPoints.Add(CreateWaypoint("Cabin_Seat_12A", planeGroup, new Vector3(-3.20f, 0.60f, 15.00f), Quaternion.Euler(0, 0f, 0)));
        rm.aircraftSeatPoints.Add(CreateWaypoint("Cabin_Seat_12B", planeGroup, new Vector3(-2.80f, 0.60f, 15.00f), Quaternion.Euler(0, 0f, 0)));
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    private static Transform CreateWaypoint(string name, Transform parent, Vector3 rawPosition, Quaternion rotation)
    {
        GameObject wp = new GameObject(name);
        wp.transform.SetParent(parent, false);

        // Snap precisely to the NavMesh
        Vector3 snapped = rawPosition;
        if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            snapped = hit.position;
        }

        wp.transform.position = snapped;
        wp.transform.rotation = rotation;
        return wp.transform;
    }

    private static void EnsureAnimatorController()
    {
        string animDir = "Assets/Animations";
        if (!System.IO.Directory.Exists(animDir)) System.IO.Directory.CreateDirectory(animDir);

        string controllerPath = animDir + "/NPC_Locomotion.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null)
        {
            SetupAirportNPCs.Execute();
        }
    }
}
