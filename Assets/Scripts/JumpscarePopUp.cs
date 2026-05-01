using UnityEngine;
using TMPro;

public class JumpscarePopUp : MonoBehaviour
{
    [Header("Instellingen")]
    public GameObject smsCanvas;
    public TextMeshProUGUI smsTekst;
    public KeyCode sluitToets = KeyCode.X;
    public float minTijd = 5f;
    public float maxTijd = 15f;

    private bool popupActief = false;

    private string[] berichten = {
        "Hé waar ben je schatje??",
        "BEL ME ASAP!!!",
        "Ben je al bijna thuis?",
        "Heb je mijn bericht gezien??",
        "ANTWOORD NU!!!! ",
        "Je moeder belde, bel terug!",
        "Ben je nog wakker? X"
    };

    void Start()
    {
        smsCanvas.SetActive(false);
        PlanVolgendePopup();
    }

    void Update()
    {
        if (popupActief && Input.GetKeyDown(sluitToets))
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