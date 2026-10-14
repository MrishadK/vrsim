using UnityEngine;
using System.Collections.Generic;

public class VehiclePathFollower : MonoBehaviour
{
    public Transform pathRoot;
    public float speed = 8f;
    public float turnSpeed = 2f;
    public bool loop = true;
    
    private List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;

    void Start()
    {
        if (pathRoot != null)
        {
            foreach (Transform child in pathRoot)
            {
                waypoints.Add(child);
            }
        }
        
        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0].position;
            currentWaypointIndex = 1;
        }
    }

    void Update()
    {
        if (waypoints.Count < 2) return;

        Transform target = waypoints[currentWaypointIndex];
        Vector3 targetPos = target.position;
        targetPos.y = transform.position.y; 

        float distance = Vector3.Distance(transform.position, targetPos);
        
        if (distance < 2f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                if (loop) currentWaypointIndex = 0;
                else currentWaypointIndex = waypoints.Count - 1; 
            }
        }
        else
        {
            Vector3 dir = (targetPos - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
            
            if (dir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
    }
}