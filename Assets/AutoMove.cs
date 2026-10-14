using UnityEngine;
using UnityEngine.AI;

public class AutoMoveNav : MonoBehaviour
{
    public Transform[] points;
    private NavMeshAgent agent;
    private int index = 1;
    private bool waiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // VERY IMPORTANT for XR
        agent.updatePosition = false;
        agent.updateRotation = true;

        MoveToNext();
    }

    void Update()
    {
        if (agent == null || points.Length == 0) return;

        // XR camera offset correction
        Vector3 offset = transform.position - Camera.main.transform.position;

        // Move XR Origin manually
        transform.position = agent.nextPosition + offset;

        if (!waiting && agent.remainingDistance < 0.3f)
        {
            waiting = true;
            agent.isStopped = true;

            UIManager.Instance.ShowStep(index);
        }
    }

    public void ContinueMove()
    {
        index++;

        if (index < points.Length)
        {
            MoveToNext();
        }
    }

    void MoveToNext()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        
        waiting = false;
        agent.isStopped = false;
        agent.SetDestination(points[index].position);
    }
}