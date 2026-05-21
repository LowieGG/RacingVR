using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    [Header("Instellingen")]
    public Color flashKleur = Color.white;
    public float flashDuur = 0.2f;
    public KeyCode flashToets = KeyCode.F;

    private Image flashImage;
    private bool bezig = false;
    private float timer = 0f;
    private ESP32Manager esp32;
    private bool vorigeStatus = false;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();

        // Maak automatisch een image aan
        flashImage = GetComponent<Image>();
        if (flashImage == null)
            flashImage = gameObject.AddComponent<Image>();

        flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, 0f);
        flashImage.raycastTarget = false;
    }

    void Update()
    {
        // Koppel aan een ESP32 knop naar keuze
        // bool huidigeStatus = esp32 != null && esp32.???;
        // bool risingEdge = huidigeStatus && !vorigeStatus;

        if (Input.GetKeyDown(flashToets) && !bezig)
        {
            StartFlash();
        }

        if (bezig)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = 1f - (timer / flashDuur);
            flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, alpha);

            if (timer >= flashDuur)
            {
                StopFlash();
            }
        }

        // vorigeStatus = huidigeStatus;
    }

    void StartFlash()
    {
        bezig = true;
        timer = 0f;
        flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, 1f);
        Debug.Log("FLASH!");
    }

    void StopFlash()
    {
        bezig = false;
        flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, 0f);
    }
}