using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CarAnimator))]
public class VehicleAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private CarAnimator carAnimator;

    public Transform target;
    public float wanderRadius = 50f;
    public float wanderTimer = 10f;

    private float timer;
    private int roadWaypoint;
    private int travelDirection;

    private void Awake()
    {
        // VehicleTyreAnimator is a complete NavMesh driver for the Passport.
        // Running both scripts makes them repeatedly overwrite each other's
        // destination and rotation, which is what caused the sideways motion.
        if (GetComponent<VehicleTyreAnimator>() != null)
        {
            enabled = false;
        }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        carAnimator = GetComponent<CarAnimator>();
        
        // Allow NavMeshAgent to handle movement so it doesn't go through buildings
        agent.updatePosition = true; 
        agent.updateRotation = true;

        // This bus was using a pedestrian-sized avoidance radius, so agents
        // could approach and strike its side before avoidance began.
        if (gameObject.name.StartsWith("bus"))
        {
            agent.radius = Mathf.Max(agent.radius, 1.4f);
            agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 4f);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
        
        timer = wanderTimer;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= wanderTimer || (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)))
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                ProceduralRoadTrafficRoute.SetNextDestination(agent, transform, ref roadWaypoint, ref travelDirection);
            }
            timer = 0;
        }

        // No longer manually syncing nextPosition because agent moves the transform itself

        if (agent != null && agent.isActiveAndEnabled && agent.hasPath)
        {
            Vector3 desiredDir = (agent.steeringTarget - transform.position).normalized;
            
            // Calculate turning input based on angle to target
            float angleToTarget = Vector3.SignedAngle(transform.forward, desiredDir, Vector3.up);
            
            // Map angle to a -1 to 1 turn input
            float turnInput = Mathf.Clamp(angleToTarget / carAnimator.maxSteeringAngle, -1f, 1f);
            
            // Move forward
            float moveInput = 1f;
            
            // Slow down when arriving or making very sharp turns
            if (agent.remainingDistance < agent.stoppingDistance + 2f)
            {
                moveInput = Mathf.Lerp(0, 1, agent.remainingDistance / (agent.stoppingDistance + 2f));
            }
            else if (Mathf.Abs(angleToTarget) > 45f)
            {
                 moveInput = 0.3f; // slow down for sharp turns
            }

            carAnimator.turnInput = turnInput;
            carAnimator.moveInput = moveInput;
        }
        else
        {
            carAnimator.turnInput = 0f;
            carAnimator.moveInput = 0f;
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}
