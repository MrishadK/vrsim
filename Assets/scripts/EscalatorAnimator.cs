using UnityEngine;
using System.Collections.Generic;

public class EscalatorAnimator : MonoBehaviour
{
    public GameObject stepPrefab;
    public float speed = 0.5f; 
    public bool moveUp = true;
    
    public Vector3[] pathPoints;
    
    private List<Transform> activeSteps = new List<Transform>();
    private float[] stepProgresses;

    void Start()
    {
        if (pathPoints == null || pathPoints.Length < 2 || stepPrefab == null)
            return;

        float totalLength = 0f;
        for(int i = 0; i < pathPoints.Length; i++) {
            Vector3 p1 = pathPoints[i];
            Vector3 p2 = pathPoints[(i + 1) % pathPoints.Length];
            totalLength += Vector3.Distance(p1, p2);
        }

        int numSteps = pathPoints.Length; 
        stepProgresses = new float[numSteps];
        
        for (int i = 0; i < numSteps; i++) {
            GameObject stepObj = Instantiate(stepPrefab, transform);
            activeSteps.Add(stepObj.transform);
            stepProgresses[i] = (totalLength / numSteps) * i;
        }
    }

    void Update()
    {
        if (pathPoints == null || pathPoints.Length < 2 || activeSteps.Count == 0) return;

        float totalLength = 0f;
        float[] segments = new float[pathPoints.Length];
        for(int i = 0; i < pathPoints.Length; i++) {
            Vector3 p1 = pathPoints[i];
            Vector3 p2 = pathPoints[(i + 1) % pathPoints.Length];
            float dist = Vector3.Distance(p1, p2);
            segments[i] = dist;
            totalLength += dist;
        }

        float delta = speed * Time.deltaTime;
        if (!moveUp) delta = -delta;

        for (int i = 0; i < activeSteps.Count; i++) {
            stepProgresses[i] += delta;
            
            if (stepProgresses[i] < 0) stepProgresses[i] += totalLength;
            if (stepProgresses[i] > totalLength) stepProgresses[i] -= totalLength;

            float currentDist = stepProgresses[i];
            for (int j = 0; j < pathPoints.Length; j++) {
                if (currentDist <= segments[j] || j == pathPoints.Length - 1) {
                    float t = currentDist / segments[j];
                    Vector3 p1 = pathPoints[j];
                    Vector3 p2 = pathPoints[(j + 1) % pathPoints.Length];
                    activeSteps[i].position = Vector3.Lerp(p1, p2, t);
                    activeSteps[i].rotation = transform.rotation; // Inherit escalator's world rotation
                    break;
                }
                currentDist -= segments[j];
            }
        }
    }
}