using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Definitive Airport Passenger Flow State Machine.
/// Every passenger follows a strictly ONE-WAY realistic progression:
/// Curbside -> Entrance -> Check-in & Bag Drop -> Security & Arch -> Immigration -> Lounge -> Gate A12 -> Jetway -> Aircraft.
/// Passengers NEVER walk backwards from inside to outside.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class AirportNPC : MonoBehaviour
{
    public enum AirportStage
    {
        ArrivingOutside,    // 0: Curbside drop-off / sidewalk
        EnteringTerminal,   // 1: Walking through the terminal front glass doors
        QueueingCheckIn,    // 2: Waiting in line at the check-in desk
        AtCheckIn,          // 3: Presenting passport & dropping checked luggage onto belt
        QueueingSecurity,   // 4: Waiting in line at the security screening
        PassingSecurity,    // 5: Walking through metal detector arch
        QueueingImmigration,// 6: Waiting in line at passport control booth
        AtImmigration,      // 7: Getting passport inspected & stamped
        WaitingAtLounge,    // 8: Sitting on chairs or browsing in departure area
        QueueingGate,       // 9: In line at Gate A12 boarding desk
        BoardingGate,       // 10: Scanning boarding pass at Gate A12
        WalkingJetway,      // 11: Walking through jetway bridge into plane
        BoardedAirplane     // 12: Seated inside cabin / completed flight boarding
    }

    [Header("Current State")]
    [SerializeField] private AirportStage currentStage = AirportStage.ArrivingOutside;

    [Header("Movement & Steering")]
    public float walkSpeed = 1.25f;
    public float rotationSpeed = 6f;
    public float stopDistance = 0.4f;

    [Header("Timing Variety")]
    public float minCounterWait = 3f;
    public float maxCounterWait = 7f;
    public float wanderRadius = 15f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 6f;

    private NavMeshAgent agent;
    private Animator animator;
    private ProceduralSit proceduralSit;
    private QueueStation currentQueue;
    private Transform assignedSeat;

    private float currentSpeedParam;
    private float currentTurnParam;
    private bool isExecutingStationAction;
    private bool isSitting;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int TurnParam = Animator.StringToHash("Turn");
    private static readonly int InteractParam = Animator.StringToHash("Interact");
    private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");

    public AirportStage CurrentStage => currentStage;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        animator = GetComponentInChildren<Animator>();
        proceduralSit = GetComponent<ProceduralSit>();

        if (agent != null)
        {
            agent.speed = walkSpeed + Random.Range(-0.15f, 0.2f);
            agent.stoppingDistance = stopDistance;
            agent.radius = 0.3f;
            agent.height = 1.75f;
            agent.acceleration = 8f;
            agent.angularSpeed = 180f;
            agent.avoidancePriority = Random.Range(30, 70);
            agent.updateRotation = false; // We handle rotation manually for smooth humanoid turning
        }
    }

    private void Start()
    {
        StartCoroutine(PassengerLifecycleRoutine());
    }

    private void Update()
    {
        UpdateAnimationAndRotation();
    }

    private void UpdateAnimationAndRotation()
    {
        if (animator == null || agent == null || !agent.isActiveAndEnabled) return;

        float targetSpeed = agent.velocity.magnitude;
        currentSpeedParam = Mathf.Lerp(currentSpeedParam, targetSpeed, Time.deltaTime * 6f);
        animator.SetFloat(SpeedParam, currentSpeedParam);

        Vector3 desiredVel = agent.desiredVelocity;
        if (desiredVel.sqrMagnitude > 0.05f)
        {
            desiredVel.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(desiredVel.normalized);
            float angle = Vector3.SignedAngle(transform.forward, desiredVel.normalized, Vector3.up);
            float targetTurn = Mathf.Clamp(angle / 45f, -1f, 1f);
            currentTurnParam = Mathf.Lerp(currentTurnParam, targetTurn, Time.deltaTime * 10f);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        else
        {
            currentTurnParam = Mathf.Lerp(currentTurnParam, 0f, Time.deltaTime * 10f);
        }

        animator.SetFloat(TurnParam, currentTurnParam);
        animator.SetBool(IsWalkingParam, currentSpeedParam > 0.1f);
    }

    /// <summary>
    /// Called by QueueStation whenever the passenger in front steps forward.
    /// </summary>
    public void NotifyQueueAdvanced()
    {
        if (currentQueue != null && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            Vector3 newPos = currentQueue.GetQueuePosition(this);
            if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    /// <summary>
    /// Master lifecycle routine: strictly one-way forward progression.
    /// </summary>
    private IEnumerator PassengerLifecycleRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 0.8f));

        while (currentStage != AirportStage.BoardedAirplane)
        {
            Transform destination = GetStageDestination();

            if (destination != null && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                currentQueue = destination.GetComponent<QueueStation>();

                if (currentQueue != null)
                {
                    currentQueue.JoinQueue(this);
                    Vector3 queuePos = currentQueue.GetQueuePosition(this);
                    if (NavMesh.SamplePosition(queuePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);
                    else
                        agent.SetDestination(destination.position);

                    // Wait until we reach our current spot in the queue
                    while (true)
                    {
                        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) break;
                        if (!agent.pathPending && agent.remainingDistance <= stopDistance)
                        {
                            // If we're not first in line, wait and face the station
                            if (!currentQueue.IsFirstInLine(this))
                            {
                                Vector3 lookTarget = destination.position;
                                lookTarget.y = transform.position.y;
                                if ((lookTarget - transform.position).sqrMagnitude > 0.01f)
                                {
                                    Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position);
                                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 4f);
                                }
                                yield return null;
                                continue;
                            }

                            // We ARE first in line!
                            break;
                        }
                        yield return null;
                    }
                }
                else
                {
                    // Direct navigation to point
                    Vector3 targetPos = destination.position;
                    if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);
                    else
                        agent.SetDestination(targetPos);

                    while (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && (agent.pathPending || agent.remainingDistance > stopDistance))
                    {
                        yield return null;
                    }
                }

                // Align smoothly with the station's orientation
                float alignTimer = 0f;
                while (alignTimer < 0.6f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, destination.rotation, Time.deltaTime * rotationSpeed);
                    alignTimer += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }

            // Perform specific station behavior
            yield return StartCoroutine(ExecuteStationBehavior());

            // Leave any queue before moving to next stage
            if (currentQueue != null)
            {
                currentQueue.LeaveQueue(this);
                currentQueue = null;
            }

            AdvanceToNextStage();
        }

        // Final completion: Passenger is inside aircraft
        yield return StartCoroutine(HandleAircraftArrival());
    }

    private IEnumerator ExecuteStationBehavior()
    {
        isExecutingStationAction = true;
        float waitDuration = Random.Range(minCounterWait, maxCounterWait);

        switch (currentStage)
        {
            case AirportStage.ArrivingOutside:
                // Just arrived, brief pause before opening doors
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
                break;

            case AirportStage.EnteringTerminal:
                // Stepped through doors, continue walking into checkin hall
                yield return new WaitForSeconds(0.3f);
                break;

            case AirportStage.QueueingCheckIn:
                // Arrived at front of Check-in counter!
                if (animator != null) animator.SetTrigger(InteractParam); // Talk to check-in agent
                yield return new WaitForSeconds(1.5f);

                // Drop baggage onto luggage conveyor belt
                DropLuggageOnBelt();

                yield return new WaitForSeconds(waitDuration);
                break;

            case AirportStage.QueueingSecurity:
                // Front of security: Walk through metal detector arch
                yield return new WaitForSeconds(1.2f);
                break;

            case AirportStage.PassingSecurity:
                // Collecting belongings after metal detector
                yield return new WaitForSeconds(Random.Range(2f, 4f));
                break;

            case AirportStage.QueueingImmigration:
                // At passport control booth
                if (animator != null) animator.SetTrigger(InteractParam); // Present passport
                yield return new WaitForSeconds(waitDuration);
                break;

            case AirportStage.WaitingAtLounge:
                // Relax in departure lounge
                float loungeWait = Random.Range(10f, 20f);
                bool shouldSit = (proceduralSit != null && Random.value > 0.4f);

                if (shouldSit)
                {
                    isSitting = true;
                    if (agent != null) agent.isStopped = true;
                    proceduralSit.enabled = true;
                }

                yield return new WaitForSeconds(loungeWait);

                if (shouldSit)
                {
                    proceduralSit.enabled = false;
                    if (agent != null) agent.isStopped = false;
                    isSitting = false;
                    yield return new WaitForSeconds(0.5f);
                }
                break;

            case AirportStage.QueueingGate:
                // At Gate A12 boarding desk
                if (animator != null) animator.SetTrigger(InteractParam); // Show boarding pass
                yield return new WaitForSeconds(Random.Range(2f, 4f));
                break;

            case AirportStage.BoardingGate:
            case AirportStage.WalkingJetway:
                // Walking down the jetway into the plane
                yield return new WaitForSeconds(0.5f);
                break;
        }

        isExecutingStationAction = false;
    }

    private void DropLuggageOnBelt()
    {
        Transform rightHand = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (rightHand == null) return;

        Transform luggage = rightHand.Find("NPC_Luggage");
        if (luggage != null)
        {
            luggage.SetParent(null);

            // Add physics so conveyor belt handles it
            BoxCollider col = luggage.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = luggage.gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(0.5f, 0.7f, 0.35f);
                col.center = new Vector3(0f, 0.35f, 0f);
            }

            Rigidbody rb = luggage.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = luggage.gameObject.AddComponent<Rigidbody>();
                rb.mass = 20f;
            }

            // Push gently forward onto scale / belt
            rb.isKinematic = false;
            rb.AddForce(transform.forward * 1.5f + Vector3.up * 0.8f, ForceMode.Impulse);

            // Clean up luggage after 30 seconds
            Destroy(luggage.gameObject, 30f);
        }
    }

    private IEnumerator HandleAircraftArrival()
    {
        // Passenger has boarded the aircraft!
        // Never walk outside. They take a seat in the cabin or smoothly despawn.
        if (proceduralSit != null && Random.value > 0.3f)
        {
            // Remain seated inside the aircraft
            if (agent != null) agent.isStopped = true;
            proceduralSit.enabled = true;
            yield return new WaitForSeconds(180f); // Seated for the duration of the flight
        }

        // Fade out or destroy passenger
        if (AirportTrafficManager.Instance != null)
        {
            AirportTrafficManager.Instance.NotifyPassengerBoarded(this);
        }

        Destroy(gameObject, 1f);
    }

    private Transform GetStageDestination()
    {
        var rm = AirportRouteManager.Instance;
        if (rm == null) return null;

        switch (currentStage)
        {
            case AirportStage.ArrivingOutside:
                return rm.GetRandomPoint(rm.entryPoints);

            case AirportStage.EnteringTerminal:
                return rm.GetLeastBusyStation(rm.checkInPoints) ?? rm.GetRandomPoint(rm.checkInPoints);

            case AirportStage.QueueingCheckIn:
                return rm.GetLeastBusyStation(rm.checkInPoints);

            case AirportStage.AtCheckIn:
                return rm.GetLeastBusyStation(rm.securityPoints);

            case AirportStage.QueueingSecurity:
                return rm.GetLeastBusyStation(rm.securityPoints);

            case AirportStage.PassingSecurity:
                return rm.GetLeastBusyStation(rm.immigrationPoints);

            case AirportStage.QueueingImmigration:
                return rm.GetLeastBusyStation(rm.immigrationPoints);

            case AirportStage.AtImmigration:
                return rm.GetRandomPoint(rm.loungePoints) ?? rm.GetRandomPoint(rm.gatePoints);

            case AirportStage.WaitingAtLounge:
                return rm.GetLeastBusyStation(rm.gatePoints) ?? rm.GetRandomPoint(rm.gatePoints);

            case AirportStage.QueueingGate:
                return rm.GetLeastBusyStation(rm.gatePoints);

            case AirportStage.BoardingGate:
                return rm.GetRandomPoint(rm.boardingPoints);

            case AirportStage.WalkingJetway:
                return rm.GetRandomPoint(rm.aircraftSeatPoints) ?? rm.GetRandomPoint(rm.boardingPoints);

            default:
                return null;
        }
    }

    /// <summary>
    /// STRICT ONE-WAY PROGRESSION: Never loops backward to Spawn / Outside!
    /// </summary>
    private void AdvanceToNextStage()
    {
        switch (currentStage)
        {
            case AirportStage.ArrivingOutside:
                currentStage = AirportStage.EnteringTerminal;
                break;
            case AirportStage.EnteringTerminal:
                currentStage = AirportStage.QueueingCheckIn;
                break;
            case AirportStage.QueueingCheckIn:
                currentStage = AirportStage.AtCheckIn;
                break;
            case AirportStage.AtCheckIn:
                currentStage = AirportStage.QueueingSecurity;
                break;
            case AirportStage.QueueingSecurity:
                currentStage = AirportStage.PassingSecurity;
                break;
            case AirportStage.PassingSecurity:
                currentStage = AirportStage.QueueingImmigration;
                break;
            case AirportStage.QueueingImmigration:
                currentStage = AirportStage.AtImmigration;
                break;
            case AirportStage.AtImmigration:
                currentStage = AirportStage.WaitingAtLounge;
                break;
            case AirportStage.WaitingAtLounge:
                currentStage = AirportStage.QueueingGate;
                break;
            case AirportStage.QueueingGate:
                currentStage = AirportStage.BoardingGate;
                break;
            case AirportStage.BoardingGate:
                currentStage = AirportStage.WalkingJetway;
                break;
            case AirportStage.WalkingJetway:
                currentStage = AirportStage.BoardedAirplane;
                break;
            case AirportStage.BoardedAirplane:
                // Terminal stage — do not loop backward!
                break;
        }
    }

    /// <summary>
    /// Set a custom starting stage (used by traffic manager for pre-populating airport).
    /// </summary>
    public void SetStartingStage(AirportStage stage)
    {
        currentStage = stage;
    }
}
