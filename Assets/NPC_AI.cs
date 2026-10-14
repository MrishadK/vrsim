using UnityEngine;
using UnityEngine.AI;

public class TestMove : MonoBehaviour
{
    void Start()
    {
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.destination = new Vector3(0, 0, 5); // simple forward point
        }
    }
}