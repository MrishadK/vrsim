using System.Collections;
using UnityEngine;
using UnityEngine.Splines; // Required for Unity Splines
using UnityEngine.XR.Interaction.Toolkit; // Required for VR Control Lock

public class FlightSequenceManager : MonoBehaviour
{
    [Header("VR Player Rig Setup")]
    [Tooltip("Drag your XR Origin (Player) here.")]
    public Transform xrOrigin;
    
    [Tooltip("Drag the seat pivot inside the cockpit/cabin here.")]
    public Transform playerSeatTarget;
    
    [Tooltip("Drag your locomotion script (e.g., Action Based Continuous Move Provider) here.")]
    public ActionBasedContinuousMoveProvider vrMovementProvider;

    [Header("Airplane Setup")]
    [Tooltip("Drag your main Airplane object here.")]
    public Transform airplane;
    
    [Tooltip("Drag the Spline Animate component attached to your airplane here.")]
    public SplineAnimate planeSplineAnimator;

    [Header("Chained Flight Paths")]
    [Tooltip("Element 0 = Pushback Spline | Element 1 = Taxiway Spline")]
    public SplineContainer[] flightPaths;

    [Header("Cinematics & Timing")]
    [Tooltip("Drag your UI Blackout Canvas Group here.")]
    public CanvasGroup blackoutScreen;
    
    [Tooltip("How long the screen stays pitch black during transitions.")]
    public float blackoutDuration = 2f;
    
    [Tooltip("How long the plane pauses at the terminal gate before starting pushback.")]
    public float initialDelay = 3f;

    private bool isSequenceStarted = false;

    private void Start()
    {
        if (xrOrigin == null)
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null) xrOrigin = origin.transform;
            else if (Camera.main != null) xrOrigin = Camera.main.transform.root;
        }

        if (vrMovementProvider == null && xrOrigin != null)
        {
            vrMovementProvider = xrOrigin.GetComponentInChildren<ActionBasedContinuousMoveProvider>();
        }

        if (airplane == null)
        {
            var anim = FindObjectOfType<SplineAnimate>();
            if (anim != null)
            {
                airplane = anim.transform;
                if (planeSplineAnimator == null) planeSplineAnimator = anim;
            }
        }
        else if (planeSplineAnimator == null)
        {
            planeSplineAnimator = airplane.GetComponentInChildren<SplineAnimate>();
        }
    }

    // --- 1. TRIGGER DETECTION ---
    private void OnTriggerEnter(Collider other)
    {
        // Ensures only the VR player triggers the timeline once
        bool isPlayer = other.CompareTag("Player") || other.CompareTag("MainCamera")
                     || other.GetComponentInParent<CharacterController>() != null
                     || other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null;

        if (isPlayer && !isSequenceStarted)
        {
            isSequenceStarted = true;
            StartCoroutine(ExecuteFlightTimeline());
        }
    }

    // --- 2. MASTER SEQUENTIAL TIMELINE ---
    IEnumerator ExecuteFlightTimeline()
    {
        // Ensure our array isn't empty before proceeding
        if (flightPaths == null || flightPaths.Length == 0)
        {
            Debug.LogWarning("Flight Paths array is empty in FlightSequenceManager. Please assign splines if available.", this);
        }

        // STAGE 1: Lock Player Controls & Board the Plane
        if (vrMovementProvider != null) 
            vrMovementProvider.enabled = false;

        // Smoothly glide the player from the trigger zone directly into the window seat
        if (xrOrigin != null && playerSeatTarget != null)
        {
            while (Vector3.Distance(xrOrigin.position, playerSeatTarget.position) > 0.05f)
            {
                xrOrigin.position = Vector3.MoveTowards(xrOrigin.position, playerSeatTarget.position, 3.5f * Time.deltaTime);
                yield return null;
            }

            // Perfectly snap position/rotation and lock the player structure as a child of the airplane
            xrOrigin.position = playerSeatTarget.position;
            xrOrigin.rotation = playerSeatTarget.rotation;
            if (airplane != null)
                xrOrigin.SetParent(airplane);
        }

        // STAGE 2: Pre-Flight Blackout
        if (blackoutScreen != null)
        {
            blackoutScreen.alpha = 1f;
            yield return new WaitForSeconds(blackoutDuration);
            blackoutScreen.alpha = 0f;
        }

        // Wait a few seconds at the gate for immersion
        yield return new WaitForSeconds(initialDelay);

        // STAGE 3: Process Splines in Ordered Series
        for (int i = 0; i < flightPaths.Length; i++)
        {
            if (flightPaths[i] == null)
            {
                Debug.LogWarning($"Spline at index {i} is missing! Skipping to next phase.", this);
                continue;
            }

            // Dynamically assign the current track to the engine
            planeSplineAnimator.Container = flightPaths[i];
            
            // Force tracking metrics back to zero point
            planeSplineAnimator.NormalizedTime = 0f;
            
            // Fire the native spline playback sequence
            planeSplineAnimator.Play();

            // Hold code progression execution right here until track processing completes fully
            while (planeSplineAnimator.NormalizedTime < 1f)
            {
                // Safety check: if it stops playing for any native reason, break out
                if (!planeSplineAnimator.IsPlaying && planeSplineAnimator.NormalizedTime > 0.98f)
                    break;

                yield return null; 
            }

            // Step 4: Handle Intermittent Transitions (e.g., Reached tarmac corner, lining up)
            if (i < flightPaths.Length - 1)
            {
                // Optional: Perform a quick blackout or ground clearance pause between Pushback and Taxiway phases
                yield return new WaitForSeconds(2f); 
            }
        }

        Debug.Log("Sequential Flight Path Sequence Completed Successfully.");
    }
}