using UnityEngine;
using UnityEngine.UI;
using KartGame.KartSystems;
using TMPro;

namespace KartGame.UI
{
    /// <summary>
    /// DashboardHUD v2 - F1-stijl cockpit dashboard met echt rond stuurwiel en halo frame.
    /// Voeg dit script toe aan een Canvas. In XR wordt Screen Space - Camera gebruikt.
    /// Wijs de speler-kart toe aan 'Kart'.
    /// </summary>
    public class DashboardHUD : MonoBehaviour
    {
        [Header("Kart Reference")]
        [Tooltip("Sleep de speler-kart (ArcadeKart) hier naartoe.")]
        public ArcadeKart Kart;

        [Header("Stuurwiel")]
        [Tooltip("Maximale rotatie van het stuurwiel in graden.")]
        [Range(90f, 540f)]
        public float MaxSteerAngle = 270f;

        [Tooltip("Hoe snel het stuurwiel draait.")]
        [Range(1f, 20f)]
        public float SteerSpeed = 10f;

        [Header("Voorruit Frame")]
        [Tooltip("Toon een gebogen voorruit frame.")]
        public bool ShowWindshield = true;
        public Color WindshieldColor = new Color(0.06f, 0.06f, 0.06f, 0.95f);

        [Header("VR / World Space")]
        [Tooltip("Laat deze canvas in World Space staan zodat hij aan de cockpit vast blijft zitten.")]
        public bool WorldSpaceMode = false;

        [Tooltip("Verberg het 2D UI-stuur in VR. Gebruik daar het echte cockpit-stuur voor.")]
        public bool HideSteeringWheelInVR = true;

        [Tooltip("Verberg het 2D voorruit-frame in VR. Gebruik daar de echte cockpit-geometry voor.")]
        public bool HideWindshieldInVR = true;

        // Private UI refs
        private RectTransform m_SteeringWheel;
        private UICircle      m_RpmArc;
        private TextMeshProUGUI m_SpeedText;
        private TextMeshProUGUI m_GearText;
        private Image m_ThrottleLight;
        private Image m_BrakeLight;
        private Image m_DriftLight;

        private float m_CurrentSteerAngle = 0f;

        // Kleuren
        static readonly Color C_Dark      = new Color(0.07f, 0.07f, 0.07f, 0.96f);
        static readonly Color C_Carbon    = new Color(0.13f, 0.13f, 0.13f, 1f);
        static readonly Color C_Rim       = new Color(0.25f, 0.25f, 0.25f, 1f);
        static readonly Color C_Spoke     = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color C_Screen    = new Color(0.04f, 0.06f, 0.10f, 1f);
        static readonly Color C_Cyan      = new Color(0.0f,  0.85f, 0.95f, 1f);
        static readonly Color C_Green     = new Color(0.1f,  0.95f, 0.3f,  1f);
        static readonly Color C_Red       = new Color(1f,    0.15f, 0.1f,  1f);
        static readonly Color C_Orange    = new Color(1f,    0.55f, 0.0f,  1f);
        static readonly Color C_GreenDim  = new Color(0.05f, 0.22f, 0.08f, 1f);
        static readonly Color C_RedDim    = new Color(0.22f, 0.04f, 0.04f, 1f);
        static readonly Color C_OrangeDim = new Color(0.22f, 0.12f, 0.0f,  1f);

        void Start()
        {
            // Auto-detect: als de canvas al op World Space staat (ingesteld in de Inspector),
            // schakel WorldSpaceMode in zodat BuildDashboard het niet overschrijft.
            var existingCanvas = GetComponent<Canvas>();
            if (existingCanvas != null && existingCanvas.renderMode == RenderMode.WorldSpace)
                WorldSpaceMode = true;

            BuildDashboard();
        }

        void Update()
        {
            if (Kart == null) return;
            if (!WorldSpaceMode || !HideSteeringWheelInVR) UpdateWheel();
            UpdateDisplay();
            UpdateLights();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  BUILD
        // ══════════════════════════════════════════════════════════════════════════

        void BuildDashboard()
        {
            var canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();

            if (WorldSpaceMode)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = null;
                // World Space: blijft vastzittend in de 3D wereld aan de cockpit.
                // Render mode NIET aanpassen — de gebruiker heeft het al goed ingesteld.
                // CanvasScaler verwijderen (niet compatibel met WorldSpace + eigen schaal).
                var oldScaler = GetComponent<CanvasScaler>();
                if (oldScaler != null) Destroy(oldScaler);
            }
            else
            {
                Camera uiCamera = Camera.main;
                if (uiCamera != null)
                {
                    canvas.renderMode    = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera   = uiCamera;
                    canvas.planeDistance = 1.25f;
                }
                else
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                canvas.sortingOrder = 10;

                var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution  = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight   = 0.5f;
            }
            if (!GetComponent<GraphicRaycaster>())
                gameObject.AddComponent<GraphicRaycaster>();

            // ── Gebogen voorruit frame ────────────────────────────────────────
            if (ShowWindshield && (!WorldSpaceMode || !HideWindshieldInVR)) BuildWindshield();

            // ── Dashboard bodem balk ──────────────────────────────────────────
            var dashBar = MakeRect("DashBar", transform, C_Dark,
                new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(0,260));

            // ── Stuurwiel (midden, net boven bodem) ───────────────────────────
            if (!WorldSpaceMode || !HideSteeringWheelInVR)
                BuildSteeringWheel();

            // ── Snelheids + versnelling display (in stuur scherm) ─────────────
            BuildCenterDisplay();

            // ── RPM boog (half cirkel boven stuur) ───────────────────────────
            BuildRPMArc();

            // ── Indicator lichtjes ────────────────────────────────────────────
            BuildLights();
        }

        // ─── Gebogen voorruit ─────────────────────────────────────────────────────

        void BuildWindshield()
        {
            // Grote boog bovenaan — simuleert de gebogen bovenkant van de voorruit
            // We gebruiken een enorme UICircle ring waarvan alleen de bovenboog zichtbaar is
            var archGO = new GameObject("WindshieldArch");
            archGO.transform.SetParent(transform, false);
            var archRT = archGO.AddComponent<RectTransform>();
            archRT.anchorMin = new Vector2(0.5f, 1f);
            archRT.anchorMax = new Vector2(0.5f, 1f);
            archRT.sizeDelta = new Vector2(2400f, 2400f);
            archRT.anchoredPosition = new Vector2(0f, -900f);
            var arch = archGO.AddComponent<UICircle>();
            arch.InnerRadius = 0.94f;
            arch.Segments    = 120;
            arch.FillAmount  = 0.28f;   // alleen bovenboog
            arch.StartAngle  = 244f;
            arch.color       = WindshieldColor;

            // Linker A-stijl (schuin, van links-beneden naar boog)
            var pillarL = new GameObject("APillarL");
            pillarL.transform.SetParent(transform, false);
            var plRT = pillarL.AddComponent<RectTransform>();
            plRT.anchorMin = new Vector2(0f, 0.22f);
            plRT.anchorMax = new Vector2(0f, 0.22f);
            plRT.sizeDelta = new Vector2(80f, 620f);
            plRT.anchoredPosition = new Vector2(55f, 310f);
            plRT.localEulerAngles = new Vector3(0f, 0f, 18f);
            pillarL.AddComponent<Image>().color = WindshieldColor;

            // Rechter A-stijl
            var pillarR = new GameObject("APillarR");
            pillarR.transform.SetParent(transform, false);
            var prRT = pillarR.AddComponent<RectTransform>();
            prRT.anchorMin = new Vector2(1f, 0.22f);
            prRT.anchorMax = new Vector2(1f, 0.22f);
            prRT.sizeDelta = new Vector2(80f, 620f);
            prRT.anchoredPosition = new Vector2(-55f, 310f);
            prRT.localEulerAngles = new Vector3(0f, 0f, -18f);
            pillarR.AddComponent<Image>().color = WindshieldColor;

            // Smalle bovenrand (raam-rand bovenin)
            MakeRect("TopEdge", transform, WindshieldColor,
                new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
                new Vector2(0f, -38f), new Vector2(0f, 0f));
        }

        // ─── Stuurwiel ────────────────────────────────────────────────────────────

        void BuildSteeringWheel()
        {
            float size = 340f;

            var wheelGO = new GameObject("SteeringWheel");
            wheelGO.transform.SetParent(transform, false);
            var wheelRT = wheelGO.AddComponent<RectTransform>();
            wheelRT.anchorMin = wheelRT.anchorMax = new Vector2(0.5f, 0f);
            wheelRT.sizeDelta = new Vector2(size, size);
            wheelRT.anchoredPosition = new Vector2(0f, 145f);
            m_SteeringWheel = wheelRT;

            // --- Buitenste ring (carbon look = donkergrijs) ---
            MakeCircle(wheelGO.transform, "OuterRing", size, 0.82f, C_Rim, 64);

            // --- Grip zones (3 gekleurde sectoren op de ring) ---
            MakeCircleArc(wheelGO.transform, "GripTop",    size, 0.82f, C_Carbon, 64, 0.18f, -30f);
            MakeCircleArc(wheelGO.transform, "GripBotL",   size, 0.82f, C_Carbon, 32, 0.22f, 130f);
            MakeCircleArc(wheelGO.transform, "GripBotR",   size, 0.82f, C_Carbon, 32, 0.22f, 228f);

            // --- Spaken (3 stuks, F1-stijl) ---
            CreateSpoke(wheelGO.transform,  90f, size * 0.08f, size * 0.38f);  // boven
            CreateSpoke(wheelGO.transform, 210f, size * 0.08f, size * 0.38f);  // linksonder
            CreateSpoke(wheelGO.transform, 330f, size * 0.08f, size * 0.38f);  // rechtsonder

            // --- Naaf (center cirkel) ---
            MakeCircle(wheelGO.transform, "Hub", size * 0.28f, 0f, C_Carbon, 32);

            // --- Knoppen links & rechts van het scherm ---
            BuildWheelButtons(wheelGO.transform, size);
        }

        void BuildWheelButtons(Transform wheel, float wheelSize)
        {
            float btnW = 44f, btnH = 22f, gap = 6f;
            float sideX = wheelSize * 0.29f;
            float startY = 18f;

            Color[] btnColors = {
                new Color(0.1f, 0.4f, 0.9f, 1f),   // blauw
                new Color(0.9f, 0.2f, 0.1f, 1f),   // rood
                new Color(0.1f, 0.7f, 0.2f, 1f),   // groen
                new Color(0.9f, 0.7f, 0.0f, 1f),   // geel
            };
            string[] lblL = { "N", "BW", "EB", "OK" };
            string[] lblR = { "PL", "PC", "OT", "MID" };

            for (int i = 0; i < 4; i++)
            {
                float y = startY - i * (btnH + gap);
                BuildSmallButton(wheel, "BtnL" + i, new Vector2(-sideX, y), btnW, btnH,
                    btnColors[i], lblL[i]);
                BuildSmallButton(wheel, "BtnR" + i, new Vector2( sideX, y), btnW, btnH,
                    btnColors[(i + 1) % 4], lblR[i]);
            }
        }

        void BuildSmallButton(Transform parent, string name, Vector2 pos,
                              float w, float h, Color col, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            img.color = col;

            var txt = CreateTMP(name + "Lbl", go.transform, label,
                9f, Color.white, TextAlignmentOptions.Center);
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        // ─── Center display (in stuur) ────────────────────────────────────────────

        void BuildCenterDisplay()
        {
            float screenW = 130f, screenH = 90f;

            var screen = new GameObject("CenterScreen");
            screen.transform.SetParent(m_SteeringWheel != null ? m_SteeringWheel : transform, false);
            var srt = screen.AddComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(screenW, screenH);
            srt.anchoredPosition = m_SteeringWheel != null ? new Vector2(0f, 8f) : new Vector2(0f, 145f);
            var bg = screen.AddComponent<Image>();
            bg.color = C_Screen;

            // Snelheid groot
            m_SpeedText = CreateTMP("Speed", screen.transform,
                "0", 36f, C_Cyan, TextAlignmentOptions.Center);
            var sprt = m_SpeedText.GetComponent<RectTransform>();
            sprt.anchorMin = sprt.anchorMax = new Vector2(0.5f, 0.5f);
            sprt.sizeDelta = new Vector2(100f, 45f);
            sprt.anchoredPosition = new Vector2(0f, 14f);

            // KM/H label
            var kmh = CreateTMP("KMH", screen.transform,
                "KM/H", 9f, new Color(0.5f,0.6f,0.7f,1f), TextAlignmentOptions.Center);
            var krt = kmh.GetComponent<RectTransform>();
            krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f);
            krt.sizeDelta = new Vector2(80f, 16f);
            krt.anchoredPosition = new Vector2(0f, -6f);

            // Versnelling
            m_GearText = CreateTMP("Gear", screen.transform,
                "1", 22f, C_Orange, TextAlignmentOptions.Right);
            var grt = m_GearText.GetComponent<RectTransform>();
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(40f, 30f);
            grt.anchoredPosition = new Vector2(52f, 22f);

            // Scheidingslijn
            var line = new GameObject("Line");
            line.transform.SetParent(screen.transform, false);
            var lrt = line.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.05f, 0f); lrt.anchorMax = new Vector2(0.95f, 0f);
            lrt.anchoredPosition = new Vector2(0f, 22f);
            lrt.sizeDelta = new Vector2(0f, 2f);
            line.AddComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f, 0.8f);
        }

        // ─── RPM boog ────────────────────────────────────────────────────────────

        void BuildRPMArc()
        {
            float size = 400f;
            var arcGO = new GameObject("RPMArc");
            arcGO.transform.SetParent(transform, false);
            var art = arcGO.AddComponent<RectTransform>();
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0f);
            art.sizeDelta = new Vector2(size, size);
            art.anchoredPosition = new Vector2(0f, 145f);

            // Track boog (grijs)
            var track = arcGO.AddComponent<UICircle>();
            track.InnerRadius = 0.90f;
            track.Segments    = 80;
            track.FillAmount  = 0.65f;
            track.StartAngle  = 144f;
            track.color       = new Color(0.15f, 0.15f, 0.15f, 1f);

            // RPM vul boog (groen → rood)
            var fillGO = new GameObject("RPMFill");
            fillGO.transform.SetParent(arcGO.transform, false);
            var frt = fillGO.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            m_RpmArc = fillGO.AddComponent<UICircle>();
            m_RpmArc.InnerRadius = 0.90f;
            m_RpmArc.Segments    = 80;
            m_RpmArc.FillAmount  = 0f;
            m_RpmArc.StartAngle  = 144f;
            m_RpmArc.color       = C_Green;
        }

        // ─── Indicator lichtjes ───────────────────────────────────────────────────

        void BuildLights()
        {
            float y = 230f;
            m_ThrottleLight = MakeLightPill("GAS",   new Vector2(0.35f, 0f), new Vector2(0f, y), C_GreenDim,  "GAS");
            m_BrakeLight    = MakeLightPill("REM",   new Vector2(0.50f, 0f), new Vector2(0f, y), C_RedDim,    "REM");
            m_DriftLight    = MakeLightPill("DRIFT", new Vector2(0.65f, 0f), new Vector2(0f, y), C_OrangeDim, "DRIFT");
        }

        Image MakeLightPill(string name, Vector2 anchor, Vector2 pos, Color col, string label)
        {
            var go = new GameObject("Light_" + name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(80f, 26f);
            var img = go.AddComponent<Image>();
            img.color = col;

            var txt = CreateTMP(name + "Lbl", go.transform,
                label, 10f, Color.white, TextAlignmentOptions.Center);
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            return img;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  UPDATE
        // ══════════════════════════════════════════════════════════════════════════

        void UpdateWheel()
        {
            if (m_SteeringWheel == null) return;
            float target = -Kart.Input.TurnInput * MaxSteerAngle;
            m_CurrentSteerAngle = Mathf.Lerp(m_CurrentSteerAngle, target, Time.deltaTime * SteerSpeed);
            m_SteeringWheel.localEulerAngles = new Vector3(0f, 0f, m_CurrentSteerAngle);
        }

        void UpdateDisplay()
        {
            if (Kart.Rigidbody == null) return;
            float speed    = Kart.Rigidbody.velocity.magnitude;
            float speedKMH = speed * 3.6f;
            float t        = Mathf.Clamp01(speed / (Kart.baseStats.TopSpeed * 1.05f));

            m_SpeedText.text  = Mathf.RoundToInt(speedKMH).ToString();
            m_SpeedText.color = Color.Lerp(C_Cyan, C_Red, t);

            // Gesimuleerde versnelling op basis van snelheid
            int gear = Mathf.Clamp(Mathf.FloorToInt(speedKMH / 22f) + 1, 1, 6);
            m_GearText.text = gear.ToString();

            // RPM boog vullen
            if (m_RpmArc != null)
            {
                m_RpmArc.FillAmount = t * 0.65f;
                m_RpmArc.color      = Color.Lerp(C_Green, C_Red, t);
                m_RpmArc.SetVerticesDirty();
            }
        }

        void UpdateLights()
        {
            m_ThrottleLight.color = Kart.Input.Accelerate ? C_Green  : C_GreenDim;
            m_BrakeLight.color    = Kart.Input.Brake      ? C_Red    : C_RedDim;
            m_DriftLight.color    = Kart.WantsToDrift     ? C_Orange : C_OrangeDim;
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════════

        // Gewone rechthoek
        GameObject MakeRect(string name, Transform parent, Color color,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            return go;
        }

        // Volledige cirkel / ring via UICircle
        UICircle MakeCircle(Transform parent, string name, float size,
            float inner, Color color, int segs)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var c = go.AddComponent<UICircle>();
            c.InnerRadius = inner; c.Segments = segs;
            c.FillAmount  = 1f;   c.color    = color;
            return c;
        }

        // Boog (gedeeltelijke cirkel)
        UICircle MakeCircleArc(Transform parent, string name, float size,
            float inner, Color color, int segs, float fill, float startAngle)
        {
            var c = MakeCircle(parent, name, size, inner, color, segs);
            c.FillAmount = fill;
            c.StartAngle = startAngle;
            return c;
        }

        // Spaak van het stuurwiel
        void CreateSpoke(Transform parent, float angleDeg, float width, float length)
        {
            var go = new GameObject("Spoke_" + angleDeg);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, length);
            rt.anchoredPosition = new Vector2(
                Mathf.Sin(angleDeg * Mathf.Deg2Rad) * length * 0.35f,
                Mathf.Cos(angleDeg * Mathf.Deg2Rad) * length * 0.35f);
            rt.localEulerAngles = new Vector3(0f, 0f, -angleDeg);
            go.AddComponent<Image>().color = C_Spoke;
        }

        // TextMeshPro label
        TextMeshProUGUI CreateTMP(string name, Transform parent, string text,
            float size, Color color, TextAlignmentOptions align)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size;
            tmp.color = color; tmp.alignment = align;
            tmp.fontStyle = FontStyles.Bold;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(160f, 60f);
            return tmp;
        }
    }
}
