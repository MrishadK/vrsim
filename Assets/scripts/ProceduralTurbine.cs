using UnityEngine;

public class ProceduralTurbine : MonoBehaviour
{
    [Header("Turbine Settings")]
    public float radius = 0.6f;
    public float spinnerRadius = 0.15f;
    public int bladeCount = 24;
    public float spinSpeed = 800f; // degrees per second
    public Material turbineMaterial;

    private void Start()
    {
        GenerateTurbine();
    }

    private void GenerateTurbine()
    {
        // Create the central spinner cone
        GameObject spinner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spinner.name = "Spinner";
        spinner.transform.SetParent(this.transform, false);
        spinner.transform.localPosition = new Vector3(0, 0, 0.1f);
        // Elongate it into a cone shape along Z
        spinner.transform.localScale = new Vector3(spinnerRadius * 2, spinnerRadius * 2, spinnerRadius * 4);
        
        if (turbineMaterial != null) spinner.GetComponent<MeshRenderer>().sharedMaterial = turbineMaterial;
        Destroy(spinner.GetComponent<Collider>());

        // Create the fan blades
        for (int i = 0; i < bladeCount; i++)
        {
            float angle = i * (360f / bladeCount);
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade_" + i;
            blade.transform.SetParent(this.transform, false);
            
            // Math for radial positioning
            float rad = angle * Mathf.Deg2Rad;
            float midDist = spinnerRadius + (radius - spinnerRadius) / 2f;
            
            blade.transform.localPosition = new Vector3(Mathf.Sin(rad) * midDist, Mathf.Cos(rad) * midDist, 0);
            
            // Scale blade: thickness, length, depth
            float bladeLength = radius - spinnerRadius;
            blade.transform.localScale = new Vector3(0.02f, bladeLength, 0.1f);
            
            // Rotate blade to point outward (around Z) and twist slightly (pitch around Y)
            blade.transform.localRotation = Quaternion.Euler(0, 0, -angle); 
            blade.transform.localRotation *= Quaternion.Euler(0, 45f, 0); 
            
            if (turbineMaterial != null) blade.GetComponent<MeshRenderer>().sharedMaterial = turbineMaterial;
            Destroy(blade.GetComponent<Collider>());
        }
    }

    private void Update()
    {
        // Spin around Z axis continuously
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime, Space.Self);
    }
}
