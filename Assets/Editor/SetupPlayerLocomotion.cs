using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// AeroSim VR — Editor Setup Tool for Player Locomotion, Animator Controller, and Body Avatar.
/// Builds the complete 2D Freeform Directional Locomotion Blend Tree for the player
/// and binds the PlayerBodyAvatar onto the XR Origin in the game scene.
/// 
/// Menu: Tools → Setup Player Locomotion & Body Avatar (Ctrl+Shift+P)
/// </summary>
public static class SetupPlayerLocomotion
{
    private const string CONTROLLER_PATH = "Assets/Animations/Player_Locomotion.controller";
    private const string ANIM_BASE = "Assets/Kevin Iglesias/Human Animations/Animations/Male";
    private const string SURVIVAL_MODEL_PATH = "Assets/uploads_files_4323274_Survival_Character.fbx";
    private const string DUMMY_MODEL_PATH = "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx";

    [InitializeOnLoadMethod]
    private static void AutoEnsureController()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(CONTROLLER_PATH))
            {
                CreateOrUpdateAnimatorController();
            }
        };
    }

    [MenuItem("Tools/Setup Player Locomotion & Body Avatar #&p", false, 4)]
    public static void Execute()
    {
        // 1. Build or refresh Animator Controller
        AnimatorController controller = CreateOrUpdateAnimatorController();
        if (controller == null)
        {
            Debug.LogError("[SetupPlayerLocomotion] Failed to create Player_Locomotion.controller!");
            return;
        }

        // 2. Find XR Origin in active scene
        GameObject playerRig = FindPlayerRig();
        if (playerRig == null)
        {
            Debug.LogError("[SetupPlayerLocomotion] No XR Origin found in active scene. Please open SampleScene.");
            return;
        }

        Undo.SetCurrentGroupName("Setup Player Locomotion & Body Avatar");
        int undoGroup = Undo.GetCurrentGroup();

        // 3. Ensure AirportLocomotion on Player
        var locomotion = playerRig.GetComponent<AirportLocomotion>();
        if (locomotion == null)
        {
            locomotion = Undo.AddComponent<AirportLocomotion>(playerRig);
        }
        locomotion.walkSpeed = 1.45f;
        locomotion.sprintSpeed = 3.3f;
        locomotion.acceleration = 6.5f;
        locomotion.brakingDeceleration = 9.0f;
        EditorUtility.SetDirty(locomotion);

        // 4. Ensure PlayerFootstepAudio on Player
        var footstepAudio = playerRig.GetComponent<PlayerFootstepAudio>();
        if (footstepAudio == null)
        {
            footstepAudio = Undo.AddComponent<PlayerFootstepAudio>(playerRig);
        }
        footstepAudio.BindLocomotion(locomotion);
        EditorUtility.SetDirty(footstepAudio);

        // 5. Ensure PlayerBodyAvatar on Player
        var bodyAvatar = playerRig.GetComponent<PlayerBodyAvatar>();
        if (bodyAvatar == null)
        {
            bodyAvatar = Undo.AddComponent<PlayerBodyAvatar>(playerRig);
        }

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SURVIVAL_MODEL_PATH)
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(DUMMY_MODEL_PATH);

        bodyAvatar.characterModelAsset = modelAsset;
        bodyAvatar.animatorController = controller;
        bodyAvatar.hideHeadInFirstPerson = true;
        EditorUtility.SetDirty(bodyAvatar);

        Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log("<color=#00FF99>[SetupPlayerLocomotion]</color> Successfully configured Player Locomotion, 2D Blend-Tree Animation, and Body Avatar on XR Origin!");
    }

    public static AnimatorController CreateOrUpdateAnimatorController()
    {
        string dir = Path.GetDirectoryName(CONTROLLER_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller != null)
        {
            AssetDatabase.DeleteAsset(CONTROLLER_PATH);
        }

        controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        // Add Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("DirectionX", AnimatorControllerParameterType.Float);
        controller.AddParameter("DirectionZ", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsSprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AnimSpeed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Load Clips
        AnimationClip idleClip = GetClip($"{ANIM_BASE}/Idles/HumanM@Idle01.fbx");

        AnimationClip walkFwd = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_Forward.fbx");
        AnimationClip walkBwd = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_Backward.fbx");
        AnimationClip walkLeft = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_Left.fbx");
        AnimationClip walkRight = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_Right.fbx");
        AnimationClip walkFwdLeft = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_ForwardLeft.fbx");
        AnimationClip walkFwdRight = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_ForwardRight.fbx");
        AnimationClip walkBwdLeft = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_BackwardLeft.fbx");
        AnimationClip walkBwdRight = GetClip($"{ANIM_BASE}/Movement/Walk/HumanM@Walk01_BackwardRight.fbx");

        AnimationClip sprintFwd = GetClip($"{ANIM_BASE}/Movement/Sprint/HumanM@Sprint01_Forward.fbx");
        AnimationClip runBwd = GetClip($"{ANIM_BASE}/Movement/Run/HumanM@Run01_Backward.fbx");
        AnimationClip runLeft = GetClip($"{ANIM_BASE}/Movement/Run/HumanM@Run01_Left.fbx");
        AnimationClip runRight = GetClip($"{ANIM_BASE}/Movement/Run/HumanM@Run01_Right.fbx");
        AnimationClip sprintFwdLeft = GetClip($"{ANIM_BASE}/Movement/Sprint/HumanM@Sprint01_ForwardLeft.fbx")
            ?? GetClip($"{ANIM_BASE}/Movement/Run/HumanM@Run01_ForwardLeft.fbx");
        AnimationClip sprintFwdRight = GetClip($"{ANIM_BASE}/Movement/Sprint/HumanM@Sprint01_ForwardRight.fbx")
            ?? GetClip($"{ANIM_BASE}/Movement/Run/HumanM@Run01_ForwardRight.fbx");
        AnimationClip runBwdLeft = GetClip($"{ANIM_BASE}/Movement/Run/HumanM@Run01_BackwardLeft.fbx");
        AnimationClip runBwdRight = GetClip($"{ANIM_BASE}/Movement/Run/HumanM@Run01_BackwardRight.fbx");

        // 1. Idle State
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idleClip;

        // 2. 2D Freeform Directional Walk Blend Tree
        BlendTree walkTree;
        AnimatorState walkState = controller.CreateBlendTreeInController("Walk", out walkTree);
        walkTree.blendType = BlendTreeType.FreeformDirectional2D;
        walkTree.blendParameter = "DirectionX";
        walkTree.blendParameterY = "DirectionZ";

        if (walkFwd != null) walkTree.AddChild(walkFwd, new Vector2(0f, 1f));
        if (walkBwd != null) walkTree.AddChild(walkBwd, new Vector2(0f, -1f));
        if (walkLeft != null) walkTree.AddChild(walkLeft, new Vector2(-1f, 0f));
        if (walkRight != null) walkTree.AddChild(walkRight, new Vector2(1f, 0f));
        if (walkFwdLeft != null) walkTree.AddChild(walkFwdLeft, new Vector2(-0.707f, 0.707f));
        if (walkFwdRight != null) walkTree.AddChild(walkFwdRight, new Vector2(0.707f, 0.707f));
        if (walkBwdLeft != null) walkTree.AddChild(walkBwdLeft, new Vector2(-0.707f, -0.707f));
        if (walkBwdRight != null) walkTree.AddChild(walkBwdRight, new Vector2(0.707f, -0.707f));

        walkState.speedParameterActive = true;
        walkState.speedParameter = "AnimSpeed";

        // 3. 2D Freeform Directional Sprint Blend Tree
        BlendTree sprintTree;
        AnimatorState sprintState = controller.CreateBlendTreeInController("Sprint", out sprintTree);
        sprintTree.blendType = BlendTreeType.FreeformDirectional2D;
        sprintTree.blendParameter = "DirectionX";
        sprintTree.blendParameterY = "DirectionZ";

        if (sprintFwd != null) sprintTree.AddChild(sprintFwd, new Vector2(0f, 1f));
        if (runBwd != null) sprintTree.AddChild(runBwd, new Vector2(0f, -1f));
        if (runLeft != null) sprintTree.AddChild(runLeft, new Vector2(-1f, 0f));
        if (runRight != null) sprintTree.AddChild(runRight, new Vector2(1f, 0f));
        if (sprintFwdLeft != null) sprintTree.AddChild(sprintFwdLeft, new Vector2(-0.707f, 0.707f));
        if (sprintFwdRight != null) sprintTree.AddChild(sprintFwdRight, new Vector2(0.707f, 0.707f));
        if (runBwdLeft != null) sprintTree.AddChild(runBwdLeft, new Vector2(-0.707f, -0.707f));
        if (runBwdRight != null) sprintTree.AddChild(runBwdRight, new Vector2(0.707f, -0.707f));

        sprintState.speedParameterActive = true;
        sprintState.speedParameter = "AnimSpeed";

        // 4. State Transitions
        // Idle -> Walk
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
        idleToWalk.duration = 0.2f;

        // Walk -> Idle
        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
        walkToIdle.duration = 0.25f;

        // Walk -> Sprint
        var walkToSprint = walkState.AddTransition(sprintState);
        walkToSprint.AddCondition(AnimatorConditionMode.If, 0, "IsSprinting");
        walkToSprint.duration = 0.2f;

        // Sprint -> Walk
        var sprintToWalk = sprintState.AddTransition(walkState);
        sprintToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSprinting");
        sprintToWalk.duration = 0.25f;

        // Sprint -> Idle
        var sprintToIdle = sprintState.AddTransition(idleState);
        sprintToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
        sprintToIdle.duration = 0.25f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return controller;
    }

    private static AnimationClip GetClip(string fbxPath)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxPath);
        if (clip != null) return clip;

        var all = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var a in all)
        {
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                return c;
        }
        return null;
    }

    private static GameObject FindPlayerRig()
    {
        if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<XROrigin>() != null)
            return Selection.activeGameObject;

        var origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin != null) return origin.gameObject;

        return GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin") ?? GameObject.Find("XR Rig");
    }
}
