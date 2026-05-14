using UnityEngine;

public class KartHonk : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip honkSound;

    private ESP32Manager esp32;
    private bool vorigeHonkStatus = false;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.honkIngedrukt;
        bool risingEdge = huidigeStatus && !vorigeHonkStatus;

        if (risingEdge || Input.GetKeyDown(KeyCode.H))
        {
            Honk();
        }

        vorigeHonkStatus = huidigeStatus;
    }

    void Honk()
    {
        if (audioSource != null && honkSound != null)
        {
            audioSource.PlayOneShot(honkSound);
            Debug.Log("TOET TOET!");
        }
    }
}