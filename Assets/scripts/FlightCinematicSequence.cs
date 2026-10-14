using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlightCinematicSequence : MonoBehaviour
{
    [Header("Player & Plane")]
    public Transform xrOrigin; // Drag your main VR Player rig here
    public Transform airplane; // The main airplane object
    public Transform seatTarget; // The empty object inside the plane where they sit

    [Header("Flight Path Targets")]
    public Transform pushbackPoint;
    public Transform runwayStartPoint;
    public Transform skyEndPoint;

    [Header("Speeds")]
    public float taxiSpeed = 5f;
    public float takeoffSpeed = 50f;

    [Header("Fade UI")]
    public CanvasGroup fadeCanvasGroup; // A black UI panel covering the screen

    private bool sequenceStarted = false;

    // This triggers when the player steps into your Box Collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !sequenceStarted)
        {
            sequenceStarted = true;
            StartCoroutine(PlayFlightSequence());
        }
    }

    // The Master Timeline
    IEnumerator PlayFlightSequence()
    {
        // 1. FADE TO BLACK (Takes 1 second)
        float fadeTime = 0;
        while (fadeTime < 1f)
        {
            fadeTime += Time.deltaTime;
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = fadeTime;
            yield return null;
        }

        // 2. TELEPORT TO SEAT & LOCK TO PLANE
        xrOrigin.position = seatTarget.position;
        xrOrigin.rotation = seatTarget.rotation;
        xrOrigin.SetParent(airplane); // This makes the player move WITH the plane!

        // 3. FADE BACK IN (Takes 1 second)
        fadeTime = 1f;
        while (fadeTime > 0f)
        {
            fadeTime -= Time.deltaTime;
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = fadeTime;
            yield return null;
        }

        // Wait a moment for the player to look around the cabin
        yield return new WaitForSeconds(3f);

        // 4. PUSHBACK PHASE
        yield return StartCoroutine(MovePlane(pushbackPoint, taxiSpeed));
        
        yield return new WaitForSeconds(1f); // Pause before taxi

        // 5. TAXI TO RUNWAY PHASE
        yield return StartCoroutine(MovePlane(runwayStartPoint, taxiSpeed));

        yield return new WaitForSeconds(2f); // Waiting for clearance...

        // 6. TAKEOFF PHASE
        yield return StartCoroutine(MovePlane(skyEndPoint, takeoffSpeed));

        // 7. STOP (End of simulation)
        Debug.Log("Flight Sequence Complete!");
    }

    // A helper function that smoothly moves the plane to a target
    IEnumerator MovePlane(Transform target, float speed)
    {
        // Keep moving until we are really close to the target
        while (Vector3.Distance(airplane.position, target.position) > 0.5f)
        {
            // Smoothly look at the target (banking/turning)
            Vector3 direction = target.position - airplane.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            airplane.rotation = Quaternion.Slerp(airplane.rotation, targetRotation, Time.deltaTime * 2f);

            // Move forward
            airplane.position = Vector3.MoveTowards(airplane.position, target.position, speed * Time.deltaTime);
            
            // Wait until the next frame before looping
            yield return null; 
        }
    }
}