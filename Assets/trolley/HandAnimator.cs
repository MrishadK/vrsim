using UnityEngine;
using UnityEngine.InputSystem; // Required for modern VR inputs

/// <summary>
/// DEPRECATED: Use ProceduralHandAnimator instead, which drives finger bones
/// directly without requiring AnimationClips or an AnimatorController.
/// This script is kept for backward compatibility only.
/// </summary>
[System.Obsolete("Use ProceduralHandAnimator instead — it drives finger bones procedurally without needing an AnimatorController.")]
public class HandAnimator : MonoBehaviour
{
    [Header("Controller Inputs")]
    [Tooltip("The input action for the Index Finger (Trigger).")]
    public InputActionProperty pinchAnimationAction; 
    
    [Tooltip("The input action for the Middle Finger (Grip).")]
    public InputActionProperty gripAnimationAction;  

    [Header("3D Model Setup")]
    [Tooltip("Drag your 3D Hand Model here from the Hierarchy.")]
    public Animator handAnimator;

    void Start()
    {
        // SAFETY NET: If the slot in the Inspector is left completely empty, 
        // the script will automatically search inside the RightHandAnchor 
        // to find the 'uploads_files_2864994_arms' model for you.
        if (handAnimator == null)
        {
            handAnimator = GetComponentInChildren<Animator>();
            
            if (handAnimator == null)
            {
                Debug.LogError("No Animator found! Please drag your 3D hand model into the HandAnimator script.", this);
            }
        }
    }

    void Update()
    {
        // Prevent the game from crashing if the Animator is still missing
        if (handAnimator == null) return;

        // 1. Read how hard the player is squeezing the buttons (from 0.0 to 1.0)
        float triggerValue = pinchAnimationAction.action != null ? pinchAnimationAction.action.ReadValue<float>() : 0f;
        float gripValue = gripAnimationAction.action != null ? gripAnimationAction.action.ReadValue<float>() : 0f;

        // 2. Send those exact numbers to the Unity animation system
        handAnimator.SetFloat("Trigger", triggerValue);
        handAnimator.SetFloat("Grip", gripValue);
    }
}