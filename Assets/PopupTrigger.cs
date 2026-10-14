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

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;         
            
            // 1. Show UI and Freeze Player
            if (popupCanvas != null) popupCanvas.SetActive(true); 
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

    // This ensures your testing hack ONLY runs on your PC, keeping the Quest APK optimized!
#if UNITY_EDITOR
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
        }
    }
#endif
}