using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        TimeManager tm = FindFirstObjectByType<TimeManager>();

        if (tm == null)
        {
            Debug.LogError("No TimeManager found!");
            return;
        }

        Debug.Log("FINISH HIT → LAP COMPLETE");

        tm.CompleteLap();
    }
}