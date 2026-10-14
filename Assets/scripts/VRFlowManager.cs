using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A strict state machine manager for tracking the player's journey through the airport.
/// </summary>
public class VRFlowManager : MonoBehaviour
{
    public enum JourneyState
    {
        Entrance,
        CheckIn,
        Security,
        Gate,
        Boarded
    }

    [Header("Current State")]
    public JourneyState currentState = JourneyState.Entrance;

    [Header("State Events")]
    public UnityEvent OnCheckInCompleted;
    public UnityEvent OnSecurityCompleted;
    public UnityEvent OnBoardingStarted;

    /// <summary>
    /// Call this from the Passport Socket to advance the flow.
    /// </summary>
    public void CompleteCheckIn()
    {
        if (currentState == JourneyState.Entrance || currentState == JourneyState.CheckIn)
        {
            Debug.Log("[VRFlowManager] Check-in Complete! Proceed to Security.");
            currentState = JourneyState.Security;
            OnCheckInCompleted?.Invoke();
        }
    }

    public void CompleteSecurity()
    {
        if (currentState == JourneyState.Security)
        {
            Debug.Log("[VRFlowManager] Security Complete! Proceed to Gate.");
            currentState = JourneyState.Gate;
            OnSecurityCompleted?.Invoke();
        }
    }
    
    public void StartBoarding()
    {
        if (currentState == JourneyState.Gate)
        {
            Debug.Log("[VRFlowManager] Boarding Started!");
            currentState = JourneyState.Boarded;
            OnBoardingStarted?.Invoke();
        }
    }
}
