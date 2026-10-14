using System.Collections;
using UnityEngine;

/// <summary>
/// Controls realistic vehicle movement along a series of waypoints,
/// including smooth deceleration at drop-off zones and acceleration on exit.
/// </summary>
public class DynamicVehicleAI : MonoBehaviour
{
    [Header("Vehicle Path")]
    [Tooltip("Waypoints the vehicle will follow.")]
    public Transform[] pathNodes;
    
    [Header("Movement Settings")]
    public float maxSpeed = 10f;
    public float acceleration = 3f;
    public float turnSpeed = 5f;
    public float waypointThreshold = 1f;

    [Header("Drop-off Behavior")]
    [Tooltip("Index of the waypoint where the car should stop (e.g., terminal drop-off).")]
    public int dropOffNodeIndex = 1;
    public float minWaitTime = 5f;
    public float maxWaitTime = 12f;

    private int currentNodeIndex = 0;
    private float currentSpeed = 0f;
    private bool isWaiting = false;
    private Rigidbody rb; // Optional: If using physics

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void Update()
    {
        if (pathNodes.Length == 0 || isWaiting) return;

        Transform targetNode = pathNodes[currentNodeIndex];
        Vector3 direction = (targetNode.position - transform.position).normalized;
        
        // Steering
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        // Distance Check & Logic
        float distanceToTarget = Vector3.Distance(transform.position, targetNode.position);
        
        // Smoothly decelerate if approaching the drop-off node
        if (currentNodeIndex == dropOffNodeIndex && distanceToTarget < 10f)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * (acceleration * 0.5f));
            
            if (distanceToTarget < waypointThreshold || currentSpeed < 0.1f)
            {
                StartCoroutine(DropOffRoutine());
            }
        }
        else
        {
            // Accelerate normally
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, Time.deltaTime * acceleration);
            
            if (distanceToTarget < waypointThreshold)
            {
                currentNodeIndex = (currentNodeIndex + 1) % pathNodes.Length;
            }
        }

        // Movement application
        if (rb != null && !rb.isKinematic)
        {
            Vector3 newPosition = rb.position + transform.forward * (currentSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position += transform.forward * (currentSpeed * Time.deltaTime);
        }
    }

    private IEnumerator DropOffRoutine()
    {
        isWaiting = true;
        currentSpeed = 0f;
        
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        // Proceed to next node
        currentNodeIndex = (currentNodeIndex + 1) % pathNodes.Length;
        isWaiting = false;
    }
}
