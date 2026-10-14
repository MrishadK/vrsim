using UnityEngine;
using System.Collections;

public class TakeoffSequence : MonoBehaviour
{
    [Header("References")]
    public Transform jetway;
    public Transform frontDoorL;
    public Transform runway;

    [Header("Configuration")]
    public float startDelay = 5f;
    public Vector3 taxiTargetOffset = new Vector3(0, 0, -400f); // Adjust based on runway orientation
    
    public void StartTakeoff()
    {
        StartCoroutine(ExecuteSequence());
    }

    private IEnumerator ExecuteSequence()
    {
        Debug.Log("Takeoff Sequence Initiated. Waiting " + startDelay + " seconds.");
        yield return new WaitForSeconds(startDelay);

        // Phase 1: Retract Bridge
        if (jetway != null)
        {
            Debug.Log("Phase 1: Retracting Jetway...");
            Vector3 startPos = jetway.position;
            Vector3 endPos = startPos + jetway.right * -5f + jetway.forward * -2f; // Pull back and away
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.2f; // 5 seconds
                jetway.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
        }

        // Phase 2: Close Door
        if (frontDoorL != null)
        {
            Debug.Log("Phase 2: Closing Door...");
            Quaternion startRot = frontDoorL.localRotation;
            Quaternion endRot = Quaternion.Euler(0, 0, 0); // Assuming 0,0,0 is closed
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.5f; // 2 seconds
                frontDoorL.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(1f);

        // Phase 3: Pushback
        Debug.Log("Phase 3: Pushback...");
        Vector3 pushbackStart = transform.position;
        Vector3 pushbackEnd = pushbackStart - transform.forward * 40f;
        float pT = 0;
        while (pT < 1f)
        {
            pT += Time.deltaTime * 0.05f; // 20 seconds for pushback
            transform.position = Vector3.Lerp(pushbackStart, pushbackEnd, pT);
            yield return null;
        }

        // Turn 90 degrees left
        Quaternion turnStart = transform.rotation;
        Quaternion turnEnd = turnStart * Quaternion.Euler(0, -90, 0);
        float rT = 0;
        while (rT < 1f)
        {
            rT += Time.deltaTime * 0.1f; // 10 seconds to turn
            transform.rotation = Quaternion.Slerp(turnStart, turnEnd, rT);
            yield return null;
        }

        yield return new WaitForSeconds(2f);

        // Phase 4: Taxi
        Debug.Log("Phase 4: Taxiing to runway...");
        Vector3 runwayStartPos = runway != null ? runway.position + taxiTargetOffset : new Vector3(-218.96f, 3.2f, -100f);
        runwayStartPos.y = transform.position.y; // Keep current height
        
        // Turn to face taxi target
        Quaternion taxiTurn = Quaternion.LookRotation(runwayStartPos - transform.position);
        float trT = 0;
        while (trT < 1f)
        {
            trT += Time.deltaTime * 0.2f;
            transform.rotation = Quaternion.Slerp(transform.rotation, taxiTurn, trT);
            yield return null;
        }

        // Drive to runway
        float dist = Vector3.Distance(transform.position, runwayStartPos);
        float speed = 15f; // Taxi speed
        while (Vector3.Distance(transform.position, runwayStartPos) > 1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, runwayStartPos, speed * Time.deltaTime);
            yield return null;
        }

        // Phase 5: Align on runway
        Debug.Log("Phase 5: Aligning on runway...");
        // Assuming runway is aligned along Z axis.
        Vector3 runwayForward = Vector3.forward; 
        Quaternion alignRot = Quaternion.LookRotation(runwayForward);
        float aT = 0;
        while (aT < 1f)
        {
            aT += Time.deltaTime * 0.2f;
            transform.rotation = Quaternion.Slerp(transform.rotation, alignRot, aT);
            yield return null;
        }

        Debug.Log("Holding short...");
        yield return new WaitForSeconds(3f);

        // Phase 6: Takeoff
        Debug.Log("Phase 6: TAKEOFF!");
        float currentSpeed = 0f;
        float maxTakeoffSpeed = 100f; // Approx 200 knots
        float rotationSpeed = 0f;
        
        while (true) // Fly forever
        {
            currentSpeed = Mathf.Lerp(currentSpeed, maxTakeoffSpeed, Time.deltaTime * 0.1f); // Accelerate
            
            if (currentSpeed > maxTakeoffSpeed * 0.6f) // V1 / Rotate speed
            {
                rotationSpeed = Mathf.Lerp(rotationSpeed, 15f, Time.deltaTime * 0.5f); // Pitch up to 15 degrees
                
                // Add vertical lift
                if (currentSpeed > maxTakeoffSpeed * 0.7f)
                {
                    transform.position += Vector3.up * currentSpeed * 0.2f * Time.deltaTime; // Climb
                }
            }

            // Apply forward movement
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
            
            // Apply pitch rotation
            if (rotationSpeed > 0.1f)
            {
                Quaternion targetPitch = alignRot * Quaternion.Euler(-rotationSpeed, 0, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetPitch, Time.deltaTime);
            }
            
            yield return null;
        }
    }
}
