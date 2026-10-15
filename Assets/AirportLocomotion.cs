using System;
using UnityEngine;
using UnityEngine.XR;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// AeroSim VR — High-Fidelity Airport Player Locomotion Controller.
/// Provides authentic human walking physics with physical traction, responsive acceleration/braking,
/// stride cadence tracking, footstep event dispatch, and input blending for OpenXR VR controllers and desktop.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class AirportLocomotion : MonoBehaviour
{
    [Header("Comfort & Speeds")]
    [Tooltip("Natural brisk walking speed (m/s).")]
    [Min(0.5f)] public float walkSpeed = 1.45f;
    [Tooltip("Airport rush sprint speed (m/s).")]
    [Min(1.0f)] public float sprintSpeed = 3.3f;
    [Tooltip("Traction acceleration rate (m/s²).")]
    [Min(1f)] public float acceleration = 6.5f;
    [Tooltip("Firm braking deceleration rate (m/s²) when releasing input.")]
    [Min(1f)] public float brakingDeceleration = 9.0f;
    [Tooltip("Controller turn speed (degrees/sec).")]
    [Min(10f)] public float turnSpeed = 80f;
    [Range(0f, 1f)] public float deadZone = 0.15f;
    public bool controlsLocked = true;

    [Header("Gait & Stride Timing")]
    [Tooltip("Average steps per second during walk.")]
    public float walkStrideRate = 1.85f;
    [Tooltip("Average steps per second during sprint.")]
    public float sprintStrideRate = 2.8f;

    [Header("Subtle VR Camera Bob")]
    [Tooltip("Enable subtle, VR-comfortable camera gait bobbing.")]
    public bool enableCameraBob = true;
    public float cameraBobAmount = 0.008f;

    // Public State Accessors for Body Avatar & Hand Motion
    public float CurrentSpeed => currentSpeed;
    public float NormalizedSpeed => currentSpeed / (walkSpeed > 0.01f ? walkSpeed : 1.45f);
    public Vector2 MoveInput => lastMoveInput;
    public bool IsMoving => currentSpeed > 0.05f && lastMoveInput.sqrMagnitude > 0.001f;
    public bool IsSprinting => isSprintingActive;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public float StepCycle => stepCycle;

    // Footstep event: (isLeftFoot, intensity 0-1)
    public event Action<bool, float> OnFootstep;

    private CharacterController characterController;
    private Transform headset;
    private Transform cameraOffsetTransform;
    private float verticalVelocity;
    private float currentSpeed;
    private Vector2 lastMoveInput;
    private bool isSprintingActive;

    private float stepCycle;
    private bool isLeftStep;
    private bool hasTriggeredHalfStep;

    private Vector3 initialCameraOffsetPos;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Humanoid CharacterController calibration
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 45f;
            characterController.skinWidth = 0.03f;
            characterController.minMoveDistance = 0f;
        }

        var xrOrigin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            if (xrOrigin.Camera != null) headset = xrOrigin.Camera.transform;
            if (xrOrigin.CameraFloorOffsetObject != null)
            {
                cameraOffsetTransform = xrOrigin.CameraFloorOffsetObject.transform;
                initialCameraOffsetPos = cameraOffsetTransform.localPosition;
            }
        }

        if (headset == null)
            headset = Camera.main != null ? Camera.main.transform : transform;

        // Ensure Footstep Audio Engine is attached
        var footsteps = GetComponent<PlayerFootstepAudio>();
        if (footsteps == null)
        {
            footsteps = gameObject.AddComponent<PlayerFootstepAudio>();
        }
        footsteps.BindLocomotion(this);
    }

    public void SetLocked(bool value)
    {
        controlsLocked = value;
        verticalVelocity = 0f;
        currentSpeed = 0f;
        lastMoveInput = Vector2.zero;
        isSprintingActive = false;
    }

    private void Update()
    {
        if (controlsLocked || characterController == null || headset == null)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * brakingDeceleration);
            return;
        }

        var moveInput = ReadThumbstick(XRNode.LeftHand);
        var turnInput = ReadThumbstick(XRNode.RightHand);

        bool isSprinting = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
            if (Keyboard.current.eKey.isPressed) turnInput.x += 1f;
            if (Keyboard.current.qKey.isPressed) turnInput.x -= 1f;

            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                isSprinting = true;
        }
#endif

        // VR sprint detection (push thumbstick far forward)
        if (moveInput.y > 0.88f)
        {
            isSprinting = true;
        }

        if (moveInput.sqrMagnitude < deadZone * deadZone)
            moveInput = Vector2.zero;
        else
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        lastMoveInput = moveInput;
        isSprintingActive = isSprinting;

        // Smooth rotation
        if (Mathf.Abs(turnInput.x) >= deadZone)
        {
            transform.Rotate(Vector3.up, turnInput.x * turnSpeed * Time.deltaTime, Space.World);
        }

        var forward = Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
        var right = Vector3.ProjectOnPlane(headset.right, Vector3.up).normalized;

        float currentMaxSpeed = isSprinting ? sprintSpeed : walkSpeed;
        float targetSpeed = moveInput.magnitude > 0.01f ? currentMaxSpeed * moveInput.magnitude : 0f;

        // Responsive acceleration vs solid braking friction
        float accelRate = targetSpeed > currentSpeed ? acceleration : brakingDeceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * accelRate);

        var horizontal = (forward * moveInput.y + right * moveInput.x).normalized * currentSpeed;

        // Gravity & Grounding
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2.0f;
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

        characterController.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

        // Update Gait & Stride Cadence
        UpdateStrideCadence();

        // Subtle Camera Bobbing
        UpdateCameraBob();
    }

    private void UpdateStrideCadence()
    {
        if (currentSpeed > 0.05f && characterController.isGrounded)
        {
            float rate = isSprintingActive ? sprintStrideRate : walkStrideRate;
            float speedRatio = Mathf.Clamp(currentSpeed / (walkSpeed > 0.01f ? walkSpeed : 1.45f), 0.2f, 2.2f);

            stepCycle += speedRatio * rate * Time.deltaTime * Mathf.PI;

            if (stepCycle >= Mathf.PI * 2f)
            {
                stepCycle -= Mathf.PI * 2f;
                hasTriggeredHalfStep = false;
                isLeftStep = false;
                OnFootstep?.Invoke(isLeftStep, isSprintingActive ? 1.0f : 0.65f);
            }
            else if (!hasTriggeredHalfStep && stepCycle >= Mathf.PI)
            {
                hasTriggeredHalfStep = true;
                isLeftStep = true;
                OnFootstep?.Invoke(isLeftStep, isSprintingActive ? 1.0f : 0.65f);
            }
        }
        else
        {
            hasTriggeredHalfStep = false;
        }
    }

    private void UpdateCameraBob()
    {
        if (!enableCameraBob || cameraOffsetTransform == null) return;

        // Respect VR Comfort setting
        bool comfort = PlayerPrefs.GetInt("VR_Comfort", 1) == 1;
        float bobScale = comfort ? 0.35f : 1.0f;

        if (currentSpeed > 0.1f && characterController.isGrounded)
        {
            float bobY = Mathf.Sin(stepCycle * 2f) * cameraBobAmount * bobScale * (currentSpeed / walkSpeed);
            cameraOffsetTransform.localPosition = initialCameraOffsetPos + new Vector3(0f, bobY, 0f);
        }
        else
        {
            cameraOffsetTransform.localPosition = Vector3.Lerp(cameraOffsetTransform.localPosition, initialCameraOffsetPos, Time.deltaTime * 6f);
        }
    }

    private static Vector2 ReadThumbstick(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        return device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 axis) ? axis : Vector2.zero;
    }
}
