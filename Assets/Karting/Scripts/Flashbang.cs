using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    [Header("Instellingen")]
    public Color flashKleur = Color.white;
    public float flashDuur = 0.3f;
    public KeyCode flashToets = KeyCode.F;
    public AudioClip flashGeluid;

    private Image flashImage;
    private bool bezig = false;
    private float timer = 0f;
    private ESP32Manager esp32;
    private bool vorigeStatus = false;
    private AudioSource audioSource;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        flashImage = GetComponent<Image>();
        if (flashImage == null)
            flashImage = gameObject.AddComponent<Image>();

        flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, 0f);
        flashImage.raycastTarget = false;
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.lichtenIngedrukt;
        bool risingEdge = huidigeStatus && !vorigeStatus;

        if ((risingEdge || Input.GetKeyDown(flashToets)) && !bezig)
        {
            StartFlash();
        }

        vorigeStatus = huidigeStatus;

        if (bezig)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = 1f - (timer / flashDuur);
            flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, Mathf.Clamp01(alpha));

            if (timer >= flashDuur)
                StopFlash();
        }
    }

    void StartFlash()
    {
        bezig = true;
        timer = 0f;
        flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, 1f);

        if (flashGeluid != null)
            audioSource.PlayOneShot(flashGeluid, 2f);

        Debug.Log("FLASH!");
    }

    void StopFlash()
    {
        bezig = false;
        flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, 0f);
    }

    void OnDisable()
    {
        if (flashImage != null)
            flashImage.color = new Color(flashKleur.r, flashKleur.g, flashKleur.b, 0f);
    }
}