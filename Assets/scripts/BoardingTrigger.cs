using UnityEngine;
using System.Collections;

public class BoardingTrigger : MonoBehaviour
{
    [Header("References")]
    public Transform playerRig;
    public Transform targetSeat;
    public TakeoffSequence takeoffScript;

    private bool triggered = false;

    private void Start()
    {
        if (playerRig == null)
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null) playerRig = origin.transform;
            else if (Camera.main != null) playerRig = Camera.main.transform.root;
        }

        if (takeoffScript == null)
        {
            takeoffScript = FindObjectOfType<TakeoffSequence>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        bool isPlayer = (playerRig != null && (other.transform == playerRig || other.transform.IsChildOf(playerRig)))
                     || other.CompareTag("Player") || other.CompareTag("MainCamera")
                     || other.GetComponentInParent<CharacterController>() != null
                     || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("xr");

        if (isPlayer)
        {
            triggered = true;
            Debug.Log("Player boarded the plane. Triggering sequence...");
            
            // Move player to seat and parent them to the airplane
            if (targetSeat != null)
            {
                playerRig.SetParent(targetSeat);
                playerRig.localPosition = new Vector3(0, 0.8f, 0); // Position eyes slightly above the seat base
                
                // Align them to face out the window (Assuming the window is to the left of the seat)
                playerRig.localRotation = Quaternion.Euler(0, -90, 0); 
            }

            // Disable all movement scripts (XR Locomotion, CharacterControllers, etc)
            var components = playerRig.GetComponentsInChildren<MonoBehaviour>();
            foreach (var comp in components)
            {
                string name = comp.GetType().Name.ToLower();
                if (name.Contains("move") || name.Contains("locomotion") || name.Contains("teleport") || name.Contains("turn") || name.Contains("charactercontroller"))
                {
                    comp.enabled = false;
                }
            }

            StartCoroutine(WalkToSeatAndTakeoff());
        }
    }

    private IEnumerator WalkToSeatAndTakeoff()
    {
        if (targetSeat != null)
        {
            Vector3 startPos = playerRig.position;
            Quaternion startRot = playerRig.rotation;
            
            // Position eyes slightly above the seat base
            Vector3 endPos = targetSeat.position + targetSeat.up * 0.8f; 
            // Align them to face out the window (Assuming the window is to the left of the seat)
            Quaternion endRot = targetSeat.rotation * Quaternion.Euler(0, -90, 0); 
            
            float duration = 4f; // 4 seconds to walk to seat
            float elapsed = 0f;
            
            playerRig.SetParent(targetSeat);
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Smoothstep for natural movement
                t = t * t * (3f - 2f * t);
                
                playerRig.position = Vector3.Lerp(startPos, endPos, t);
                playerRig.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            
            playerRig.position = endPos;
            playerRig.rotation = endRot;
        }

        // Start the takeoff animation
        if (takeoffScript != null)
        {
            takeoffScript.StartTakeoff();
        }
    }
}
