using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Manages the XR Socket Interactor at the Check-in and Security desks.
/// Requires the Unity OpenXR Interaction Toolkit.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class PassportSocketManager : MonoBehaviour
{
    [Header("Flow Manager Reference")]
    public VRFlowManager flowManager;

    [Header("Validation")]
    [Tooltip("The exact tag the passport object must have to be accepted.")]
    public string expectedPassportTag = "Passport";

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    private void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    private void OnEnable()
    {
        // Subscribe to XR grab events (Legacy or modern XRI events depending on toolkit version)
#pragma warning disable CS0618
        socket.selectEntered.AddListener(OnPassportInserted);
        socket.selectExited.AddListener(OnPassportRemoved);
#pragma warning restore CS0618
    }

    private void OnDisable()
    {
#pragma warning disable CS0618
        socket.selectEntered.RemoveListener(OnPassportInserted);
        socket.selectExited.RemoveListener(OnPassportRemoved);
#pragma warning restore CS0618
    }

    private void OnPassportInserted(SelectEnterEventArgs args)
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable = args.interactableObject;
        
        if (interactable.transform.CompareTag(expectedPassportTag))
        {
            Debug.Log("[PassportSocket] Valid Passport Inserted!");

            // Prevent the player from grabbing it back immediately (optional realism)
            if (interactable is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable)
            {
                grabInteractable.interactionLayers = InteractionLayerMask.GetMask("Nothing");
            }

            // Trigger the state progression
            if (flowManager != null)
            {
                if (flowManager.currentState == VRFlowManager.JourneyState.Entrance || 
                    flowManager.currentState == VRFlowManager.JourneyState.CheckIn)
                {
                    flowManager.CompleteCheckIn();
                }
                else if (flowManager.currentState == VRFlowManager.JourneyState.Security)
                {
                    flowManager.CompleteSecurity();
                }
            }
        }
        else
        {
            Debug.LogWarning("[PassportSocket] Invalid object inserted. Expecting tag: " + expectedPassportTag);
        }
    }

    private void OnPassportRemoved(SelectExitEventArgs args)
    {
        // Handle logic if player is allowed to take passport back
    }
}
