using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the airplane's pushback, taxi, and aggressive takeoff using Rigidbody
/// for perfectly smooth, jitter-free VR movement.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CinematicTakeoffController : MonoBehaviour
{
    [Header("Sequence Nodes")]
    public Transform[] pushbackNodes;
    public Transform[] taxiNodes;
    public Transform[] takeoffNodes;

    [Header("Speed Settings")]
    public float pushbackSpeed = 2f;
    public float taxiSpeed = 15f;
    public float takeoffSpeed = 85f;
    public float accelerationRate = 10f;
    
    [Header("Timing")]
    public float initialDelay = 5.0f;

    private Rigidbody rb;
    private bool isFlying = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // CRITICAL for VR: Ensure interpolation is on so the nested player rig doesn't stutter.
        rb.isKinematic = true; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void BeginFlightSequence()
    {
        if (isFlying) return;
        StartCoroutine(FlightRoutine());
    }

    private IEnumerator FlightRoutine()
    {
        isFlying = true;
        Debug.Log("[CinematicFlight] Flight sequence triggered. Waiting for delay...");
        
        yield return new WaitForSeconds(initialDelay);

        Debug.Log("[CinematicFlight] Commencing Pushback.");
        yield return MoveAlongPath(pushbackNodes, pushbackSpeed, 1f, true);

        Debug.Log("[CinematicFlight] Commencing Taxi.");
        yield return MoveAlongPath(taxiNodes, taxiSpeed, 5f, false);

        Debug.Log("[CinematicFlight] Cleared for Takeoff!");
        yield return MoveAlongPath(takeoffNodes, takeoffSpeed, accelerationRate, false);
        
        Debug.Log("[CinematicFlight] Experience Completed.");
    }

    private IEnumerator MoveAlongPath(Transform[] path, float targetSpeed, float accel, bool reverseDirection)
    {
        if (path == null || path.Length == 0) yield break;

        float currentSpeed = 0f;
        int targetNode = 0;

        while (targetNode < path.Length)
        {
            Vector3 targetPos = path[targetNode].position;
            Vector3 direction = (targetPos - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetPos);

            // Smoothly accelerate
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * accel);

            if (distance < 1f)
            {
                targetNode++;
                if (targetNode >= path.Length) break;
            }

            // Smooth rotation towards target (ignore Y-axis tilting on the ground)
            if (direction != Vector3.zero)
            {
                Quaternion lookRot;
                if (reverseDirection)
                {
                    // Look away from direction if pushing back
                    lookRot = Quaternion.LookRotation(-direction);
                }
                else
                {
                    lookRot = Quaternion.LookRotation(direction);
                }
                
                // Keep the plane level for taxi, allow pitch for takeoff
                if (path == taxiNodes)
                {
                    lookRot.x = 0;
                    lookRot.z = 0;
                }
                
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 2f));
            }

            // Use MovePosition for jitter-free physics updates
            Vector3 newPos = transform.position + direction * (currentSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            // Wait for the next FixedUpdate (crucial for Rigidbody)
            yield return new WaitForFixedUpdate();
        }
    }
}
