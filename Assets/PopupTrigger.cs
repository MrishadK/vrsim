using UnityEngine;
using UnityEngine.Events; // NEW: The professional way to trigger multiple actions!
using UnityEngine.XR.Interaction.Toolkit; 

public class PopupTrigger : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The UI Canvas to show")]
    public GameObject popupCanvas; 
    [Tooltip("The movement system to freeze")]
    public ContinuousMoveProviderBase moveProvider; 

    [Header("Pro Event System")]
    [Tooltip("Drag anything here to happen exactly when the player enters the zone (e.g., Hide Marker, Play Sound)")]
    public UnityEvent onPlayerEnter;
    [Tooltip("Drag anything here to happen when the 'Understood' button is clicked")]
    public UnityEvent onPopupClosed;

    private bool hasTriggered = false;

    private void Start()
    {
        if (popupCanvas != null) 
        {
            popupCanvas.SetActive(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player") || other.CompareTag("MainCamera")) return true;
        if (other.GetComponentInParent<CharacterController>() != null) return true;
        if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && IsPlayer(other))
        {
            hasTriggered = true;         
            
            // 1. Show UI and Freeze Player
            if (popupCanvas != null) popupCanvas.SetActive(true); 
            if (moveProvider == null) moveProvider = FindObjectOfType<ContinuousMoveProviderBase>();
            if (moveProvider != null) moveProvider.enabled = false;

            // 2. Trigger all custom Inspector events (like hiding your 3D ring!)
            onPlayerEnter?.Invoke();
        }
    }

    public void ClosePopup()
    {
        // 1. Hide UI and Unfreeze Player
        if (popupCanvas != null) popupCanvas.SetActive(false); 
        if (moveProvider != null) moveProvider.enabled = true;

        // 2. Trigger custom closing events
        onPopupClosed?.Invoke();
    }

    private void Update()
    {
        if (popupCanvas != null && popupCanvas.activeInHierarchy)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ClosePopup(); 
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ClosePopup(); 
            }
#endif
            // Meta Quest / OpenXR Controller primary button (A / X) support
            var rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightHand.isValid && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aPressed) && aPressed)
            {
                ClosePopup();
            }
            var leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
            if (leftHand.isValid && leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool xPressed) && xPressed)
            {
                ClosePopup();
            }
        }
    }
}