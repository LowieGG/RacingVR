using UnityEngine;

public class CollisionVibration : MonoBehaviour
{
    private ESP32Manager esp32;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (esp32 != null)
        {
            esp32.StuurVibratie();
            Debug.Log("Botsing gedetecteerd!");
        }
    }
}