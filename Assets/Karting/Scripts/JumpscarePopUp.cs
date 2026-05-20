using UnityEngine;
using TMPro;

public class JumpscarePopUp : MonoBehaviour
{
    [Header("Instellingen")]
    public GameObject smsCanvas;
    public TextMeshProUGUI smsTekst;
    public TextMeshProUGUI contactNaam;  // Extra tekstveld voor naam
    public AudioClip smsGeluid;          // Sleep je "sms" clip hierin
    public float minTijd = 5f;
    public float maxTijd = 15f;

    private bool popupActief = false;
    private bool vorigeSchakelaarStatus = false;
    private ESP32Manager esp32;
    private AudioSource audioSource;

    private string[] berichten = {
        "Hé waar ben je??",
        "BEL ME ASAP!!!",
        "HGH WDJ",
        "ANTWOORD NU!!!!",
        "Feestje vanavond, kom je?",
        "you up?"
    };

    private string[] namen = {
        "Mama",
        "Thomas",
        "Emma",
        "Kevin",
        "Sofie",
        "Papa",
        "Lars"
    };

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        smsCanvas.SetActive(false);
        PlanVolgendePopup();
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.smsKnopIngedrukt;
        bool risingEdge = huidigeStatus && !vorigeSchakelaarStatus;

        if (risingEdge && popupActief)
            VerbergPopup();

        vorigeSchakelaarStatus = huidigeStatus;

        if (Input.GetKeyDown(KeyCode.X) && popupActief)
            VerbergPopup();
    }

    void ToonPopup()
    {
        // Kies random bericht en naam
        string bericht = berichten[Random.Range(0, berichten.Length)];
        string naam = namen[Random.Range(0, namen.Length)];

        smsTekst.text = bericht;

        if (contactNaam != null)
            contactNaam.text = naam;

        // Speel geluid
        if (smsGeluid != null)
            audioSource.PlayOneShot(smsGeluid);

        smsCanvas.SetActive(true);
        popupActief = true;
        Debug.Log("SMS POPUP!");
    }

    void VerbergPopup()
    {
        smsCanvas.SetActive(false);
        popupActief = false;
        PlanVolgendePopup();
    }

    void PlanVolgendePopup()
    {
        float volgendeTijd = Random.Range(minTijd, maxTijd);
        Invoke(nameof(ToonPopup), volgendeTijd);
    }
}