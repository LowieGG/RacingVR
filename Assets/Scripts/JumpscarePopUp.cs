using UnityEngine;
using TMPro;

public class JumpscarePopUp : MonoBehaviour
{
    [Header("Instellingen")]
    public GameObject smsCanvas;
    public TextMeshProUGUI smsTekst;
    public float minTijd = 5f;
    public float maxTijd = 15f;

    [Header("ESP32")]
    public int switchPin = 12; // Pas aan naar jouw pin

    private bool popupActief = false;
    private bool vorigeSchakelaarStatus = false;

    private string[] berichten = {
        "Hé waar ben je?? 😂",
        "BEL ME ASAP!!!",
        "Ben je al bijna thuis?",
        "Heb je mijn bericht gezien??",
        "ANTWOORD NU!!!! 😡",
        "Je moeder belde, bel terug!",
        "Feestje vanavond, kom je? 🎉"
    };

    void Start()
    {
        smsCanvas.SetActive(false);
        PlanVolgendePopup();
    }

    void Update()
    {
        // Lees schakelaar via ESP32Manager
        ESP32Manager esp32 = FindObjectOfType<ESP32Manager>();
        if (esp32 == null) return;

        bool huidigeStatus = esp32.schakelaarIngedrukt;

        // Detecteer rising edge (van uit naar aan)
        bool risingEdge = huidigeStatus && !vorigeSchakelaarStatus;

        if (risingEdge && popupActief)
        {
            VerbergPopup();
        }

        vorigeSchakelaarStatus = huidigeStatus;

        // Keyboard backup voor testen
        if (Input.GetKeyDown(KeyCode.X) && popupActief)
        {
            VerbergPopup();
        }
    }

    void ToonPopup()
    {
        smsTekst.text = berichten[Random.Range(0, berichten.Length)];
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