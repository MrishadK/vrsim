using UnityEngine;

/// <summary>
/// Animates baggage conveyor belts and moves physical luggage objects along the belt.
/// </summary>
public class AirportLuggageConveyor : MonoBehaviour
{
    [Header("Conveyor Settings")]
    [SerializeField] private float speed = 1.2f;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField] private Renderer beltRenderer;
    [SerializeField] private string texturePropertyName = "_BaseMap";

    private Material beltMaterial;
    private Vector2 currentOffset = Vector2.zero;

    private void Start()
    {
        if (beltRenderer == null)
        {
            beltRenderer = GetComponent<Renderer>();
        }

        if (beltRenderer != null)
        {
            beltMaterial = beltRenderer.material;
        }
    }

    private void Update()
    {
        if (beltMaterial != null)
        {
            currentOffset.y += speed * Time.deltaTime * 0.5f;
            beltMaterial.SetTextureOffset(texturePropertyName, currentOffset);
            if (beltMaterial.HasProperty("_MainTex"))
            {
                beltMaterial.SetTextureOffset("_MainTex", currentOffset);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)
        {
            Vector3 worldDir = transform.TransformDirection(moveDirection.normalized);
            other.attachedRigidbody.position += worldDir * (speed * Time.deltaTime);
        }
    }
}
