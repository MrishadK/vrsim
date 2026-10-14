using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AeroSim VR — Procedural bone-driven hand animator.
/// Drives finger bone rotations directly from VR controller trigger/grip inputs
/// without needing AnimationClips or an AnimatorController.
///
/// Attach this to the instantiated hand model GameObject (child of the controller anchor).
/// It auto-discovers finger bones by common naming conventions and curls them in response to input.
/// </summary>
[DisallowMultipleComponent]
public class ProceduralHandAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  PUBLIC CONFIGURATION
    // ─────────────────────────────────────────────

    [Header("VR Controller Inputs")]
    [Tooltip("Index finger trigger input (0–1 float axis).")]
    public InputActionProperty triggerAction;

    [Tooltip("Grip / squeeze input (0–1 float axis).")]
    public InputActionProperty gripAction;

    [Header("Curl Angles (degrees per joint)")]
    [Tooltip("How far each finger joint rotates when fully curled.")]
    [SerializeField] private float indexCurlAngle = 90f;
    [SerializeField] private float middleCurlAngle = 90f;
    [SerializeField] private float ringCurlAngle = 90f;
    [SerializeField] private float pinkyCurlAngle = 85f;
    [SerializeField] private float thumbCurlAngle = 60f;

    [Header("Response")]
    [Tooltip("How fast the fingers respond to input changes.")]
    [SerializeField, Range(1f, 30f)] private float smoothSpeed = 18f;

    [Header("Manual Bone Assignment (optional fallback)")]
    [Tooltip("Leave empty for auto-detection. Assign manually only if auto-detection fails.")]
    public Transform[] thumbBones;
    public Transform[] indexBones;
    public Transform[] middleBones;
    public Transform[] ringBones;
    public Transform[] pinkyBones;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Stores the original local rotation and curl axis for each finger bone.
    /// </summary>
    private struct BoneState
    {
        public Transform bone;
        public Quaternion openRotation;
        public Vector3 curlAxis; // local axis to curl around (usually local X or Z)
        public float maxAngle;
    }

    private List<BoneState> triggerBones = new List<BoneState>(); // index finger
    private List<BoneState> gripBones = new List<BoneState>();    // middle, ring, pinky
    private List<BoneState> thumbBoneStates = new List<BoneState>(); // thumb (mixed)

    private float currentTrigger;
    private float currentGrip;
    private bool initialized;

    // ─────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        EnableAction(triggerAction);
        EnableAction(gripAction);
    }

    private void OnDisable()
    {
        DisableAction(triggerAction);
        DisableAction(gripAction);
    }

    private void Start()
    {
        InitializeBones();
    }

    private void Update()
    {
        if (!initialized) return;

        // Read input values
        float targetTrigger = ReadAction(triggerAction);
        float targetGrip = ReadAction(gripAction);

        // Smooth interpolation for natural feel
        currentTrigger = Mathf.Lerp(currentTrigger, targetTrigger, Time.deltaTime * smoothSpeed);
        currentGrip = Mathf.Lerp(currentGrip, targetGrip, Time.deltaTime * smoothSpeed);

        // Apply curls
        ApplyCurl(triggerBones, currentTrigger);
        ApplyCurl(gripBones, currentGrip);

        // Thumb gets influence from both trigger and grip
        float thumbBlend = Mathf.Max(currentTrigger * 0.5f, currentGrip * 0.8f);
        ApplyCurl(thumbBoneStates, thumbBlend);
    }

    // ─────────────────────────────────────────────
    //  BONE DISCOVERY
    // ─────────────────────────────────────────────

    public void InitializeBones()
    {
        triggerBones.Clear();
        gripBones.Clear();
        thumbBoneStates.Clear();

        // Try manual assignment first
        if (HasManualBones())
        {
            RegisterManualBones();
        }
        else
        {
            AutoDiscoverBones();
        }

        int total = triggerBones.Count + gripBones.Count + thumbBoneStates.Count;
        if (total > 0)
        {
            initialized = true;
            Debug.Log($"[ProceduralHandAnimator] Initialized {total} finger bones on '{gameObject.name}' " +
                      $"(Thumb:{thumbBoneStates.Count} Index:{triggerBones.Count} Grip:{gripBones.Count})");
        }
        else
        {
            Debug.LogWarning($"[ProceduralHandAnimator] No finger bones found on '{gameObject.name}'. " +
                             "Assign bones manually in the Inspector or ensure the model has standard bone names.", this);
        }
    }

    private bool HasManualBones()
    {
        return (thumbBones != null && thumbBones.Length > 0 && thumbBones[0] != null) ||
               (indexBones != null && indexBones.Length > 0 && indexBones[0] != null) ||
               (middleBones != null && middleBones.Length > 0 && middleBones[0] != null);
    }

    private void RegisterManualBones()
    {
        if (indexBones != null)
        {
            foreach (var b in indexBones)
                if (b != null) triggerBones.Add(CreateBoneState(b, indexCurlAngle));
        }

        if (middleBones != null)
        {
            foreach (var b in middleBones)
                if (b != null) gripBones.Add(CreateBoneState(b, middleCurlAngle));
        }

        if (ringBones != null)
        {
            foreach (var b in ringBones)
                if (b != null) gripBones.Add(CreateBoneState(b, ringCurlAngle));
        }

        if (pinkyBones != null)
        {
            foreach (var b in pinkyBones)
                if (b != null) gripBones.Add(CreateBoneState(b, pinkyCurlAngle));
        }

        if (thumbBones != null)
        {
            foreach (var b in thumbBones)
                if (b != null) thumbBoneStates.Add(CreateBoneState(b, thumbCurlAngle));
        }
    }

    /// <summary>
    /// Auto-discovers finger bones using common naming patterns from:
    /// - Mixamo (e.g. mixamorig:LeftHandIndex1, mixamorig:RightHandRing2)
    /// - Unreal/MetaHuman (e.g. index_01_r, middle_02_l)
    /// - Blender (e.g. Index_Finger_1_L, Thumb_1_R)
    /// - Generic (e.g. LeftIndex1, RightMiddle3, L_Index_01)
    /// </summary>
    private void AutoDiscoverBones()
    {
        var allTransforms = GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            string name = t.name.ToLowerInvariant();

            // Skip root-level transforms
            if (t == transform) continue;

            // ── THUMB ──
            if (IsFingerBone(name, "thumb"))
            {
                thumbBoneStates.Add(CreateBoneState(t, thumbCurlAngle));
            }
            // ── INDEX ──
            else if (IsFingerBone(name, "index"))
            {
                triggerBones.Add(CreateBoneState(t, indexCurlAngle));
            }
            // ── MIDDLE ──
            else if (IsFingerBone(name, "middle") || IsFingerBone(name, "mid"))
            {
                gripBones.Add(CreateBoneState(t, middleCurlAngle));
            }
            // ── RING ──
            else if (IsFingerBone(name, "ring"))
            {
                gripBones.Add(CreateBoneState(t, ringCurlAngle));
            }
            // ── PINKY ──
            else if (IsFingerBone(name, "pinky") || IsFingerBone(name, "little"))
            {
                gripBones.Add(CreateBoneState(t, pinkyCurlAngle));
            }
        }
    }

    /// <summary>
    /// Checks if a bone name matches a finger type across multiple naming conventions.
    /// </summary>
    private static bool IsFingerBone(string boneName, string fingerKey)
    {
        // Must contain the finger name
        if (!boneName.Contains(fingerKey)) return false;

        // Must also look like a phalanx bone (has a digit suffix or proximal/distal keyword)
        // This prevents matching root joints that shouldn't be curled
        bool hasBoneIndex = false;
        for (int i = 0; i < boneName.Length; i++)
        {
            if (char.IsDigit(boneName[i]))
            {
                hasBoneIndex = true;
                break;
            }
        }

        if (hasBoneIndex) return true;

        // Also accept names with proximal/intermediate/distal suffixes
        if (boneName.Contains("proximal") || boneName.Contains("intermediate") ||
            boneName.Contains("distal") || boneName.Contains("tip"))
            return true;

        // Accept if it has separator patterns indicating it's a real bone
        if (boneName.Contains("_" + fingerKey) || boneName.Contains(fingerKey + "_") ||
            boneName.EndsWith(fingerKey))
            return true;

        return false;
    }

    // ─────────────────────────────────────────────
    //  BONE STATE CREATION & CURL APPLICATION
    // ─────────────────────────────────────────────

    private BoneState CreateBoneState(Transform bone, float maxAngle)
    {
        // Determine the best axis to curl around based on child bone direction
        Vector3 curlAxis = DetermineCurlAxis(bone);

        return new BoneState
        {
            bone = bone,
            openRotation = bone.localRotation,
            curlAxis = curlAxis,
            maxAngle = maxAngle
        };
    }

    /// <summary>
    /// Determines the best local axis for finger curling.
    /// Uses the direction to the child bone to figure out bone orientation,
    /// then picks a perpendicular axis to curl around.
    /// </summary>
    private Vector3 DetermineCurlAxis(Transform bone)
    {
        // Check if bone has a child (next phalanx) to determine bone direction
        if (bone.childCount > 0)
        {
            Vector3 boneDir = bone.InverseTransformPoint(bone.GetChild(0).position).normalized;

            // The curl axis should be perpendicular to the bone direction
            if (Mathf.Abs(boneDir.y) > 0.7f)
                return Vector3.right;   // bone extends along Y, curl around X
            else if (Mathf.Abs(boneDir.x) > 0.7f)
                return Vector3.forward; // bone extends along X, curl around Z
            else
                return Vector3.right;   // bone extends along Z, curl around X
        }

        // Default: curl around local X axis (most common in Mixamo/generic rigs)
        return Vector3.right;
    }

    private static void ApplyCurl(List<BoneState> bones, float t)
    {
        t = Mathf.Clamp01(t);
        for (int i = 0; i < bones.Count; i++)
        {
            var bs = bones[i];
            if (bs.bone == null) continue;

            // Apply curl as a rotation offset from the open pose
            Quaternion curlRotation = Quaternion.AngleAxis(bs.maxAngle * t, bs.curlAxis);
            bs.bone.localRotation = bs.openRotation * curlRotation;
        }
    }

    // ─────────────────────────────────────────────
    //  INPUT HELPERS
    // ─────────────────────────────────────────────

    private static float ReadAction(InputActionProperty property)
    {
        if (property.action != null && property.action.enabled)
            return property.action.ReadValue<float>();
        return 0f;
    }

    private static void EnableAction(InputActionProperty property)
    {
        if (property.action != null && !property.action.enabled)
            property.action.Enable();
    }

    private static void DisableAction(InputActionProperty property)
    {
        if (property.action != null && property.action.enabled)
            property.action.Disable();
    }
}
