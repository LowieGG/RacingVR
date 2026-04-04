using UnityEngine;
using UnityEngine.UI;

namespace KartGame.UI
{
    /// <summary>
    /// UICircle - Tekent een echte cirkel of ring via Unity's UI mesh systeem.
    /// Gebruik dit als component op een UI GameObject voor perfecte cirkels zonder sprite.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UICircle : MaskableGraphic
    {
        [Range(0f, 1f)]
        [Tooltip("0 = gevulde schijf, 1 = dunne ring. Alles ertussen = ring met dikte.")]
        public float InnerRadius = 0.75f;

        [Range(3, 128)]
        [Tooltip("Hoe vloeiender de cirkel (meer segmenten = ronder maar zwaarder).")]
        public int Segments = 64;

        [Tooltip("Vulpercentage van de cirkel (1 = volledig, 0.5 = halve cirkel).")]
        [Range(0f, 1f)]
        public float FillAmount = 1f;

        [Tooltip("Starthoek in graden (0 = rechts, 90 = boven).")]
        public float StartAngle = 0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            float cx = r.x + r.width  * 0.5f;
            float cy = r.y + r.height * 0.5f;
            float outerR = Mathf.Min(r.width, r.height) * 0.5f;
            float innerR = outerR * InnerRadius;

            int segs = Mathf.Max(3, Mathf.RoundToInt(Segments * FillAmount));
            float angleStep = 360f * FillAmount / segs;
            float startRad = (StartAngle - 90f) * Mathf.Deg2Rad;

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            for (int i = 0; i <= segs; i++)
            {
                float angle = startRad + i * angleStep * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Buitenste vertex
                vert.position = new Vector3(cx + cos * outerR, cy + sin * outerR);
                vert.uv0 = new Vector2((cos + 1f) * 0.5f, (sin + 1f) * 0.5f);
                vh.AddVert(vert);

                // Binnenste vertex
                vert.position = new Vector3(cx + cos * innerR, cy + sin * innerR);
                vert.uv0 = new Vector2((cos * InnerRadius + 1f) * 0.5f, (sin * InnerRadius + 1f) * 0.5f);
                vh.AddVert(vert);
            }

            // Triangles
            for (int i = 0; i < segs; i++)
            {
                int b = i * 2;
                vh.AddTriangle(b,     b + 1, b + 2);
                vh.AddTriangle(b + 1, b + 3, b + 2);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif
    }
}
