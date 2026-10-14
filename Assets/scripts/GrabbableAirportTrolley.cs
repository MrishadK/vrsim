using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR Grabbable Airport Trolley — Grab the handle with your VR controller grip button,
/// walk around and the trolley follows you like pushing a real trolley.
/// Luggage baggage is auto-attached on top.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GrabbableAirportTrolley : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    [Header("Trolley Physics")]
    [Tooltip("How smoothly the trolley follows the hand.")]
    [SerializeField] private float followSmoothing = 20f;

    [Tooltip("Max speed the trolley can move.")]
    [SerializeField] private float maxSpeed = 4f;

    [Header("Wheel Rotation")]
    [SerializeField] private Transform[] wheelTransforms;
    [SerializeField] private float wheelRadius = 0.04f;

    [Header("Luggage Setup")]
    [Tooltip("Luggage model spawned on top of trolley automatically.")]
    [SerializeField] private GameObject luggagePrefab;
    [SerializeField] private Vector3 luggageOffset = new Vector3(0f, 0.55f, 0.1f);
    [SerializeField] private float luggageScale = 1.0f;

    private Rigidbody rb;
    private bool isGrabbed;
    private Transform grabHand;
    private Vector3 grabOffset;
    private GameObject luggageInstance;
    private Vector3 lastPosition;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();

        // Configure Rigidbody for trolley-like ground physics
        rb.mass = 15f;
        rb.drag = 5f;
        rb.angularDrag = 8f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // XR Grab settings for realistic trolley feel
        movementType = MovementType.VelocityTracking;
        throwOnDetach = false;
        trackPosition = true;
        trackRotation = true;
        smoothPosition = true;
        smoothPositionAmount = followSmoothing;
        smoothRotation = true;
        smoothRotationAmount = followSmoothing;

        // Attach point at handle height
        if (attachTransform == null)
        {
            GameObject attachPoint = new GameObject("TrolleyGrabPoint");
            attachPoint.transform.SetParent(transform, false);
            attachPoint.transform.localPosition = new Vector3(0f, 1.0f, -0.35f);
            attachTransform = attachPoint.transform;
        }

        // Ensure proper interaction layer (Default layer is bit 0)
        interactionLayers = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Default");

        // Ensure there is at least one collider on the trolley for physics/grab detection
        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(0.6f, 1.1f, 0.9f);
        }

        lastPosition = transform.position;
    }

    private void Start()
    {
        SpawnLuggage();
        FixTrolleyMaterial();
    }

    private void FixTrolleyMaterial()
    {
        // Fix the red/orange texture issue by applying a clean metallic silver material to the trolley
        foreach (var mr in GetComponentsInChildren<MeshRenderer>())
        {
            // Skip luggage
            if (mr.transform.IsChildOf(luggageInstance != null ? luggageInstance.transform : null)) continue;
            
            if (mr.material != null)
            {
                // Remove the tiny weird red texture
                mr.material.mainTexture = null;
                if (mr.material.HasProperty("_BaseMap")) mr.material.SetTexture("_BaseMap", null);
                
                // Set to silver/chrome metal
                mr.material.color = new Color(0.75f, 0.75f, 0.76f, 1.0f); // Silver
                if (mr.material.HasProperty("_BaseColor")) mr.material.SetColor("_BaseColor", new Color(0.75f, 0.75f, 0.76f, 1.0f));
                
                // Make it metallic and smooth
                if (mr.material.HasProperty("_Metallic")) mr.material.SetFloat("_Metallic", 0.85f);
                if (mr.material.HasProperty("_Smoothness")) mr.material.SetFloat("_Smoothness", 0.7f);
                if (mr.material.HasProperty("_Glossiness")) mr.material.SetFloat("_Glossiness", 0.7f);
            }
        }
    }

    private void SpawnLuggage()
    {
        if (luggageInstance != null) return;

        if (luggagePrefab != null)
        {
            luggageInstance = Instantiate(luggagePrefab, transform);
        }
        else
        {
            // Try to load luggage model from project
#if UNITY_EDITOR
            var lugModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Airport_Luggage_Baggage.obj");
            if (lugModel != null)
            {
                luggageInstance = Instantiate(lugModel, transform);
            }
            else
#endif
            {
                // Fallback: create simple procedural luggage
                luggageInstance = new GameObject("Luggage_Fallback");
                luggageInstance.transform.SetParent(transform, false);

                // Large suitcase
                var suitcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                suitcase.name = "Suitcase";
                suitcase.transform.SetParent(luggageInstance.transform, false);
                suitcase.transform.localPosition = new Vector3(0, 0, 0);
                suitcase.transform.localScale = new Vector3(0.45f, 0.55f, 0.25f);
                var suitcaseCol = suitcase.GetComponent<Collider>();
                if (suitcaseCol) Destroy(suitcaseCol);
                var suitcaseRend = suitcase.GetComponent<Renderer>();
                if (suitcaseRend) suitcaseRend.material.color = new Color(0.10f, 0.14f, 0.30f);

                // Smaller bag on top
                var bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.name = "Bag";
                bag.transform.SetParent(luggageInstance.transform, false);
                bag.transform.localPosition = new Vector3(0.05f, 0.38f, 0);
                bag.transform.localScale = new Vector3(0.38f, 0.18f, 0.22f);
                var bagCol = bag.GetComponent<Collider>();
                if (bagCol) Destroy(bagCol);
                var bagRend = bag.GetComponent<Renderer>();
                if (bagRend) bagRend.material.color = new Color(0.45f, 0.08f, 0.06f);
            }
        }

        if (luggageInstance != null)
        {
            luggageInstance.name = "Trolley_Luggage";
            luggageInstance.transform.localPosition = luggageOffset;
            luggageInstance.transform.localScale = Vector3.one * luggageScale;

            // Remove all colliders from luggage so they don't interfere
            foreach (var col in luggageInstance.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        isGrabbed = true;
        grabHand = args.interactorObject.transform;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        isGrabbed = false;
        grabHand = null;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (!isGrabbed) return;

        // Clamp speed
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        // Keep trolley upright
        Vector3 euler = transform.eulerAngles;
        euler.x = 0f;
        euler.z = 0f;
        transform.rotation = Quaternion.Euler(euler);
    }

    private void Update()
    {
        // Animate wheels
        if (wheelTransforms != null && wheelTransforms.Length > 0)
        {
            Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
            float speed = velocity.magnitude;
            float rotAngle = (speed / (2f * Mathf.PI * wheelRadius)) * 360f * Time.deltaTime;

            foreach (var wheel in wheelTransforms)
            {
                if (wheel != null)
                {
                    wheel.Rotate(Vector3.right, rotAngle, Space.Self);
                }
            }
        }
        lastPosition = transform.position;
    }
}
