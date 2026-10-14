using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: Places a fully-configured VR grabbable Airport Trolley with luggage
/// directly into the current scene. One click — no configuration needed.
/// </summary>
public static class PlaceGrabbableTrolley
{
    private const string TROLLEY_FBX = "Assets/trolley/source/Airport_Trolley.fbx";
    private const string LUGGAGE_OBJ = "Assets/Models/Airport_Luggage_Baggage.obj";

    [MenuItem("Tools/Place Grabbable Airport Trolley in Scene", false, 6)]
    public static void Execute()
    {
        // 1. Load trolley model
        GameObject trolleyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TROLLEY_FBX);
        if (trolleyPrefab == null)
        {
            // Try OBJ fallback
            trolleyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/trolley/source/Airport_Trolley.obj");
        }
        if (trolleyPrefab == null)
        {
            Debug.LogError("[PlaceGrabbableTrolley] Airport_Trolley model not found!");
            return;
        }

        // 2. Instantiate trolley into scene
        GameObject trolley = (GameObject)PrefabUtility.InstantiatePrefab(trolleyPrefab);
        trolley.name = "Airport_Trolley_Grabbable";

        // Position near airport entrance area (adjustable)
        trolley.transform.position = new Vector3(-42f, 0.1f, -32f);
        trolley.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        // 3. Add Rigidbody (required by GrabbableAirportTrolley)
        Rigidbody rb = trolley.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = trolley.AddComponent<Rigidbody>();
        }
        rb.mass = 15f;
#if UNITY_2023_3_OR_NEWER
        rb.linearDamping = 5f;
        rb.angularDamping = 8f;
#else
        rb.drag = 5f;
        rb.angularDrag = 8f;
#endif
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 4. Ensure there is at least one collider for physics & grab detection
        if (trolley.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider col = trolley.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(0.6f, 1.1f, 0.9f);
        }

        // 5. Add GrabbableAirportTrolley component (handles XR grab + luggage spawn)
        GrabbableAirportTrolley trolleyScript = trolley.GetComponent<GrabbableAirportTrolley>();
        if (trolleyScript == null)
        {
            trolleyScript = trolley.AddComponent<GrabbableAirportTrolley>();
        }

        // 6. Load & attach luggage model
        GameObject luggagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LUGGAGE_OBJ);
        if (luggagePrefab != null)
        {
            GameObject luggage = (GameObject)PrefabUtility.InstantiatePrefab(luggagePrefab, trolley.transform);
            luggage.name = "Trolley_Luggage";
            luggage.transform.localPosition = new Vector3(0f, 0.55f, 0.1f);
            luggage.transform.localScale = Vector3.one;

            // Remove all colliders from luggage
            foreach (var c in luggage.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(c);
            }
        }

        // 7. Create the grab-handle attach point at the trolley handle bar
        GameObject grabPoint = new GameObject("TrolleyGrabPoint");
        grabPoint.transform.SetParent(trolley.transform, false);
        grabPoint.transform.localPosition = new Vector3(0f, 1.0f, -0.35f);

        // Assign attach point properly using the public property
        trolleyScript.attachTransform = grabPoint.transform;

        // Mark dirty & select
        Undo.RegisterCreatedObjectUndo(trolley, "Place Grabbable Airport Trolley");
        EditorUtility.SetDirty(trolley);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject = trolley;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("<color=#00FFCC>[PlaceGrabbableTrolley]</color> Grabbable Airport Trolley with luggage placed in scene! Walk up, press GRIP to grab, walk to push it around.");
    }
}
