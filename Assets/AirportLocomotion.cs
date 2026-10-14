using UnityEngine;
using UnityEngine.XR;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lightweight, Quest-ready locomotion for the airport experience. It uses the
/// standard OpenXR thumbstick feature usages, so it does not depend on a scene
/// specific Input Action reference.
/// </summary>
[DisallowMultipleComponent]
public sealed class AirportLocomotion : MonoBehaviour
{
    [Header("Comfort")]
    [Min(0.1f)] public float walkSpeed = 1.0f;
    [Min(0.1f)] public float sprintSpeed = 2.5f;
    [Min(0.1f)] public float acceleration = 2f;
    [Min(0f)] public float turnSpeed = 70f;
    [Range(0f, 1f)] public float deadZone = 0.2f;
    public bool controlsLocked = true;

    private CharacterController characterController;
    private Transform headset;
    private float verticalVelocity;
    private float currentSpeed;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        var xrOrigin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
        headset = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera.transform : Camera.main != null ? Camera.main.transform : transform;
    }

    public void SetLocked(bool value)
    {
        controlsLocked = value;
        verticalVelocity = 0f;
    }

    private void Update()
    {
        if (controlsLocked || characterController == null || headset == null)
            return;

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

        if (moveInput.sqrMagnitude < deadZone * deadZone)
            moveInput = Vector2.zero;
        else
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (Mathf.Abs(turnInput.x) >= deadZone)
            transform.Rotate(Vector3.up, turnInput.x * turnSpeed * Time.deltaTime, Space.World);

        var forward = Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
        var right = Vector3.ProjectOnPlane(headset.right, Vector3.up).normalized;
        
        float currentMaxSpeed = isSprinting ? sprintSpeed : walkSpeed;
        float targetSpeed = moveInput.magnitude > 0 ? currentMaxSpeed : 0f;
        
        // Smooth out the acceleration/deceleration so it's not immediate
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);
        
        var horizontal = (forward * moveInput.y + right * moveInput.x).normalized * currentSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1.5f;
        else
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

        characterController.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private static Vector2 ReadThumbstick(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        return device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 axis) ? axis : Vector2.zero;
    }
}
