using UnityEngine;

/// <summary>
/// Continuous rotation helper for airport surface radars, beacons, and turbine fans.
/// </summary>
public class SimpleRotator : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 90f; // degrees per second

    private void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
