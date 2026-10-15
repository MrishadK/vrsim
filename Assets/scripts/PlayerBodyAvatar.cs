using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// AeroSim VR — Animated Humanoid Player Body Avatar System.
/// Spawns and synchronizes an authentic 3D passenger character model with real-time
/// 2D blend-tree walking, strafing, sprinting, and idle animations.
/// 
/// In first-person / VR, the player can look down to see their own animated chest,
/// legs, jeans, and sneakers taking real footsteps, and their full body casts dynamic
/// shadows onto the terminal floor without camera face-clipping.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBodyAvatar : MonoBehaviour
{
    [Header("Model & Animation References")]
    [Tooltip("Humanoid passenger model prefab or FBX asset.")]
    public GameObject characterModelAsset;

    [Tooltip("Animator Controller with 2D directional locomotion blend tree.")]
    public RuntimeAnimatorController animatorController;

    [Header("First-Person Alignment")]
    [Tooltip("Maximum angle (degrees) the head can turn before the torso rotates.")]
    public float maxNeckYawAngle = 45f;
    [Tooltip("How fast the torso catches up with the view direction.")]
    public float torsoTurnSpeed = 5.0f;
    [Tooltip("Smooth position tracking speed to floor projection.")]
    public float positionFollowSpeed = 25f;

    [Header("First-Person Mesh Visibility")]
    [Tooltip("Hide the player's own head mesh so it never clips into camera view while preserving shadow casting.")]
    public bool hideHeadInFirstPerson = true;

    private GameObject avatarInstance;
    private Animator animator;
    private Transform headsetCamera;
    private AirportLocomotion locomotion;
    private Transform headBone;

    private float currentBodyYaw;

    private void Awake()
    {
        locomotion = GetComponent<AirportLocomotion>() ?? GetComponentInParent<AirportLocomotion>();
    }

    private void Start()
    {
        InitializeAvatar();
    }

    public void InitializeAvatar()
    {
        // 1. Resolve camera
        if (headsetCamera == null)
        {
            var origin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null && origin.Camera != null)
                headsetCamera = origin.Camera.transform;
            else if (Camera.main != null)
                headsetCamera = Camera.main.transform;
        }

        // 2. Load default assets if not assigned
#if UNITY_EDITOR
        if (characterModelAsset == null)
        {
            characterModelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/uploads_files_4323274_Survival_Character.fbx");

            if (characterModelAsset == null)
            {
                characterModelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx");
            }
        }

        if (animatorController == null)
        {
            animatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/Player_Locomotion.controller");
        }
#endif

        // 3. Spawn Avatar instance if not already present
        if (avatarInstance == null && characterModelAsset != null)
        {
            avatarInstance = Instantiate(characterModelAsset, transform);
            avatarInstance.name = "Player_Body_Avatar";

            // Remove any physics colliders on the avatar to prevent self-collision with CharacterController
            foreach (var col in avatarInstance.GetComponentsInChildren<Collider>(true))
            {
                Destroy(col);
            }

            // Setup Animator
            animator = avatarInstance.GetComponent<Animator>();
            if (animator == null)
                animator = avatarInstance.AddComponent<Animator>();

            if (animatorController != null)
                animator.runtimeAnimatorController = animatorController;

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // Ensures shadows & feet animate even when looking up

            // Configure shadow casting and first-person head visibility
            ConfigureRenderers();

            // Find head bone for clean camera near-clip safety
            FindHeadBone();
        }

        if (headsetCamera != null)
        {
            currentBodyYaw = headsetCamera.eulerAngles.y;
            if (avatarInstance != null)
                avatarInstance.transform.rotation = Quaternion.Euler(0f, currentBodyYaw, 0f);
        }
    }

    private void ConfigureRenderers()
    {
        if (avatarInstance == null) return;

        foreach (var smr in avatarInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            // Cast shadows onto the airport floor
            smr.shadowCastingMode = ShadowCastingMode.TwoSided;
            smr.receiveShadows = true;
            smr.updateWhenOffscreen = true;

            string n = smr.name.ToLower();
            if (hideHeadInFirstPerson && (n.Contains("head") || n.Contains("hair") || n.Contains("eye") || n.Contains("mouth") || n.Contains("leash")))
            {
                // Shadow-only for head components in first-person so they don't clip through camera view
                smr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private void FindHeadBone()
    {
        if (avatarInstance == null) return;

        if (animator != null && animator.isHuman)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        if (headBone == null)
        {
            foreach (var t in avatarInstance.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLower();
                if (n == "head" || n == "b-head" || n.Contains("head"))
                {
                    headBone = t;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (avatarInstance == null || locomotion == null) return;

        // Drive Animator parameters
        if (animator != null)
        {
            float normalizedSpeed = locomotion.NormalizedSpeed;
            Vector2 moveInput = locomotion.MoveInput;
            bool isMoving = locomotion.IsMoving;
            bool isSprinting = locomotion.IsSprinting;

            animator.SetFloat("Speed", normalizedSpeed);
            animator.SetFloat("DirectionX", moveInput.x);
            animator.SetFloat("DirectionZ", moveInput.y);
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsSprinting", isSprinting);

            // Scale animation playback rate proportionally with physical ground speed
            float walkRefSpeed = locomotion.walkSpeed > 0.1f ? locomotion.walkSpeed : 1.4f;
            float animSpeed = Mathf.Max(0.7f, locomotion.CurrentSpeed / walkRefSpeed);
            animator.SetFloat("AnimSpeed", animSpeed);
        }
    }

    private void LateUpdate()
    {
        if (avatarInstance == null || headsetCamera == null) return;

        // 1. Grounded position alignment: Place avatar root directly beneath camera on floor
        Vector3 camPos = headsetCamera.position;
        Vector3 targetFloorPos = new Vector3(camPos.x, transform.position.y, camPos.z);
        avatarInstance.transform.position = Vector3.Lerp(avatarInstance.transform.position, targetFloorPos, Time.deltaTime * positionFollowSpeed);

        // 2. Realistic torso/body orientation
        float cameraYaw = headsetCamera.eulerAngles.y;

        if (locomotion != null && locomotion.IsMoving && locomotion.CurrentSpeed > 0.1f)
        {
            // When walking/running: Body rotates smoothly towards motion direction or forward gaze
            Vector2 input = locomotion.MoveInput;
            float inputAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            float moveWorldYaw = cameraYaw + inputAngle;

            currentBodyYaw = Mathf.MoveTowardsAngle(currentBodyYaw, moveWorldYaw, Time.deltaTime * 360f);
        }
        else
        {
            // When stationary: Torso stays steady until head turns past comfortable neck angle
            float angleDiff = Mathf.DeltaAngle(currentBodyYaw, cameraYaw);
            if (Mathf.Abs(angleDiff) > maxNeckYawAngle)
            {
                float targetYaw = cameraYaw - Mathf.Sign(angleDiff) * maxNeckYawAngle;
                currentBodyYaw = Mathf.LerpAngle(currentBodyYaw, targetYaw, Time.deltaTime * torsoTurnSpeed);
            }
        }

        avatarInstance.transform.rotation = Quaternion.Euler(0f, currentBodyYaw, 0f);

        // 3. Prevent head clipping in first-person camera while preserving full body shadow
        if (hideHeadInFirstPerson && headBone != null)
        {
            // Scale head bone to 0 for the camera
            headBone.localScale = Vector3.zero;
        }
    }
}
