using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

/// <summary>
/// AeroSim VR — POV First-Person Hands Manager for XR Origin (XR Rig).
/// Spawns, aligns, and animates a realistic pair of POV hands attached to the VR controllers.
/// Uses ProceduralHandAnimator for real-time bone-driven finger animation.
/// </summary>
[DisallowMultipleComponent]
public class VRPovHandsManager : MonoBehaviour
{
    [Header("Hand Model References")]
    [Tooltip("3D Arms / Hands model asset (e.g. Assets/trolley/uploads_files_2864994_arms.fbx).")]
    [SerializeField] private GameObject handModelAsset;

    [Header("Controller Anchors (Auto-detected if null)")]
    [SerializeField] private Transform leftControllerAnchor;
    [SerializeField] private Transform rightControllerAnchor;

    [Header("POV Alignment Offsets")]
    [Tooltip("Position offset relative to controller pivot.")]
    [SerializeField] private Vector3 leftHandPositionOffset = new Vector3(-0.02f, -0.04f, -0.06f);
    [SerializeField] private Vector3 rightHandPositionOffset = new Vector3(0.02f, -0.04f, -0.06f);
    
    [Tooltip("Rotation offset relative to controller pivot.")]
    [SerializeField] private Vector3 leftHandRotationOffset = new Vector3(-15f, 0f, 0f);
    [SerializeField] private Vector3 rightHandRotationOffset = new Vector3(-15f, 0f, 0f);
    
    [Tooltip("Uniform scale factor for natural POV proportions.")]
    [SerializeField] private float handScale = 1.0f;

    [Header("Mirror Left Hand")]
    [Tooltip("Mirror the hand model on X axis for the left hand (required when using a single right-hand model for both sides).")]
    [SerializeField] private bool mirrorLeftHand = true;

    [Header("Input Actions (Optional override)")]
    [SerializeField] private InputActionProperty leftTriggerAction;
    [SerializeField] private InputActionProperty leftGripAction;
    [SerializeField] private InputActionProperty rightTriggerAction;
    [SerializeField] private InputActionProperty rightGripAction;

    private GameObject leftHandInstance;
    private GameObject rightHandInstance;

    private void Start()
    {
        InitializePOVHands();
    }

    public void InitializePOVHands()
    {
        // 1. Auto-detect controller anchors if not assigned
        if (leftControllerAnchor == null || rightControllerAnchor == null)
        {
            FindControllerAnchors();
        }

        // 2. Load default hand model asset if not assigned
        if (handModelAsset == null)
        {
#if UNITY_EDITOR
            handModelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/trolley/uploads_files_2864994_arms.fbx");
#endif
        }

        // 3. Create default input actions if none assigned
        EnsureDefaultInputActions();

        // 4. Spawn & Attach Right Hand (original model orientation)
        if (rightControllerAnchor != null && rightHandInstance == null)
        {
            rightHandInstance = AttachHandModel(
                rightControllerAnchor, "POV_Right_Hand",
                rightHandPositionOffset, rightHandRotationOffset,
                new Vector3(handScale, handScale, handScale),
                rightTriggerAction, rightGripAction);
        }

        // 5. Spawn & Attach Left Hand (mirrored on X axis)
        if (leftControllerAnchor != null && leftHandInstance == null)
        {
            Vector3 leftScale = mirrorLeftHand
                ? new Vector3(-handScale, handScale, handScale) // Mirror X for left hand
                : new Vector3(handScale, handScale, handScale);

            leftHandInstance = AttachHandModel(
                leftControllerAnchor, "POV_Left_Hand",
                leftHandPositionOffset, leftHandRotationOffset,
                leftScale,
                leftTriggerAction, leftGripAction);
        }
    }

    /// <summary>
    /// Creates default XR Input System action bindings for trigger and grip
    /// using the standard OpenXR interaction profile paths.
    /// </summary>
    private void EnsureDefaultInputActions()
    {
        if (leftTriggerAction.action == null || string.IsNullOrEmpty(leftTriggerAction.action.name))
        {
            var action = new InputAction("LeftTrigger", InputActionType.Value, "<XRController>{LeftHand}/trigger");
            leftTriggerAction = new InputActionProperty(action);
        }

        if (leftGripAction.action == null || string.IsNullOrEmpty(leftGripAction.action.name))
        {
            var action = new InputAction("LeftGrip", InputActionType.Value, "<XRController>{LeftHand}/grip");
            leftGripAction = new InputActionProperty(action);
        }

        if (rightTriggerAction.action == null || string.IsNullOrEmpty(rightTriggerAction.action.name))
        {
            var action = new InputAction("RightTrigger", InputActionType.Value, "<XRController>{RightHand}/trigger");
            rightTriggerAction = new InputActionProperty(action);
        }

        if (rightGripAction.action == null || string.IsNullOrEmpty(rightGripAction.action.name))
        {
            var action = new InputAction("RightGrip", InputActionType.Value, "<XRController>{RightHand}/grip");
            rightGripAction = new InputActionProperty(action);
        }
    }

    private void FindControllerAnchors()
    {
        var origin = GetComponent<XROrigin>();
        if (origin != null && origin.CameraFloorOffsetObject != null)
        {
            var offset = origin.CameraFloorOffsetObject.transform;
            foreach (Transform child in offset)
            {
                string n = child.name.ToLower();
                if (leftControllerAnchor == null && (n.Contains("left") && (n.Contains("controller") || n.Contains("hand"))))
                {
                    leftControllerAnchor = child;
                }
                else if (rightControllerAnchor == null && (n.Contains("right") && (n.Contains("controller") || n.Contains("hand"))))
                {
                    rightControllerAnchor = child;
                }
            }
        }

        // Fallback recursive search
        if (leftControllerAnchor == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Left Controller" || t.name == "LeftHand Controller")
                {
                    leftControllerAnchor = t;
                    break;
                }
            }
        }

        if (rightControllerAnchor == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Right Controller" || t.name == "RightHand Controller")
                {
                    rightControllerAnchor = t;
                    break;
                }
            }
        }
    }

    private GameObject AttachHandModel(Transform anchor, string name, Vector3 posOffset, Vector3 rotOffset, Vector3 scale, InputActionProperty triggerAct, InputActionProperty gripAct)
    {
        // Check if hand already exists
        Transform existing = anchor.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject handObj;
        if (handModelAsset != null)
        {
            handObj = Instantiate(handModelAsset, anchor);
        }
        else
        {
            // Fallback procedural hand representation if FBX missing
            handObj = CreateProceduralHand(anchor);
        }

        handObj.name = name;
        handObj.transform.localPosition = posOffset;
        handObj.transform.localRotation = Quaternion.Euler(rotOffset);
        handObj.transform.localScale = scale;

        // Remove any physics colliders that might interfere with player locomotion
        foreach (var col in handObj.GetComponentsInChildren<Collider>(true))
        {
            Destroy(col);
        }

        // Remove any existing Animator component that has no controller
        // (prevents empty Animator from overriding our procedural bone rotations)
        var existingAnimator = handObj.GetComponent<Animator>();
        if (existingAnimator != null && existingAnimator.runtimeAnimatorController == null)
        {
            Destroy(existingAnimator);
        }

        // Fix SkinnedMeshRenderer for negative scale (mirrored left hand) and apply realistic skin material
        Texture2D skinTexture = null;
#if UNITY_EDITOR
        skinTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/trolley/textures/T_arms_A.png");
#endif

        foreach (var smr in handObj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (scale.x < 0f)
            {
                // Flip triangle winding so normals face outward when mirrored
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                // Some renderers need updateWhenOffscreen for negative scale to work
                smr.updateWhenOffscreen = true;
            }

            // Apply realistic skin material properties
            if (smr.material != null)
            {
                if (skinTexture != null)
                {
                    smr.material.mainTexture = skinTexture;
                    // For URP/HDRP or Standard shader Base Map
                    smr.material.SetTexture("_BaseMap", skinTexture); 
                }
                
                // Override the green tint with a natural skin tone
                smr.material.color = new Color(0.85f, 0.72f, 0.62f, 1.0f);
                if (smr.material.HasProperty("_BaseColor"))
                {
                    smr.material.SetColor("_BaseColor", new Color(0.85f, 0.72f, 0.62f, 1.0f));
                }
                
                // Reduce smoothness for skin
                if (smr.material.HasProperty("_Smoothness"))
                {
                    smr.material.SetFloat("_Smoothness", 0.15f);
                }
                else if (smr.material.HasProperty("_Glossiness"))
                {
                    smr.material.SetFloat("_Glossiness", 0.15f);
                }
            }
        }

        foreach (var mr in handObj.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (scale.x < 0f)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }
        }

        // Add ProceduralHandAnimator for real-time bone-driven finger animation
        var proceduralAnimator = handObj.GetComponent<ProceduralHandAnimator>();
        if (proceduralAnimator == null)
            proceduralAnimator = handObj.AddComponent<ProceduralHandAnimator>();

        proceduralAnimator.triggerAction = triggerAct;
        proceduralAnimator.gripAction = gripAct;

        return handObj;
    }

    /// <summary>
    /// Creates a simple procedural hand shape when no FBX model is available.
    /// Uses capsules to represent palm and fingers.
    /// </summary>
    private GameObject CreateProceduralHand(Transform parent)
    {
        var hand = new GameObject("ProceduralHand");
        hand.transform.SetParent(parent, false);

        // Palm
        var palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        palm.name = "Palm";
        palm.transform.SetParent(hand.transform, false);
        palm.transform.localScale = new Vector3(0.08f, 0.025f, 0.10f);
        palm.transform.localPosition = new Vector3(0f, 0f, 0.04f);
        var palmCol = palm.GetComponent<Collider>();
        if (palmCol != null) Destroy(palmCol);

        // Set palm color to skin-like tone
        var palmRenderer = palm.GetComponent<Renderer>();
        if (palmRenderer != null)
            palmRenderer.material.color = new Color(0.87f, 0.72f, 0.60f, 1f);

        // Create 5 finger stubs
        string[] fingerNames = { "Thumb1", "Index1", "Middle1", "Ring1", "Pinky1" };
        Vector3[] fingerPositions = {
            new Vector3(-0.035f, 0f, 0.02f),  // thumb
            new Vector3(-0.025f, 0f, 0.10f),  // index
            new Vector3(-0.008f, 0f, 0.105f), // middle
            new Vector3(0.010f, 0f, 0.10f),   // ring
            new Vector3(0.028f, 0f, 0.09f),   // pinky
        };
        float[] fingerLengths = { 0.035f, 0.04f, 0.045f, 0.04f, 0.03f };

        for (int i = 0; i < 5; i++)
        {
            var finger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            finger.name = fingerNames[i];
            finger.transform.SetParent(hand.transform, false);
            finger.transform.localPosition = fingerPositions[i];
            finger.transform.localScale = new Vector3(0.015f, fingerLengths[i], 0.015f);
            finger.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var fCol = finger.GetComponent<Collider>();
            if (fCol != null) Destroy(fCol);

            var fRenderer = finger.GetComponent<Renderer>();
            if (fRenderer != null)
                fRenderer.material.color = new Color(0.87f, 0.72f, 0.60f, 1f);
        }

        return hand;
    }
}
