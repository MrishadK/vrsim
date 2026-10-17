using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a rising barrier gate. Automatically opens when a player approaches.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BarrierGateController : MonoBehaviour
{
    public Transform gateArm;
    public float openAngle = 90f; 
    public float closedAngle = 0f;
    public float speed = 3f;
    
    private bool isOpen = false;
    private Coroutine moveCoroutine;

    private void Start()
    {
        // Ensure trigger is set
        GetComponent<BoxCollider>().isTrigger = true;
    }

    public void OpenGate()
    {
        if (isOpen) return;
        isOpen = true;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveGate(openAngle));
    }

    public void CloseGate()
    {
        if (!isOpen) return;
        isOpen = false;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveGate(closedAngle));
    }

    private IEnumerator MoveGate(float targetAngle)
    {
        if (gateArm == null) yield break;
        
        float startAngle = gateArm.localEulerAngles.z;
        if (startAngle > 180) startAngle -= 360; 
        
        float current = startAngle;
        while (Mathf.Abs(current - targetAngle) > 0.1f)
        {
            current = Mathf.Lerp(current, targetAngle, Time.deltaTime * speed);
            gateArm.localRotation = Quaternion.Euler(0, 0, current);
            yield return null;
        }
        gateArm.localRotation = Quaternion.Euler(0, 0, targetAngle);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Simple check for VR player or vehicles
        if (other.CompareTag("Player") || other.GetComponentInParent<CharacterController>() != null || other.GetComponent<CharacterController>() != null || other.GetComponent<Rigidbody>() != null)
        {
            OpenGate();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<CharacterController>() != null || other.GetComponent<CharacterController>() != null || other.GetComponent<Rigidbody>() != null)
        {
            CloseGate();
        }
    }
}
