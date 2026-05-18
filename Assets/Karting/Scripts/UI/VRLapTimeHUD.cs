using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KartGame.Track;

/// <summary>
/// Maakt een VR-vriendelijke lap time HUD aan als child van de Main Camera.
/// Toont de huidige laptijd (wit, onder) en de beste laptijd (groen, boven).
/// Volgt automatisch mee met het hoofd van de speler.
/// </summary>
public class VRLapTimeHUD : MonoBehaviour
{
    [Header("Positie (relatief aan Main Camera)")]
    [Tooltip("Lokale positie t.o.v. de Main Camera. X positief = rechts, Z = afstand voor je.")]
    public Vector3 localPosition = new Vector3(0.22f, -0.13f, 0.75f);

    [Tooltip("Breedte van het HUD paneel in UI-eenheden")]
    public float panelWidth = 320f;

    [Tooltip("Hoogte van het HUD paneel in UI-eenheden")]
    public float panelHeight = 130f;

    // Interne tijdsregistratie
    private float currentLapStartTime = 0f;
    private bool raceStarted = false;
    private List<float> finishedLapTimes = new List<float>();

    // UI referenties
    private Text currentLapTimeText;
    private Text bestLapTimeText;

    // ---------------------------------------------------------------
    void OnEnable()
    {
        TimeDisplay.OnUpdateLap += OnLapCompleted;
        TimeDisplay.OnSetLaps += OnSetLaps;
    }

    void OnDisable()
    {
        TimeDisplay.OnUpdateLap -= OnLapCompleted;
        TimeDisplay.OnSetLaps -= OnSetLaps;
    }

    void Start()
    {
        CreateVRCanvas();
    }

    // ---------------------------------------------------------------
    // Canvas aanmaken als child van Main Camera
    // ---------------------------------------------------------------
    void CreateVRCanvas()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[VRLapTimeHUD] Geen Main Camera gevonden! Zorg dat de camera de tag 'MainCamera' heeft.");
            return;
        }

        // --- Root canvas object ---
        GameObject canvasGO = new GameObject("VRLapTimeHUD_Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvasGO.transform.SetParent(mainCam.transform, false);
        canvasGO.transform.localPosition = localPosition;
        canvasGO.transform.localRotation = Quaternion.identity;
        // 1 unit in de canvas = 1 millimeter in de wereld (scale 0.001)
        canvasGO.transform.localScale = Vector3.one * 0.001f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // --- Halftransparante achtergrond ---
        GameObject bg = new GameObject("Background");
        bg.layer = LayerMask.NameToLayer("UI");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        SetFullStretch(bgRect);

        // --- Beste laptijd (groen, boven, gecentreerd) ---
        bestLapTimeText = CreateLabel(
            canvasGO.transform,
            "BestLapTime",
            "Best:  --:--.--",
            Color.green,
            36,
            new Vector2(0f, 0.52f),   // anchorMin
            new Vector2(1f, 1.0f),    // anchorMax
            new Vector2(8f, 4f),      // offsetMin
            new Vector2(-8f, -4f)     // offsetMax
        );

        // --- Huidige laptijd (wit, onder, gecentreerd) ---
        currentLapTimeText = CreateLabel(
            canvasGO.transform,
            "CurrentLapTime",
            "Lap:  0:00.00",
            Color.white,
            30,
            new Vector2(0f, 0f),      // anchorMin
            new Vector2(1f, 0.52f),   // anchorMax
            new Vector2(8f, 4f),      // offsetMin
            new Vector2(-8f, -4f)     // offsetMax
        );
    }

    // ---------------------------------------------------------------
    // Hulpfunctie: maak een Text label aan
    // ---------------------------------------------------------------
    Text CreateLabel(Transform parent, string name, string defaultText,
                     Color color, int fontSize,
                     Vector2 anchorMin, Vector2 anchorMax,
                     Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        Text t = go.AddComponent<Text>();
        t.text = defaultText;
        t.color = color;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        // Gebruik de ingebouwde Unity font
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null)
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;

        return t;
    }

    void SetFullStretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    // ---------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------
    void OnSetLaps(int laps)
    {
        // Race wordt geïnitialiseerd, reset onze teller
        currentLapStartTime = 0f;
        raceStarted = false;
        finishedLapTimes.Clear();
        if (bestLapTimeText != null)  bestLapTimeText.text  = "Best:  --:--.--";
        if (currentLapTimeText != null) currentLapTimeText.text = "Lap:  0:00.00";
    }

    void OnLapCompleted()
    {
        if (currentLapStartTime == 0f)
        {
            // Eerste crossing = startsein van de klok
            currentLapStartTime = Time.time;
            raceStarted = true;
            return;
        }

        // Sla laptijd op
        float lapTime = Time.time - currentLapStartTime;
        finishedLapTimes.Add(lapTime);
        currentLapStartTime = Time.time;

        // Update beste laptijd
        RefreshBestLap();
    }

    void RefreshBestLap()
    {
        if (finishedLapTimes.Count == 0 || bestLapTimeText == null) return;

        float best = float.MaxValue;
        foreach (float t in finishedLapTimes)
            if (t < best) best = t;

        bestLapTimeText.text = "Best:  " + FormatTime(best);
    }

    // ---------------------------------------------------------------
    // Update: huidige laptijd in real-time tonen
    // ---------------------------------------------------------------
    void Update()
    {
        if (!raceStarted || currentLapStartTime == 0f || currentLapTimeText == null) return;

        float elapsed = Time.time - currentLapStartTime;
        currentLapTimeText.text = "Lap:  " + FormatTime(elapsed);
    }

    // ---------------------------------------------------------------
    // Tijdformat: M:SS.hh
    // ---------------------------------------------------------------
    string FormatTime(float time)
    {
        int minutes  = (int)(time / 60f);
        int seconds  = (int)(time % 60f);
        int hundredths = (int)((time * 100f) % 100f);
        return string.Format("{0}:{1:00}.{2:00}", minutes, seconds, hundredths);
    }
}
