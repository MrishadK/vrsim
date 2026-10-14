using UnityEngine;

public class SimpleLinearFlight : MonoBehaviour
{
    [Header("Flight Settings")]
    [Tooltip("How fast the airplane moves forward.")]
    public float flightSpeed = 50.0f;

    void Update()
    {
        // Moves the airplane endlessly in the direction its nose is pointing
transform.Translate(Vector3.back * flightSpeed * Time.deltaTime);    }
}