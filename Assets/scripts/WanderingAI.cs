using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class WanderingAI : MonoBehaviour
{
    public float wanderRadius = 20f;
    public float waitTimeMin = 2f;
    public float waitTimeMax = 6f;

    private NavMeshAgent agent;
    private Animator animator;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Start wandering
        StartCoroutine(WanderRoutine());
    }

    void Update()
    {
        if (agent != null && animator != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
        }
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !isWaiting)
                {
                    // Reached destination, start waiting/analyzing
                    isWaiting = true;
                    float waitTime = Random.Range(waitTimeMin, waitTimeMax);
                    yield return new WaitForSeconds(waitTime);

                    // Pick new destination
                    Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                    agent.SetDestination(newPos);
                    isWaiting = false;
                }
            }
            else
            {
                // If not on navmesh, don't spam errors
                yield return new WaitForSeconds(1f);
            }
            yield return null;
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, layermask))
        {
            return navHit.position;
        }
        return origin;
    }
}
