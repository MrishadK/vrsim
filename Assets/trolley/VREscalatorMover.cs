using UnityEngine;

public class VREscalatorMover : MonoBehaviour
{
    [Header("Escalator Path")]
    [Tooltip("Drag an empty object placed at the BOTTOM of the escalator here")]
    public Transform bottomPoint;
    [Tooltip("Drag an empty object placed at the TOP of the escalator here")]
    public Transform topPoint;
    
    [Header("Settings")]
    public float speed = 1.5f;

    private CharacterController playerController;
    private Vector3 movementVector;

    void Start()
    {
        // Calculate the exact angle and direction from the bottom to the top
        if (bottomPoint != null && topPoint != null)
        {
            movementVector = (topPoint.position - bottomPoint.position).normalized;
        }
        else
        {
            Debug.LogWarning("Escalator is missing Top or Bottom points!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Unity's VR XR Origin uses a CharacterController for its physical body.
        // We check if the thing that just stepped on the escalator has one.
        CharacterController cc = other.GetComponent<CharacterController>() ?? other.GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            playerController = cc;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // If the player is on the escalator, smoothly carry them up the vector
        if (playerController != null)
        {
            playerController.Move(movementVector * speed * Time.deltaTime);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // When the player steps off at the top, let them walk normally again
        if (playerController != null && other.gameObject == playerController.gameObject)
        {
            playerController = null;
        }
    }
}