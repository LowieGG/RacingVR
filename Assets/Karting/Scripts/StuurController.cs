using UnityEngine;

public class StuurController : MonoBehaviour
{
    public float maxRotatie = 90f; // Maximale hoek van het stuur
    private ESP32Manager esp32;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void Update()
    {
        if (esp32 == null) return;

        // Zet de stuurhoek om naar rotatie (-100 tot 100 → -90° tot 90°)
        float hoek = (esp32.stuurHoek / 100f) * maxRotatie;
        transform.localRotation = Quaternion.Euler(0f, 0f, -hoek);
    }
}
