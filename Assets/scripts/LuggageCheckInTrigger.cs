using UnityEngine;
using TMPro;

public class LuggageCheckInTrigger : MonoBehaviour
{
    [Header("References")]
    public GameObject luggageModel;
    public Canvas infoCanvas;
    public TextMeshProUGUI weightText;

    private bool hasTriggered = false;

    private void Start()
    {
        // Ensure UI and model are hidden initially
        if (luggageModel != null)
            luggageModel.SetActive(false);
            
        if (infoCanvas != null)
            infoCanvas.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Optionally check if it's the player triggering it. 
        // We will assume any trigger enters (e.g. from the XR Rig or hands) counts.
        if (other.CompareTag("Player") || other.GetComponentInParent<CharacterController>() != null || other.name.Contains("XR") || other.name.Contains("Player"))
        {
            hasTriggered = true;
            ShowLuggageInfo();
        }
    }

    private void ShowLuggageInfo()
    {
        if (luggageModel != null)
            luggageModel.SetActive(true);

        if (infoCanvas != null && weightText != null)
        {
            infoCanvas.gameObject.SetActive(true);

            // Generate random weight between 15kg and 45kg
            float weight = Random.Range(15f, 45f);
            
            // Format the weight to 1 decimal place
            string weightStr = weight.ToString("F1") + " Kg";

            string colorTag = weight > 30f ? "<color=red>" : "<color=green>";

            string fullMessage = $"Bag Weight: {colorTag}{weightStr}</color>\n\n" +
                                 $"This weight depends on your ticket.\n" +
                                 $"Standard ticket has a 30Kg luggage limit.\n" +
                                 $"If it goes above, you will have to remove items or pay an extra amount.";

            weightText.text = fullMessage;
        }
    }
}
