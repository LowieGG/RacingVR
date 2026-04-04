using UnityEngine;
using System.Collections.Generic;

namespace KartGame.KartSystems
{
    /// <summary>
    /// CockpitKartHider v3 - Verbergt ALLES van de speler-kart inclusief bestuurder.
    ///
    /// Voeg dit script toe aan het KartClassic_Player object.
    /// Het zoekt automatisch alle Renderers in het eigen object EN in de hele scene
    /// die bij de speler-kart horen.
    ///
    /// Als de bestuurder nog steeds zichtbaar is:
    ///   - Zet 'Force Hide All Skinned Meshes' aan in de Inspector
    /// </summary>
    public class CockpitKartHider : MonoBehaviour
    {
        [Header("Verbergen")]
        [Tooltip("Verberg de kart body en bestuurder.")]
        public bool HideKartBody = true;

        [Tooltip("Verberg ook de wielen.")]
        public bool HideWheels = true;

        [Header("Extra opties")]
        [Tooltip("Verberg ALLE SkinnedMeshRenderers in de hele scene (bestuurder-figuren). " +
                 "Zet dit aan als de bestuurder nog steeds zichtbaar is.")]
        public bool ForceHideAllSkinnedMeshes = true;

        [Tooltip("Verberg ook objecten die niet direct child zijn (bijv. losse karakter prefab).")]
        public bool SearchWholeScene = true;

        [Tooltip("Namen van objecten die je NIET wil verbergen.")]
        public string[] KeepVisible = new string[0];

        private List<Renderer> m_KartRenderers = new List<Renderer>();
        private List<SkinnedMeshRenderer> m_SceneSkinnedRenderers = new List<SkinnedMeshRenderer>();

        void Start()
        {
            CollectRenderers();
            ApplyVisibility();
        }

        void CollectRenderers()
        {
            m_KartRenderers.Clear();
            m_SceneSkinnedRenderers.Clear();

            // Alle renderers in dit object en kinderen
            var own = GetComponentsInChildren<Renderer>(includeInactive: true);
            m_KartRenderers.AddRange(own);

            // Zoek ook in de hele scene naar SkinnedMeshRenderers (bestuurder figuur)
            if (SearchWholeScene || ForceHideAllSkinnedMeshes)
            {
                var allSkinned = FindObjectsOfType<SkinnedMeshRenderer>(includeInactive: true);
                m_SceneSkinnedRenderers.AddRange(allSkinned);
            }
        }

        void ApplyVisibility()
        {
            // ── Verberg Template_Character en PlayerIdle direct op naam ───────
            // (de bekende bestuurder objecten in KartVisual)
            string[] characterObjectNames = { "Template_Character", "PlayerIdle", "Root1" };
            foreach (string objName in characterObjectNames)
            {
                var found = FindDeepChild(transform, objName);
                if (found != null)
                    found.gameObject.SetActive(!HideKartBody);
            }

            // ── Eigen kart renderers ──────────────────────────────────────────
            foreach (var r in m_KartRenderers)
            {
                if (r == null) continue;
                if (IsKeptVisible(r.gameObject.name)) continue;

                string n = r.gameObject.name.ToLower();
                bool isWheel = n.Contains("wheel") || n.Contains("tyre") || n.Contains("tire");

                r.enabled = isWheel ? !HideWheels : !HideKartBody;
            }

            // ── SkinnedMesh renderers (bestuurder karakter) ───────────────────
            if (ForceHideAllSkinnedMeshes)
            {
                foreach (var r in m_SceneSkinnedRenderers)
                {
                    if (r == null) continue;
                    if (IsKeptVisible(r.gameObject.name)) continue;
                    r.enabled = !HideKartBody;
                }
            }
        }

        // Zoekt recursief naar een child met de gegeven naam
        Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                var result = FindDeepChild(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        bool IsKeptVisible(string objName)
        {
            foreach (string k in KeepVisible)
                if (!string.IsNullOrEmpty(k) && objName.Contains(k)) return true;
            return false;
        }

        // Handige knoppen in Inspector (werken ook tijdens Play)
        public void ShowAll()
        {
            HideKartBody = false;
            HideWheels   = false;
            ApplyVisibility();
        }

        public void HideAll()
        {
            HideKartBody = true;
            HideWheels   = true;
            ApplyVisibility();
        }

        void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (m_KartRenderers.Count == 0) CollectRenderers();
            ApplyVisibility();
        }
    }
}
