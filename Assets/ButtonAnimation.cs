using System.Collections;
using UnityEngine;

public class ButtonAnimation : MonoBehaviour
{
    public enum BewegingsAs
    {
        LocalX,
        LocalY,
        LocalZ
    }

    [Header("Identificatie")]
    public int knopNummer = -1;

    [Header("Onderdelen")]
    public Transform knopTop;
    public Renderer knopRenderer;
    public Material knopMateriaal;

    [Header("Instellingen")]
    public Color indrukKleur = Color.green;
    public float wachttijd = 0.2f;
    public BewegingsAs bewegingsAs = BewegingsAs.LocalZ;
    public float ingedrukteLocalWaarde = -0.000522f;
    public float ingedrukteLocalZ = -0.000522f;
    public bool activeerMetTrigger = false;

    private Color origineleKleur;
    private Vector3 originelePositie;
    private Coroutine animatie;
    private MaterialPropertyBlock propertyBlock;
    private int kleurPropertyId;
    private bool heeftKleurProperty;

    void Awake()
    {
        VernieuwStartWaarden();
    }

    public void VernieuwStartWaarden()
    {
        if (knopTop == null)
        {
            Transform cubeChild = transform.Find("Cube");
            Transform cylinderChild = transform.Find("Cylinder");
            knopTop = cylinderChild != null ? cylinderChild : cubeChild != null ? cubeChild : transform;
        }

        if (IsToggleObject())
        {
            bewegingsAs = BewegingsAs.LocalY;
            ingedrukteLocalWaarde = 0.00039f;
        }

        originelePositie = knopTop.localPosition;

        if (knopRenderer == null)
            knopRenderer = knopTop.GetComponentInChildren<Renderer>(true);

        propertyBlock = new MaterialPropertyBlock();
        ResolveKleurProperty();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activeerMetTrigger)
            DrukIn();
    }

    void OnDisable()
    {
        ResetVisueel();
    }

    public void DrukIn()
    {
        if (animatie != null)
            StopCoroutine(animatie);

        animatie = StartCoroutine(DrukKnopIn());
    }

    public bool KomtOvereenMetKnopNummer(int nummer)
    {
        if (nummer < 0)
            return false;

        if (knopNummer == nummer)
            return true;

        if (NaamLijktOpKnopNummer(gameObject.name, nummer))
            return true;

        if (knopTop != null && NaamLijktOpKnopNummer(knopTop.name, nummer))
            return true;

        Transform parent = transform.parent;
        while (parent != null)
        {
            if (NaamLijktOpKnopNummer(parent.name, nummer))
                return true;

            parent = parent.parent;
        }

        return false;
    }

    private IEnumerator DrukKnopIn()
    {
        if (knopTop != null)
            knopTop.localPosition = MaakIngedruktePositie();

        ZetRendererKleur(indrukKleur);

        yield return new WaitForSeconds(wachttijd);

        if (knopTop != null)
            knopTop.localPosition = originelePositie;

        ZetRendererKleur(origineleKleur);

        animatie = null;
    }

    private void ResetVisueel()
    {
        if (animatie != null)
        {
            StopCoroutine(animatie);
            animatie = null;
        }

        if (knopTop != null)
            knopTop.localPosition = originelePositie;

        ZetRendererKleur(origineleKleur);
    }

    private Vector3 MaakIngedruktePositie()
    {
        float doelWaarde = Mathf.Approximately(ingedrukteLocalWaarde, -0.000522f)
            ? ingedrukteLocalZ
            : ingedrukteLocalWaarde;

        switch (bewegingsAs)
        {
            case BewegingsAs.LocalX:
                return new Vector3(doelWaarde, originelePositie.y, originelePositie.z);
            case BewegingsAs.LocalY:
                return new Vector3(originelePositie.x, doelWaarde, originelePositie.z);
            default:
                return new Vector3(originelePositie.x, originelePositie.y, doelWaarde);
        }
    }

    private void ResolveKleurProperty()
    {
        origineleKleur = Color.white;
        heeftKleurProperty = false;

        Material material = knopRenderer != null ? knopRenderer.sharedMaterial : knopMateriaal;
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
        {
            kleurPropertyId = Shader.PropertyToID("_BaseColor");
            origineleKleur = material.GetColor(kleurPropertyId);
            heeftKleurProperty = true;
            return;
        }

        if (material.HasProperty("_Color"))
        {
            kleurPropertyId = Shader.PropertyToID("_Color");
            origineleKleur = material.GetColor(kleurPropertyId);
            heeftKleurProperty = true;
        }
    }

    private void ZetRendererKleur(Color kleur)
    {
        if (knopRenderer == null || !heeftKleurProperty)
            return;

        knopRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(kleurPropertyId, kleur);
        knopRenderer.SetPropertyBlock(propertyBlock);
    }

    private static bool NaamLijktOpKnopNummer(string naam, int nummer)
    {
        if (string.IsNullOrEmpty(naam))
            return false;

        string n = nummer.ToString();
        string lower = naam.ToLowerInvariant();

        return lower == n ||
               lower.Contains("(" + n + ")") ||
               lower.Contains("[" + n + "]") ||
               lower.Contains("knop " + n) ||
               lower.Contains("knop_" + n) ||
               lower.Contains("knop-" + n) ||
               lower.Contains("knop" + n) ||
               lower.Contains("button " + n) ||
               lower.Contains("button_" + n) ||
               lower.Contains("button-" + n) ||
               lower.Contains("button" + n);
    }

    private static bool NaamLijktOpToggle(string naam)
    {
        return !string.IsNullOrEmpty(naam) && naam.ToLowerInvariant().Contains("toggle");
    }

    private bool IsToggleObject()
    {
        Transform current = transform;
        while (current != null)
        {
            if (NaamLijktOpToggle(current.name))
                return true;

            current = current.parent;
        }

        return false;
    }
}
