using UnityEditor;
using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Editor setup tool to attach and calibrate realistic POV hands onto the XR Origin (XR Rig).
/// Run via: Tools → Setup VR POV Hands on XR Rig (Ctrl+Shift+H)
/// </summary>
public static class SetupVRHands
{
    private const string HAND_MODEL_PATH = "Assets/trolley/uploads_files_2864994_arms.fbx";

    [MenuItem("Tools/Setup VR POV Hands on XR Rig #&h", false, 3)]
    public static void Execute()
    {
        // 1. Find XR Origin in scene
        GameObject rig = FindXROrigin();
        if (rig == null)
        {
            Debug.LogError("[SetupVRHands] No 'XR Origin' or 'XR Rig' found in the active scene! Please open your VR game scene (e.g. SampleScene).");
            return;
        }

        // 2. Load the Arms / Hands 3D Model
        GameObject handPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HAND_MODEL_PATH);
        if (handPrefab == null)
        {
            Debug.LogError($"[SetupVRHands] Hand model asset not found at {HAND_MODEL_PATH}");
            return;
        }

        // 3. Find Left & Right Controller transforms
        Transform leftController = null;
        Transform rightController = null;

        foreach (Transform t in rig.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower();
            if (leftController == null && (n == "left controller" || n == "lefthand controller" || n == "left hand interaction"))
            {
                leftController = t;
            }
            else if (rightController == null && (n == "right controller" || n == "righthand controller" || n == "right hand interaction"))
            {
                rightController = t;
            }
        }

        if (leftController == null || rightController == null)
        {
            Debug.LogError("[SetupVRHands] Could not locate 'Left Controller' or 'Right Controller' inside the XR Origin hierarchy!");
            return;
        }

        Undo.SetCurrentGroupName("Setup VR POV Hands");
        int undoGroup = Undo.GetCurrentGroup();

        // 4. Attach & Calibrate Right Hand POV (original model orientation)
        SetupHandUnderController(rightController, "POV_Right_Hand", handPrefab,
            new Vector3(0.02f, -0.04f, -0.06f),
            new Vector3(-15f, 0f, 0f),
            Vector3.one);

        // 5. Attach & Calibrate Left Hand POV (mirrored on X axis)
        SetupHandUnderController(leftController, "POV_Left_Hand", handPrefab,
            new Vector3(-0.02f, -0.04f, -0.06f),
            new Vector3(-15f, 0f, 0f),
            new Vector3(-1f, 1f, 1f)); // Mirror X for proper left hand

        // 6. Attach VRPovHandsManager component to XR Origin
        var mgr = rig.GetComponent<VRPovHandsManager>();
        if (mgr == null)
        {
            mgr = Undo.AddComponent<VRPovHandsManager>(rig);
        }
        EditorUtility.SetDirty(mgr);

        Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = rig;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("<color=#00FFCC>[SetupVRHands]</color> Successfully configured pair of realistic first-person POV hands on XR Origin (XR Rig)! " +
                  "Left hand is mirrored. ProceduralHandAnimator attached for real-time finger animation.");
    }

    private static GameObject FindXROrigin()
    {
        // 1. Check selected object
        if (Selection.activeGameObject != null && (Selection.activeGameObject.GetComponent<XROrigin>() != null || Selection.activeGameObject.name.Contains("XR")))
        {
            return Selection.activeGameObject;
        }

        // 2. Search by component
        var origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin != null) return origin.gameObject;

        // 3. Search by name
        GameObject byName = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin") ?? GameObject.Find("XR Rig");
        if (byName != null) return byName;

        return null;
    }

    private static void SetupHandUnderController(Transform controllerAnchor, string handName, GameObject prefab, Vector3 localPos, Vector3 localRot, Vector3 localScale)
    {
        // Remove existing if present to ensure clean fresh setup
        Transform old = controllerAnchor.Find(handName);
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        GameObject handObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, controllerAnchor);
        handObj.name = handName;
        handObj.transform.localPosition = localPos;
        handObj.transform.localRotation = Quaternion.Euler(localRot);
        handObj.transform.localScale = localScale;

        // Remove physics colliders from hands to avoid colliding with player's own body
        foreach (var col in handObj.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(col);
        }

        // Remove any Animator with no controller (prevents it from overriding our procedural animation)
        var existingAnimator = handObj.GetComponent<Animator>();
        if (existingAnimator != null && existingAnimator.runtimeAnimatorController == null)
        {
            Object.DestroyImmediate(existingAnimator);
        }

        // Fix SkinnedMeshRenderer for negative scale (mirrored hand)
        if (localScale.x < 0f)
        {
            foreach (var smr in handObj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                smr.updateWhenOffscreen = true;
            }
        }

        // Add ProceduralHandAnimator component for real-time bone-driven finger animation
        if (handObj.GetComponent<ProceduralHandAnimator>() == null)
        {
            handObj.AddComponent<ProceduralHandAnimator>();
        }

        Undo.RegisterCreatedObjectUndo(handObj, $"Add {handName}");
    }
}
