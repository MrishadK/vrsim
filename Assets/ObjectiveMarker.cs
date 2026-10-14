using UnityEngine;

public class ObjectiveMarker : MonoBehaviour
{
    [Header("Animation Settings")]
    public float spinSpeed = 90f;        // How fast it rotates
    public float floatAmplitude = 0.2f;  // How high it bobs up and down
    public float floatFrequency = 1f;    // How fast it bobs

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 1. Spin the marker
        transform.Rotate(0, spinSpeed * Time.deltaTime, 0);

        // 2. Float the marker up and down using a Sine wave
        float newY = startPos.y + Mathf.Sin(Time.time * Mathf.PI * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}