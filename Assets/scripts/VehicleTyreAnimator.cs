using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine.AI;

public class VehicleTyreAnimator : MonoBehaviour
{
    [Header("Wheel Settings")]
    public float wheelRadius = 0.4f;
    public Vector3 rotationAxis = new Vector3(1, 0, 0); 

    [Header("Steering Settings")]
    public float maxSteerAngle = 35f;
    public float steeringSmoothness = 10f;
    public float turnMultiplier = 15f; 

    [Header("AI Traffic Detection")]
    public float brakingDistance = 10f;
    public float sensorHeightOffset = 0.5f;
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
    
    [Header("AI Free Roam (NavMesh)")]
    public float wanderRadius = 60f;
    public float driveSpeed = 8f; // Custom speed for hybrid movement

    private NavMeshAgent agent;
    private SplineAnimate splineAnimate;
    
    private class WheelData
    {
        public Transform transform;
        public Quaternion initialRotation;
        public bool isFront;
    }
    private List<WheelData> wheels = new List<WheelData>();

    // Lighting
    private List<Material> brakeLightMaterials = new List<Material>();
    private Color brightBrakeColor = new Color(1f, 0f, 0f) * 4f; 
    private Color dimBrakeColor = new Color(0.2f, 0f, 0f);       

    private Vector3 lastPosition;
    private float lastYaw;
    private float currentSteeringAngle;
    private float currentRollAngle;
    private bool obstacleDetected = false;
    private int roadWaypoint;
    private int travelDirection;

    void Start()
    {
        splineAnimate = GetComponent<SplineAnimate>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            // A single NavMeshAgent owns vehicle motion and facing. This gives
            // reciprocal avoidance a consistent velocity for other traffic.
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.radius = 1f;
            agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 3f);
            agent.speed = driveSpeed;
            SetNewForwardDestination();
        }

        // Initialize Wheels & Lights
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            string lowerName = child.name.ToLower();
            
            if (lowerName.Contains("tire") || lowerName.Contains("tyre") || lowerName.Contains("wheel") || lowerName.Contains("rim"))
            {
                WheelData wd = new WheelData();
                wd.transform = child;
                wd.initialRotation = child.localRotation;
                wd.isFront = child.localPosition.z > 0.1f;
                wheels.Add(wd);
            }
            
            if (lowerName.Contains("chrome_08") || (lowerName.Contains("headlight") && !lowerName.Contains("glass")))
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.8f) * 3f); 
                }
            }
            
            if (lowerName.Contains("r_lights_02") || lowerName.Contains("brake"))
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", dimBrakeColor);
                    brakeLightMaterials.Add(r.material);
                }
            }
        }

        lastPosition = transform.position;
        lastYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleTrafficDetection();
        HandleFreeRoamAI();
        HandleCustomMovement();
        HandleWheels();
    }

    private void HandleTrafficDetection()
    {
        Vector3 sensorStart = transform.position + (transform.up * sensorHeightOffset);
        obstacleDetected = false;
        RaycastHit hit;
        
        // We use a wider BoxCast (3m wide total) so it detects obstacles on corners
        if (Physics.BoxCast(sensorStart, new Vector3(1.5f, 0.2f, 0.1f), transform.forward, out hit, transform.rotation, brakingDistance, obstacleLayers))
        {
            if (!hit.collider.transform.IsChildOf(this.transform) && !hit.collider.name.ToLower().Contains("road"))
            {
                obstacleDetected = true;
                Debug.DrawLine(sensorStart, hit.point, Color.red);
            }
        }

        if (splineAnimate != null)
        {
            if (obstacleDetected && splineAnimate.IsPlaying) splineAnimate.Pause();
            else if (!obstacleDetected && !splineAnimate.IsPlaying) splineAnimate.Play();
        }

        // Pausing only the visual spline left the NavMeshAgent moving into the
        // vehicle in front. Stop the actual driver as well.
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = obstacleDetected;
        }

        // Brake Light Logic
        bool isBraking = obstacleDetected || (agent != null && Vector3.Distance(transform.position, agent.destination) < 2f);
        foreach (Material mat in brakeLightMaterials)
        {
            mat.SetColor("_EmissionColor", isBraking ? brightBrakeColor : dimBrakeColor);
        }
    }

    private void HandleFreeRoamAI()
    {
        if (agent == null) return;

        // No longer forcing agent.nextPosition because agent.updatePosition is true.
        // It keeps the car on the NavMesh and respects obstacle avoidance.

        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance))
        {
            SetNewForwardDestination();
        }
    }

    private void HandleCustomMovement()
    {
        if (agent == null) return;
        if (obstacleDetected) return; // Brakes applied!

        Vector3 targetPoint = agent.steeringTarget;
        Vector3 dirToTarget = (targetPoint - transform.position).normalized;

        // --- MICRO-AVOIDANCE (Curb Repulsion) ---
        // Fire rays diagonally left and right to detect curbs. If we get too close, push the steering target away!
        Vector3 sensorStart = transform.position + (transform.up * sensorHeightOffset);
        RaycastHit sideHit;
        
        // Check Left
        if (Physics.Raycast(sensorStart, transform.forward - transform.right * 0.8f, out sideHit, 4f, obstacleLayers))
        {
            if (!sideHit.collider.transform.IsChildOf(this.transform) && !sideHit.collider.name.ToLower().Contains("road"))
            {
                dirToTarget += transform.right * 1.5f; // Push hard right
                Debug.DrawLine(sensorStart, sideHit.point, Color.magenta);
            }
        }
        // Check Right
        if (Physics.Raycast(sensorStart, transform.forward + transform.right * 0.8f, out sideHit, 4f, obstacleLayers))
        {
            if (!sideHit.collider.transform.IsChildOf(this.transform) && !sideHit.collider.name.ToLower().Contains("road"))
            {
                dirToTarget -= transform.right * 1.5f; // Push hard left
                Debug.DrawLine(sensorStart, sideHit.point, Color.magenta);
            }
        }

        // Normalize the adjusted direction
        dirToTarget.Normalize();

        // NavMeshAgent rotates and translates the vehicle. Avoiding direct
        // Transform rotation here prevents a sideways-facing mesh while the
        // agent is calculating traffic avoidance.
        agent.speed = Mathf.MoveTowards(agent.speed, driveSpeed, Time.deltaTime * 2f);
    }

    private void SetNewForwardDestination()
    {
        ProceduralRoadTrafficRoute.SetNextDestination(agent, transform, ref roadWaypoint, ref travelDirection);
    }

    private void HandleWheels()
    {
        Vector3 movement = transform.position - lastPosition;
        float distance = movement.magnitude;
        
        if (distance > 0.0001f)
        {
            float direction = Mathf.Sign(Vector3.Dot(transform.forward, movement.normalized));
            float rollDelta = (distance / wheelRadius) * Mathf.Rad2Deg * direction;
            currentRollAngle += rollDelta;
        }

        float currentYaw = transform.eulerAngles.y;
        float deltaYaw = Mathf.DeltaAngle(lastYaw, currentYaw);
        
        // Hybrid movement smoothly rotates the car body, so deltaYaw is small. 
        // We multiply it to get a realistic visual steer angle.
        float targetSteer = Mathf.Clamp(deltaYaw * turnMultiplier * 3f, -maxSteerAngle, maxSteerAngle);

        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteer, Time.deltaTime * steeringSmoothness);

        foreach (var w in wheels)
        {
            if (w.transform == null) continue;
            w.transform.localRotation = w.initialRotation;

            if (w.isFront)
            {
                w.transform.Rotate(this.transform.up, currentSteeringAngle, Space.World);
            }
            w.transform.Rotate(rotationAxis, currentRollAngle, Space.Self);
        }

        lastPosition = transform.position;
        lastYaw = currentYaw;
    }
}
