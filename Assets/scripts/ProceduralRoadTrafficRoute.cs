using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Supplies a continuous traffic route from the Procedural Road Model pieces
/// already placed in the scene. The two nearest X-aligned road chains are
/// treated as opposite travel lanes and are followed in a loop.
/// </summary>
public static class ProceduralRoadTrafficRoute
{
    private static readonly List<Vector3> roadCenters = new List<Vector3>();
    private static bool isCached;

    public static bool SetNextDestination(NavMeshAgent agent, Transform vehicle,
        ref int waypointIndex, ref int travelDirection)
    {
        CacheRoadCenters();
        if (agent == null || roadCenters.Count == 0)
            return false;

        List<Vector3> lane = GetNearestLane(vehicle.position.x);
        if (lane.Count < 2)
            return false;

        int closest = FindClosestIndex(lane, vehicle.position.z);
        if (travelDirection == 0)
        {
            // The right-hand road chain travels toward decreasing Z and the
            // left-hand chain toward increasing Z, forming a continuous loop.
            float averageX = 0f;
            foreach (Vector3 point in roadCenters) averageX += point.x;
            travelDirection = lane[0].x >= averageX / roadCenters.Count ? -1 : 1;

            // Vehicles placed outside the road are moved to the nearest lane,
            // rather than trying to cut across buildings or oncoming traffic.
            if (Mathf.Abs(vehicle.position.x - lane[0].x) > 4f || !agent.isOnNavMesh)
            {
                Vector3 entry = new Vector3(lane[0].x, vehicle.position.y, lane[closest].z);
                if (NavMesh.SamplePosition(entry, out NavMeshHit entryHit, 8f, NavMesh.AllAreas))
                    agent.Warp(entryHit.position);
            }
        }

        if (!agent.isOnNavMesh)
            return false;

        for (int attempt = 1; attempt <= lane.Count; attempt++)
        {
            waypointIndex = Mod(closest + travelDirection * attempt, lane.Count);
            Vector3 candidate = lane[waypointIndex];
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                continue;

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                return true;
            }
        }

        return false;
    }

    private static void CacheRoadCenters()
    {
        if (isCached) return;
        isCached = true;

        foreach (Transform item in Object.FindObjectsOfType<Transform>())
        {
            if (item.name.StartsWith("Procedural Road Model"))
                roadCenters.Add(item.position);
        }
    }

    private static List<Vector3> GetNearestLane(float x)
    {
        float laneX = roadCenters[0].x;
        foreach (Vector3 point in roadCenters)
        {
            if (Mathf.Abs(point.x - x) < Mathf.Abs(laneX - x))
                laneX = point.x;
        }

        List<Vector3> lane = new List<Vector3>();
        foreach (Vector3 point in roadCenters)
        {
            if (Mathf.Abs(point.x - laneX) < 0.5f)
                lane.Add(point);
        }

        lane.Sort((a, b) => a.z.CompareTo(b.z));
        return lane;
    }

    private static int FindClosestIndex(List<Vector3> lane, float z)
    {
        int closest = 0;
        for (int i = 1; i < lane.Count; i++)
        {
            if (Mathf.Abs(lane[i].z - z) < Mathf.Abs(lane[closest].z - z))
                closest = i;
        }
        return closest;
    }

    private static int Mod(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
