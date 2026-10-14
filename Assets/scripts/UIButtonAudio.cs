using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple, reliable UI sound effects trigger for hover and click events.
/// Automatically communicates with AirportSFXManager.
/// </summary>
public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AirportSFXManager.Instance != null)
        {
            AirportSFXManager.Instance.PlayHover();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AirportSFXManager.Instance != null)
        {
            AirportSFXManager.Instance.PlayClick();
        }
    }
}
