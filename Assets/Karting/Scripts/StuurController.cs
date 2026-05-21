using UnityEngine;

public class StuurController : MonoBehaviour
{
    public float maxRotatie = 45f;        // Maximale hoek van het stuur
    public float sensitivity = 5f;      // Hoger = gevoeliger stuur

    private ESP32Manager esp32;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void Update()
    {
        if (esp32 == null) return;

        // Sensitivity toepassen
        float stuurInput = esp32.stuurHoek * sensitivity;

        // Begrenzen zodat het stuur niet verder gaat dan -100 tot 100
        stuurInput = Mathf.Clamp(stuurInput, -100f, 100f);

        // Zet de stuurhoek om naar rotatie
        float hoek = (stuurInput / 100f) * maxRotatie;

        transform.localRotation = Quaternion.Euler(0f, 0f, -hoek);
    }
}