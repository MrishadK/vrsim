using UnityEngine;

public class EscalatorVisuals : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The direction the steps move. Usually diagonally down.")]
    public Vector3 moveDirection = new Vector3(-1, 0, 1); 
    public float speed = 0.5f;
    
    [Tooltip("The exact width of ONE step. Tweak this until the loop is invisible!")]
    public float snapDistance = 0.4f; 

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
        
        moveDirection = moveDirection.normalized; 
    }

    void Update()
    {
        transform.localPosition += moveDirection * speed * Time.deltaTime;

        float distanceMoved = Vector3.Distance(startPosition, transform.localPosition);

        if (distanceMoved >= snapDistance)
        {
            transform.localPosition = startPosition;
        }
    }
}