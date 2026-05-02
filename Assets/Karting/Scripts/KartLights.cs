using UnityEngine;

public class KartLights : MonoBehaviour
{
    public Light[] headlights;
    public KeyCode toggleKey = KeyCode.L;

    bool isOn = false;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOn = !isOn;

            foreach (Light light in headlights)
            {
                if (light != null)
                    light.enabled = isOn;
            }
        }
    }
}