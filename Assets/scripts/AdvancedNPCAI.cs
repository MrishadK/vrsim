using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls advanced NPC behavior including waypoint navigation, obstacle avoidance,
/// and smooth Animator blending.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class AdvancedNPCAI : MonoBehaviour
{
    [Header("Navigation Settings")]
    [Tooltip("List of transforms the NPC will visit in order.")]
    public Transform[] waypoints;
    [Tooltip("Minimum time the NPC waits at a waypoint.")]
    public float minWaitTime = 2.0f;
    [Tooltip("Maximum time the NPC waits at a waypoint.")]
    public float maxWaitTime = 5.0f;
    
    [Header("Animation Settings")]
    [Tooltip("Name of the float parameter in the Animator for movement speed.")]
    public string speedParameter = "Speed";
    [Tooltip("Name of the trigger parameter in the Animator for interaction.")]
    public string interactTrigger = "Interact";
    
    private NavMeshAgent agent;
    private Animator animator;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Optimize avoidance for a crowded VR space
        agent.avoidancePriority = Random.Range(30, 70); 
    }

    private void Start()
    {
        if (waypoints.Length > 0)
        {
            SetNextDestination();
        }
        else
        {
            Debug.LogWarning($"[AdvancedNPCAI] No waypoints assigned to {gameObject.name}");
        }
    }

    private void Update()
    {
        // Update animator blend tree based on current velocity
        float speed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat(speedParameter, speed, 0.1f, Time.deltaTime);

        // Check if we have reached our destination
        if (!agent.pathPending && !isWaiting)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    StartCoroutine(WaitAtWaypoint());
                }
            }
        }
    }

    private IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        
        // Face the waypoint's forward direction smoothly
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        float waitDuration = Random.Range(minWaitTime, maxWaitTime);
        float elapsedTime = 0f;

        // Optionally trigger an interaction animation (e.g. handing a passport)
        if (Random.value > 0.5f) // 50% chance to play interaction at waypoint
        {
            animator.SetTrigger(interactTrigger);
        }

        while (elapsedTime < waitDuration)
        {
            if (targetWaypoint != null)
            {
                Vector3 direction = targetWaypoint.forward;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        SetNextDestination();
        
        isWaiting = false;
    }

    private void SetNextDestination()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (waypoints.Length > 0 && waypoints[currentWaypointIndex] != null)
            {
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            }
        }
    }
}
