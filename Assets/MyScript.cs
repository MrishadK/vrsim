using UnityEngine;

public class TrainingSystem : MonoBehaviour
{
    public int step = 0;

    void Start()
    {
        Debug.Log("Training Started: Go to Check-in Area");
    }

    public void NextStep()
    {
        step++;

        switch (step)
        {
            case 1:
                Debug.Log("Step 1: Check-in completed. Go to Security.");
                break;

            case 2:
                Debug.Log("Step 2: Security completed. Go to Boarding Gate.");
                break;

            case 3:
                Debug.Log("Step 3: Boarding completed. Training Finished.");
                break;
        }
    }
}