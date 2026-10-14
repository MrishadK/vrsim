using UnityEngine;
using UnityEngine.Splines;

public class SplineSpeedBooster : MonoBehaviour
{
    [Header("Engine Reference")]
    public SplineAnimate planeAnimator;

    [Header("Speed Settings")]
    public float baseTaxiSpeed = 15f;
    public float maxTakeoffSpeed = 120f;

    [Header("Knot Trigger Thresholds (Percentages)")]
    [Range(0f, 1f)]
    public float knot18Percentage = 0.72f;

    void Start()
    {
        // Just grab the engine, no fancy setup required
        if (planeAnimator == null)
            planeAnimator = GetComponent<SplineAnimate>();
    }

    void Update()
    {
        if (planeAnimator == null) return;

        float currentTime = planeAnimator.NormalizedTime;

        if (currentTime < knot18Percentage)
        {
            planeAnimator.MaxSpeed = baseTaxiSpeed;
        }
        else if (currentTime >= knot18Percentage && currentTime <= 1f)
        {
            float accelerationProgress = (currentTime - knot18Percentage) / (1f - knot18Percentage);
            planeAnimator.MaxSpeed = Mathf.Lerp(baseTaxiSpeed, maxTakeoffSpeed, accelerationProgress);
        }
    }
}