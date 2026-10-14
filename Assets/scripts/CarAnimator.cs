using UnityEngine;

/// <summary>
/// Drives the Passport's movement and visual wheel/body animation.
/// It automatically finds the named wheel pivots in the Honda Passport FBX,
/// so it also works on pre-existing scene instances with empty references.
/// </summary>
public class CarAnimator : MonoBehaviour
{
    [Header("Wheel Transforms")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Header("Body Transform")]
    public Transform carBody;

    [Header("Driving Settings")]
    [Min(0f)] public float maxSpeed = 15f;
    [Min(0f)] public float acceleration = 5f;
    [Min(0f)] public float deceleration = 10f;
    [Min(0f)] public float turnSpeed = 50f;

    [Header("Animation Settings")]
    [Range(0f, 60f)] public float maxSteeringAngle = 30f;
    [Min(0.01f)] public float wheelRadius = 0.4f;

    [Header("Suspension Feel")]
    [Range(0f, 10f)] public float maxPitch = 2f;
    [Range(0f, 10f)] public float maxRoll = 3f;
    [Min(0.01f)] public float suspensionLerpSpeed = 5f;

    [Header("Control Settings")]
    public bool isPlayerControlled = true;
    [HideInInspector] public float moveInput;
    [HideInInspector] public float turnInput;

    private float currentSpeed;
    private float steeringAngle;
    private float wheelSpin;
    private float currentPitch;
    private float currentRoll;

    private Quaternion frontLeftBaseRotation;
    private Quaternion frontRightBaseRotation;
    private Quaternion rearLeftBaseRotation;
    private Quaternion rearRightBaseRotation;
    private Quaternion bodyBaseRotation;

    private void Awake()
    {
        // The traffic Passport uses VehicleTyreAnimator for its wheel visuals.
        // Do not let a second animator overwrite those wheel rotations.
        if (!isPlayerControlled && GetComponent<VehicleTyreAnimator>() != null)
        {
            enabled = false;
            return;
        }

        ResolveModelParts();
        CacheRestPose();
    }

    private void Reset()
    {
        ResolveModelParts();
        CacheRestPose();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveModelParts();
            CacheRestPose();
        }
    }

    private void Update()
    {
        if (isPlayerControlled)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                moveInput = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                turnInput = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            }
#else
            moveInput = Input.GetAxisRaw("Vertical");
            turnInput = Input.GetAxisRaw("Horizontal");
#endif
            UpdateDriving(Time.deltaTime);
        }
        else
        {
            // AI scripts can supply normalized input values. Clamp to avoid visual overspeed.
            currentSpeed = Mathf.Clamp(moveInput, -1f, 1f) * maxSpeed;
        }

        UpdateVisuals(Time.deltaTime);
    }

    private void UpdateDriving(float deltaTime)
    {
        float targetSpeed = moveInput * (moveInput < 0f ? maxSpeed * 0.5f : maxSpeed);
        float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * deltaTime);

        transform.Translate(Vector3.forward * currentSpeed * deltaTime, Space.Self);

        float speedFactor = maxSpeed > 0f ? Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed) : 0f;
        float direction = currentSpeed < 0f ? -1f : 1f;
        transform.Rotate(0f, turnInput * turnSpeed * speedFactor * direction * deltaTime, 0f, Space.Self);
    }

    private void UpdateVisuals(float deltaTime)
    {
        if (wheelRadius <= 0f)
            return;

        steeringAngle = Mathf.Lerp(steeringAngle, turnInput * maxSteeringAngle, 12f * deltaTime);
        wheelSpin = Mathf.Repeat(wheelSpin + currentSpeed / (2f * Mathf.PI * wheelRadius) * 360f * deltaTime, 360f);

        // The Passport FBX wheel pivots use local X for rolling and local Y for steering.
        SetWheelRotation(frontLeftWheel, frontLeftBaseRotation, steeringAngle);
        SetWheelRotation(frontRightWheel, frontRightBaseRotation, steeringAngle);
        SetWheelRotation(rearLeftWheel, rearLeftBaseRotation, 0f);
        SetWheelRotation(rearRightWheel, rearRightBaseRotation, 0f);

        if (carBody != null)
        {
            float targetPitch = -moveInput * maxPitch;
            float speedFactor = maxSpeed > 0f ? Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed) : 0f;
            float targetRoll = -turnInput * speedFactor * maxRoll;
            float lerp = 1f - Mathf.Exp(-suspensionLerpSpeed * deltaTime);

            currentPitch = Mathf.Lerp(currentPitch, targetPitch, lerp);
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, lerp);
            carBody.localRotation = bodyBaseRotation * Quaternion.Euler(currentPitch, 0f, currentRoll);
        }
    }

    private void SetWheelRotation(Transform wheel, Quaternion baseRotation, float steer)
    {
        if (wheel != null)
            wheel.localRotation = baseRotation * Quaternion.Euler(wheelSpin, steer, 0f);
    }

    private void ResolveModelParts()
    {
        frontLeftWheel ??= FindDescendant("Wheel_FL");
        frontRightWheel ??= FindDescendant("Wheel_FR");
        rearLeftWheel ??= FindDescendant("Wheel_RL");
        rearRightWheel ??= FindDescendant("Wheel_RR");
        carBody ??= FindDescendant("Body");
    }

    private Transform FindDescendant(string partName)
    {
        foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == partName)
                return candidate;
        }

        return null;
    }

    private void CacheRestPose()
    {
        if (frontLeftWheel != null) frontLeftBaseRotation = frontLeftWheel.localRotation;
        if (frontRightWheel != null) frontRightBaseRotation = frontRightWheel.localRotation;
        if (rearLeftWheel != null) rearLeftBaseRotation = rearLeftWheel.localRotation;
        if (rearRightWheel != null) rearRightBaseRotation = rearRightWheel.localRotation;
        if (carBody != null) bodyBaseRotation = carBody.localRotation;
    }
}
