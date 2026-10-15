using UnityEngine;

/// <summary>
/// AeroSim VR — Dynamic First-Person Arm & Hand Locomotion Motion.
/// Eliminates rigid, static hands by applying realistic gait bobbing, alternating arm swing,
/// angular turning inertia, sprint pumping, and organic idle breathing.
/// Works cleanly in tandem with ProceduralHandAnimator (which drives finger curling).
/// </summary>
[DisallowMultipleComponent]
public class PlayerHandLocomotionMotion : MonoBehaviour
{
    [Header("Hand Identity")]
    public bool isLeftHand = false;

    [Header("Locomotion Bobbing & Swing")]
    [Tooltip("Vertical bobbing amplitude when walking.")]
    public float walkBobAmount = 0.015f;
    [Tooltip("Lateral sway amplitude when walking.")]
    public float walkSwayAmount = 0.012f;
    [Tooltip("Arm swing angle in degrees when walking.")]
    public float walkSwingAngle = 3.5f;

    [Header("Sprint Motion")]
    public float sprintBobMultiplier = 1.6f;
    public float sprintSwayMultiplier = 1.4f;
    public float sprintSwingMultiplier = 2.2f;
    public Vector3 sprintOffset = new Vector3(0f, 0.02f, -0.03f);

    [Header("Inertia & Turning Sway")]
    public float turningSwayMultiplier = 0.04f;
    public float maxTurningSway = 0.05f;
    public float swayReturnSpeed = 10f;

    [Header("Organic Idle Breathing")]
    public float idleBreathingAmount = 0.003f;
    public float idleBreathingSpeed = 1.6f;

    private AirportLocomotion locomotion;
    private Transform mainCamera;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    private Vector3 currentBobOffset;
    private Quaternion currentBobRotation;

    private Vector3 turningInertiaOffset;
    private float lastCameraYaw;
    private float lastCameraPitch;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    private void Start()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (locomotion == null)
        {
            locomotion = GetComponentInParent<AirportLocomotion>() ?? Object.FindAnyObjectByType<AirportLocomotion>();
        }

        if (mainCamera == null)
        {
            if (Camera.main != null) mainCamera = Camera.main.transform;
            else
            {
                var cam = GetComponentInParent<Camera>();
                if (cam != null) mainCamera = cam.transform;
            }
        }

        if (mainCamera != null)
        {
            lastCameraYaw = mainCamera.eulerAngles.y;
            lastCameraPitch = mainCamera.eulerAngles.x;
        }
    }

    public void SetInitialOffsets(Vector3 pos, Vector3 rotEuler)
    {
        initialLocalPos = pos;
        initialLocalRot = Quaternion.Euler(rotEuler);
    }

    private void LateUpdate()
    {
        if (locomotion == null || mainCamera == null)
        {
            FindReferences();
            if (locomotion == null || mainCamera == null) return;
        }

        // 1. Calculate camera angular velocity for inertia sway
        float currentYaw = mainCamera.eulerAngles.y;
        float currentPitch = mainCamera.eulerAngles.x;

        float yawDelta = Mathf.DeltaAngle(lastCameraYaw, currentYaw);
        float pitchDelta = Mathf.DeltaAngle(lastCameraPitch, currentPitch);

        lastCameraYaw = currentYaw;
        lastCameraPitch = currentPitch;

        // Inertia lags behind head rotation
        Vector3 targetInertia = new Vector3(
            Mathf.Clamp(-yawDelta * turningSwayMultiplier, -maxTurningSway, maxTurningSway),
            Mathf.Clamp(pitchDelta * turningSwayMultiplier * 0.7f, -maxTurningSway, maxTurningSway),
            0f
        );

        turningInertiaOffset = Vector3.Lerp(turningInertiaOffset, targetInertia, Time.deltaTime * 8f);
        targetInertia = Vector3.Lerp(turningInertiaOffset, Vector3.zero, Time.deltaTime * swayReturnSpeed);
        turningInertiaOffset = targetInertia;

        // 2. Locomotion Gait Motion
        float speedNormalized = locomotion.NormalizedSpeed;
        bool isMoving = locomotion.IsMoving;
        bool isSprinting = locomotion.IsSprinting;

        Vector3 targetBobPos = Vector3.zero;
        Quaternion targetBobRot = Quaternion.identity;

        if (isMoving && speedNormalized > 0.05f)
        {
            float stepPhase = locomotion.StepCycle;
            // Left arm is in antiphase (180 deg offset) relative to right arm
            float armPhase = isLeftHand ? stepPhase + Mathf.PI : stepPhase;

            float bobMult = isSprinting ? sprintBobMultiplier : 1.0f;
            float swayMult = isSprinting ? sprintSwayMultiplier : 1.0f;
            float swingMult = isSprinting ? sprintSwingMultiplier : 1.0f;

            // Vertical bobbing: 2 cycles per stride (dips on each footstep)
            float verticalBob = Mathf.Sin(stepPhase * 2f) * walkBobAmount * bobMult * speedNormalized;

            // Lateral and forward arm swing: 1 cycle per stride
            float lateralSway = Mathf.Cos(armPhase) * walkSwayAmount * swayMult * speedNormalized;
            float swingPitch = Mathf.Sin(armPhase) * walkSwingAngle * swingMult * speedNormalized;
            float swingRoll = Mathf.Cos(armPhase) * (walkSwingAngle * 0.5f) * speedNormalized;

            Vector3 locomotionOffset = new Vector3(
                (isLeftHand ? -1f : 1f) * Mathf.Abs(lateralSway * 0.5f) + lateralSway,
                verticalBob,
                Mathf.Sin(armPhase) * 0.02f * speedNormalized
            );

            if (isSprinting)
            {
                locomotionOffset += sprintOffset;
            }

            targetBobPos = locomotionOffset;
            targetBobRot = Quaternion.Euler(swingPitch, 0f, isLeftHand ? -swingRoll : swingRoll);
        }
        else
        {
            // Organic idle breathing motion
            float breath = Mathf.Sin(Time.time * idleBreathingSpeed) * idleBreathingAmount;
            targetBobPos = new Vector3(0f, breath, breath * 0.5f);
            targetBobRot = Quaternion.Euler(breath * 10f, 0f, 0f);
        }

        // 3. Smooth blend & apply
        currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobPos + turningInertiaOffset, Time.deltaTime * 12f);
        currentBobRotation = Quaternion.Slerp(currentBobRotation, targetBobRot, Time.deltaTime * 12f);

        transform.localPosition = initialLocalPos + currentBobOffset;
        transform.localRotation = initialLocalRot * currentBobRotation;
    }
}
