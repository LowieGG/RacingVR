using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Verbindt ESP32Manager knopdrukken met de ButtonAnimation visuele feedback
/// op het F1-stuur (groen flash + beweging).
///
/// Gebruikt SendMessage + reflectie zodat dit script niet rechtstreeks
/// afhankelijk is van ButtonAnimation (andere assembly).
///
/// Zet dit script op een leeg GameObject in de scène.
/// </summary>
public class ESP32SteeringWheelVisuals : MonoBehaviour
{
    [System.Serializable]
    public class KnopKoppeling
    {
        [Tooltip("Naam van het bool-veld op ESP32Manager (bijv. 'nitroIngedrukt')")]
        public string esp32VeldNaam = "nitroIngedrukt";

        [Tooltip("Knopnummer op het stuur — komt overeen met Knop (X) of ButtonAnimation.knopNummer")]
        public int knopNummer = 1;
    }

    [Header("Koppelingen: ESP32 veld → stuurknop nummer")]
    public List<KnopKoppeling> koppelingen = new List<KnopKoppeling>
    {
        new KnopKoppeling { esp32VeldNaam = "nitroIngedrukt",   knopNummer = 1 },
        new KnopKoppeling { esp32VeldNaam = "honkIngedrukt",    knopNummer = 2 },
        new KnopKoppeling { esp32VeldNaam = "jumpIngedrukt",    knopNummer = 3 },
        new KnopKoppeling { esp32VeldNaam = "ejectIngedrukt",   knopNummer = 4 },
        new KnopKoppeling { esp32VeldNaam = "lichtenIngedrukt", knopNummer = 5 },
        new KnopKoppeling { esp32VeldNaam = "laserIngedrukt",   knopNummer = 6 },
        new KnopKoppeling { esp32VeldNaam = "smsKnopIngedrukt", knopNummer = 7 },
        new KnopKoppeling { esp32VeldNaam = "remIngedrukt",     knopNummer = 8 },
    };

    [Header("Zoekroot voor de knop-animaties")]
    [Tooltip("Laat leeg = heel de scène. Stel in op het SteeringWheel voor betere performantie.")]
    public Transform stuurSearchRoot;

    // ── intern ──────────────────────────────────────────────────────
    private ESP32Manager esp32;
    private bool[] vorigeStaten;
    private FieldInfo[] esp32Velden;

    // Cache: knopNummer → MonoBehaviour (ButtonAnimation zonder directe type-ref)
    private Dictionary<int, MonoBehaviour> animatieCache = new Dictionary<int, MonoBehaviour>();

    // ────────────────────────────────────────────────────────────────
    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
        if (esp32 == null)
        {
            Debug.LogWarning("[ESP32SteeringWheelVisuals] Geen ESP32Manager gevonden!");
            enabled = false;
            return;
        }

        vorigeStaten = new bool[koppelingen.Count];
        esp32Velden  = new FieldInfo[koppelingen.Count];

        for (int i = 0; i < koppelingen.Count; i++)
        {
            esp32Velden[i] = typeof(ESP32Manager).GetField(
                koppelingen[i].esp32VeldNaam,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (esp32Velden[i] == null)
                Debug.LogWarning($"[ESP32SteeringWheelVisuals] Veld '{koppelingen[i].esp32VeldNaam}' niet gevonden op ESP32Manager.");
        }

        HerbouwAnimatieCache();
    }

    // ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (esp32 == null) return;

        for (int i = 0; i < koppelingen.Count; i++)
        {
            if (esp32Velden[i] == null) continue;

            bool huidig = (bool)esp32Velden[i].GetValue(esp32);
            bool risingEdge = huidig && !vorigeStaten[i];

            if (risingEdge)
                TriggerKnopAnimatie(koppelingen[i].knopNummer);

            vorigeStaten[i] = huidig;
        }
    }

    // ────────────────────────────────────────────────────────────────
    private void TriggerKnopAnimatie(int knopNummer)
    {
        if (!animatieCache.TryGetValue(knopNummer, out MonoBehaviour anim) || anim == null)
        {
            HerbouwAnimatieCache();
            animatieCache.TryGetValue(knopNummer, out anim);
        }

        if (anim != null)
        {
            // SendMessage werkt assemblij-overschrijdend
            anim.SendMessage("DrukIn", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning($"[ESP32SteeringWheelVisuals] Geen ButtonAnimation gevonden voor knopNummer {knopNummer}.");
        }
    }

    // ────────────────────────────────────────────────────────────────
    private void HerbouwAnimatieCache()
    {
        animatieCache.Clear();

        MonoBehaviour[] kandidaten = stuurSearchRoot != null
            ? stuurSearchRoot.GetComponentsInChildren<MonoBehaviour>(true)
            : FindObjectsOfType<MonoBehaviour>(true);

        MethodInfo matchMethode = null;

        foreach (MonoBehaviour mb in kandidaten)
        {
            if (mb == null) continue;
            if (mb.GetType().Name != "ButtonAnimation") continue;

            // Haal KomtOvereenMetKnopNummer op via reflectie (eenmalig per type)
            if (matchMethode == null)
                matchMethode = mb.GetType().GetMethod(
                    "KomtOvereenMetKnopNummer",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            // Probeer knopNummer-veld direct uit te lezen
            FieldInfo knopNrVeld = mb.GetType().GetField(
                "knopNummer",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (knopNrVeld != null)
            {
                int nr = (int)knopNrVeld.GetValue(mb);
                if (nr >= 0 && !animatieCache.ContainsKey(nr))
                {
                    animatieCache[nr] = mb;
                    continue;
                }
            }

            // Fallback: naam parsen "Knop (X)"
            int nummerUitNaam = HaalNummerUitNaam(mb.gameObject.name);
            if (nummerUitNaam >= 0 && !animatieCache.ContainsKey(nummerUitNaam))
                animatieCache[nummerUitNaam] = mb;
        }
    }

    private static int HaalNummerUitNaam(string naam)
    {
        if (string.IsNullOrEmpty(naam)) return -1;
        int open  = naam.LastIndexOf('(');
        int close = naam.LastIndexOf(')');
        if (open >= 0 && close > open)
        {
            string tussen = naam.Substring(open + 1, close - open - 1).Trim();
            if (int.TryParse(tussen, out int n)) return n;
        }
        return -1;
    }
}
