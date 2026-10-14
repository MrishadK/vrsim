using UnityEngine;

/// <summary>
/// Smooth automated sliding glass doors for modern airport entrance/exit gates.
/// </summary>
public class AirportAutomaticDoor : MonoBehaviour
{
    [Header("Door Leaf Transforms")]
    [SerializeField] private Transform leftDoorLeaf;
    [SerializeField] private Transform rightDoorLeaf;

    [Header("Slide Distance & Speed")]
    [SerializeField] private float slideDistance = 2.2f;
    [SerializeField] private float openSpeed = 4f;
    [SerializeField] private float autoCloseDelay = 1.5f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private bool isOpen = false;
    private float lastTriggerTime = -99f;

    private void Awake()
    {
        if (leftDoorLeaf != null)
        {
            leftClosedPos = leftDoorLeaf.localPosition;
            leftOpenPos = leftClosedPos + Vector3.left * slideDistance;
        }

        if (rightDoorLeaf != null)
        {
            rightClosedPos = rightDoorLeaf.localPosition;
            rightOpenPos = rightClosedPos + Vector3.right * slideDistance;
        }
    }

    private void Update()
    {
        if (isOpen && Time.time - lastTriggerTime > autoCloseDelay)
        {
            isOpen = false;
        }

        Vector3 targetLeft = isOpen ? leftOpenPos : leftClosedPos;
        Vector3 targetRight = isOpen ? rightOpenPos : rightClosedPos;

        if (leftDoorLeaf != null)
        {
            leftDoorLeaf.localPosition = Vector3.MoveTowards(leftDoorLeaf.localPosition, targetLeft, Time.deltaTime * openSpeed);
        }

        if (rightDoorLeaf != null)
        {
            rightDoorLeaf.localPosition = Vector3.MoveTowards(rightDoorLeaf.localPosition, targetRight, Time.deltaTime * openSpeed);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        isOpen = true;
        lastTriggerTime = Time.time;
    }
}
