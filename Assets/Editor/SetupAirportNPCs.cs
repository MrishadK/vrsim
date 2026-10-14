using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class SetupAirportNPCs
{
    [MenuItem("Tools/Setup Airport NPC System", false, 50)]
    public static void Execute()
    {
        Debug.Log("[SetupAirportNPCs] Starting NPC system overhaul...");

        // 1. Create or Find Route Manager
        AirportRouteManager routeManager = Object.FindObjectOfType<AirportRouteManager>();
        if (routeManager == null)
        {
            GameObject routeObj = GameObject.Find("AirportRouteManager");
            if (routeObj == null) routeObj = new GameObject("AirportRouteManager");
            routeManager = routeObj.AddComponent<AirportRouteManager>();
        }

        // Auto-discover waypoints
        Transform[] allTransforms = Object.FindObjectsOfType<Transform>();
        routeManager.entryPoints = allTransforms.Where(t => t.name.ToLower().Contains("entry") || t.name.ToLower().Contains("spawn")).ToList();
        routeManager.checkInPoints = allTransforms.Where(t => t.name.ToLower().Contains("checkin") || t.name.ToLower().Contains("check.in") || t.name.ToLower().Contains("check-in")).ToList();
        routeManager.securityPoints = allTransforms.Where(t => t.name.ToLower().Contains("security")).ToList();
        routeManager.immigrationPoints = allTransforms.Where(t => t.name.ToLower().Contains("immigrat")).ToList();
        routeManager.gatePoints = allTransforms.Where(t => t.name.ToLower().Contains("gate")).ToList();
        routeManager.boardingPoints = allTransforms.Where(t => t.name.ToLower().Contains("boarding")).ToList();

        // 2. Build Animator Controller
        string animDir = "Assets/Animations";
        if (!System.IO.Directory.Exists(animDir)) System.IO.Directory.CreateDirectory(animDir);
        
        string controllerPath = animDir + "/NPC_Locomotion.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            // Add Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Turn", AnimatorControllerParameterType.Float);
            controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Load Animations from Kevin Iglesias
            string animBase = "Assets/Kevin Iglesias/Human Animations/Animations/Male";
            AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Idles/HumanM@Idle01.fbx");
            AnimationClip talkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Social/Conversation/HumanM@Talk01.fbx");
            AnimationClip walkFwd = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Movement/Walk/HumanM@Walk01_Forward.fbx");
            AnimationClip walkLeft = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Movement/Walk/HumanM@Walk01_Left.fbx");
            AnimationClip walkRight = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animBase}/Movement/Walk/HumanM@Walk01_Right.fbx");

            // States
            AnimatorState idleState = rootStateMachine.AddState("Idle");
            if (idleClip != null) idleState.motion = idleClip;

            BlendTree walkTree;
            AnimatorState walkState = controller.CreateBlendTreeInController("Walk", out walkTree);
            walkTree.blendType = BlendTreeType.Simple1D;
            walkTree.blendParameter = "Turn";
            
            if (walkLeft != null) walkTree.AddChild(walkLeft, -1f);
            if (walkFwd != null) walkTree.AddChild(walkFwd, 0f);
            if (walkRight != null) walkTree.AddChild(walkRight, 1f);

            AnimatorState talkState = rootStateMachine.AddState("Talk");
            if (talkClip != null) talkState.motion = talkClip;

            // Transitions
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.duration = 0.25f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.duration = 0.25f;

            var toTalk = rootStateMachine.AddAnyStateTransition(talkState);
            toTalk.AddCondition(AnimatorConditionMode.If, 0, "Interact");
            toTalk.duration = 0.2f;

            var talkToIdle = talkState.AddTransition(idleState);
            talkToIdle.hasExitTime = true;
            talkToIdle.exitTime = 1f;
            talkToIdle.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupAirportNPCs] Created NPC_Locomotion Animator Controller.");
        }

        EditorUtility.SetDirty(routeManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        
        Debug.Log($"[SetupAirportNPCs] Route Manager updated: {routeManager.entryPoints.Count} Entry, {routeManager.checkInPoints.Count} Check-In, {routeManager.securityPoints.Count} Security, {routeManager.immigrationPoints.Count} Immigration, {routeManager.gatePoints.Count} Gate, {routeManager.boardingPoints.Count} Boarding points found.");
        Debug.Log("[SetupAirportNPCs] Success! Now play the game and the AirportVRExperience will spawn the humanoid NPCs.");
    }
}
