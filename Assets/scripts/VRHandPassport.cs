using UnityEngine;
using TMPro;

/// <summary>
/// AeroSim VR — VR Hand Passport
/// Attaches a production-quality 3D Passport booklet to the player's VR controller.
/// </summary>
public class VRHandPassport : MonoBehaviour
{
    [Header("Passport Model Setup")]
    public GameObject passportPrefab;

    [Header("Positioning (Relative to Controller)")]
    public Vector3 localPositionOffset = new Vector3(-0.02f, 0.05f, 0.05f);
    public Vector3 localRotationOffset = new Vector3(-15f, 90f, -90f);
    public float scaleFactor = 0.06f;

    private GameObject passportObj;

    private void Start()
    {
        // Passport auto-spawning has been disabled as requested.
        // CreatePassportBooklet();
    }

    private void CreatePassportBooklet()
    {
        if (transform.Find("VR_Passport_Real") != null) return;

        // 1. Load the real passport FBX
        if (passportPrefab == null)
        {
#if UNITY_EDITOR
            passportPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PASSPORT/source/32313324324 جواز سفر.fbx");
#endif
        }

        if (passportPrefab != null)
        {
            passportObj = Instantiate(passportPrefab, transform);
            passportObj.name = "VR_Passport_Real";

            // Remove any colliders if they exist to prevent physics glitches
            foreach (var col in passportObj.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }

            // Adjust scale, rotation, and position to fit naturally in the VR hand
            passportObj.transform.localPosition = localPositionOffset;
            passportObj.transform.localRotation = Quaternion.Euler(localRotationOffset);
            passportObj.transform.localScale = Vector3.one * scaleFactor;
        }
        else
        {
            Debug.LogError("[AeroSim VR] Real Passport model not found at expected path!");
        }
    }
}
