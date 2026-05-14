using UnityEngine;

public class KartLights : MonoBehaviour
{
    public Light[] headlights;
    public KeyCode toggleKey = KeyCode.L;

    private bool isOn = false;
    private ESP32Manager esp32;
    private bool vorigeLichtStatus = false;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.lichtenIngedrukt;
        bool risingEdge = huidigeStatus && !vorigeLichtStatus;

        if (risingEdge || Input.GetKeyDown(toggleKey))
        {
            isOn = !isOn;
            foreach (Light light in headlights)
            {
                if (light != null)
                    light.enabled = isOn;
            }
            Debug.Log("Lichten: " + (isOn ? "AAN" : "UIT"));
        }

        vorigeLichtStatus = huidigeStatus;
    }
}