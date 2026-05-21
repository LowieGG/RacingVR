using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KartGame.Track;

public class VRLapTimeHUD : MonoBehaviour
{
    [Header("Positie (relatief aan Main Camera)")]
    public Vector3 localPosition = new Vector3(-0.6f, 0.35f, 0.75f);

    [Tooltip("Breedte van het HUD paneel in UI-eenheden")]
    public float panelWidth = 160f;

    [Tooltip("Hoogte van het HUD paneel in UI-eenheden")]
    public float panelHeight = 65f;

    [Header("Grootte")]
    public float canvasScale = 0.0005f;

    private Text currentLapTimeText;
    private Text bestLapTimeText;
    private TimeManager timeManager;

    void Start()
    {
        timeManager = FindObjectOfType<TimeManager>();
        CreateVRCanvas();
    }

    void CreateVRCanvas()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[VRLapTimeHUD] Geen Main Camera gevonden!");
            return;
        }

        GameObject canvasGO = new GameObject("VRLapTimeHUD_Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        canvasGO.transform.SetParent(mainCam.transform, false);
        canvasGO.transform.localPosition = localPosition;
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * canvasScale;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("Background");
        bg.layer = LayerMask.NameToLayer("UI");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        SetFullStretch(bgRect);

        bestLapTimeText = CreateLabel(
            canvasGO.transform,
            "BestLapTime",
            "Best:  --:--.--",
            Color.green,
            36,
            new Vector2(0f, 0.52f),
            new Vector2(1f, 1.0f),
            new Vector2(8f, 4f),
            new Vector2(-8f, -4f)
        );

        currentLapTimeText = CreateLabel(
            canvasGO.transform,
            "CurrentLapTime",
            "Lap:  0:00.00",
            Color.white,
            30,
            new Vector2(0f, 0f),
            new Vector2(1f, 0.52f),
            new Vector2(8f, 4f),
            new Vector2(-8f, -4f)
        );
    }

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

    void Update()
    {
        if (timeManager == null) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Transform canvasTransform = mainCam.transform.Find("VRLapTimeHUD_Canvas");
            if (canvasTransform != null)
            {
                canvasTransform.localPosition = localPosition;
                canvasTransform.localScale = Vector3.one * canvasScale;
            }
        }

        float current = timeManager.CurrentLapTime;
        float best = timeManager.BestLapTime;

        currentLapTimeText.text = "Lap: " + FormatTime(current);

        if (best <= 0f || best == float.MaxValue)
            bestLapTimeText.text = "Best: --:--.--";
        else
            bestLapTimeText.text = "Best: " + FormatTime(best);
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        int hundredths = (int)((time * 100f) % 100f);
        return string.Format("{0}:{1:00}.{2:00}", minutes, seconds, hundredths);
    }
}