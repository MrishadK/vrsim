using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
[ExecuteAlways]
public class TaxiwayMarkingGenerator : MonoBehaviour
{
    public float lineWidth = 0.4f;
    public Color markingColor = new Color(1f, 0.85f, 0.1f, 1f);
    public int resolution = 150;
    
    private LineRenderer lr;
    private SplineContainer splineContainer;

    void OnEnable()
    {
        splineContainer = GetComponent<SplineContainer>();
        lr = GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = gameObject.AddComponent<LineRenderer>();
        }
        
        UpdateMarkings();
    }

    void Update()
    {
        // Update in edit mode if spline changes
        if (!Application.isPlaying && splineContainer != null && lr != null)
        {
            UpdateMarkings();
        }
    }

    public void UpdateMarkings()
    {
        if (splineContainer.Splines.Count == 0) return;

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.TransformZ;
        
        Material lineMat = lr.sharedMaterial;
        if (lineMat == null)
        {
            lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            lr.sharedMaterial = lineMat;
        }
        lineMat.color = markingColor;

        lr.positionCount = resolution;
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            Unity.Mathematics.float3 pos, tangent, up;
            splineContainer.Evaluate(0, t, out pos, out tangent, out up);
            
            Vector3 worldPos = transform.TransformPoint(new Vector3(pos.x, pos.y, pos.z));
            // Keep it slightly above ground to prevent z-fighting
            worldPos.y = -0.07f; 
            lr.SetPosition(i, worldPos);
        }
        
        // Face upwards
        lr.transform.rotation = Quaternion.Euler(90, 0, 0);
    }
}
