using UnityEngine;
using KartGame.KartSystems;

namespace KartGame.KartSystems
{
    /// <summary>
    /// CockpitCamera - plaatst de camera in first-person cockpit view binnenin de kart.
    /// Voeg dit script toe aan de Main Camera in de scene.
    /// Wijs de speler-kart toe aan 'KartTarget'.
    /// Schakel de Cinemachine Virtual Camera UIT in de Hierarchy.
    /// </summary>
    public class CockpitCamera : MonoBehaviour
    {
        [Header("Kart Reference")]
        [Tooltip("Sleep de speler-kart (ArcadeKart) hier naartoe.")]
        public Transform KartTarget;

        [Header("Cockpit Positie")]
        [Tooltip("Offset van de camera t.o.v. het midden van de kart (bestuurdersstoel positie).")]
        public Vector3 CockpitOffset = new Vector3(0f, 0.75f, 0.25f);

        [Tooltip("Hoe snel de camera de kart volgt (lager = soepeler).")]
        [Range(0.01f, 1f)]
        public float PositionSmoothing = 0.08f;

        [Tooltip("Hoe snel de camera draait mee met de kart.")]
        [Range(0.01f, 1f)]
        public float RotationSmoothing = 0.06f;

        [Header("Camera Shake bij hobbels")]
        [Tooltip("Hoeveel de camera schudt bij rijden over hobbels.")]
        [Range(0f, 0.05f)]
        public float ShakeAmount = 0.012f;

        private Vector3 m_CurrentVelocity;
        private Quaternion m_CurrentRot;
        private ArcadeKart m_Kart;
        private float m_ShakeTimer;

        void Start()
        {
            if (KartTarget == null)
            {
                Debug.LogError("[CockpitCamera] Geen KartTarget ingesteld! Sleep de speler-kart naar het KartTarget veld.");
                return;
            }

            m_Kart = KartTarget.GetComponent<ArcadeKart>();
            m_CurrentRot = KartTarget.rotation;

            // Camera direct op startpositie zetten (geen lag bij begin)
            transform.position = KartTarget.TransformPoint(CockpitOffset);
            transform.rotation = KartTarget.rotation;
        }

        void LateUpdate()
        {
            if (KartTarget == null) return;

            // Doelpositie berekenen (cockpit offset in lokale ruimte van de kart)
            Vector3 targetPosition = KartTarget.TransformPoint(CockpitOffset);

            // Camera shake op basis van snelheid
            if (m_Kart != null)
            {
                float speed = m_Kart.Rigidbody != null ? m_Kart.Rigidbody.velocity.magnitude : 0f;
                float shake = ShakeAmount * (speed / 10f);
                targetPosition += new Vector3(
                    Random.Range(-shake, shake),
                    Random.Range(-shake * 0.5f, shake * 0.5f),
                    0f
                );
            }

            // Soepele positie
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref m_CurrentVelocity,
                PositionSmoothing
            );

            // Soepele rotatie (kijkt altijd dezelfde richting als de kart)
            m_CurrentRot = Quaternion.Slerp(
                m_CurrentRot,
                KartTarget.rotation,
                1f - Mathf.Pow(RotationSmoothing, Time.deltaTime * 60f)
            );
            transform.rotation = m_CurrentRot;
        }
    }
}
