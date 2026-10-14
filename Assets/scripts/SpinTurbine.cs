using UnityEngine;

public class SpinTurbine : MonoBehaviour
{
    [Header("Turbine Spin Settings")]
    [Tooltip("Degrees per second to rotate around the spin axis")]
    public float rpm = 1200f;
    public Vector3 spinAxis = Vector3.forward;

    private void Update()
    {
        transform.Rotate(spinAxis, rpm * Time.deltaTime, Space.Self);
    }
}
