using System.Collections;
using UnityEngine;

/// <summary>
/// Toggle voor het stuur. Verplaatst de Cube child omhoog/omlaag
/// en flitst even groen (net als de Knop buttons), aangestuurd via toetsenbord.
///
/// Zet dit script op het Toggle GameObject.
/// De Cube child wordt automatisch gevonden als je niks instelt.
/// </summary>
public class StuurToggle : MonoBehaviour
{
    [Header("Onderdelen")]
    [Tooltip("De Cube onder Toggle die beweegt. Wordt automatisch gevonden als leeg.")]
    public Transform cube;

    [Header("Posities (local Y van de Cube)")]
    [Tooltip("Y-positie van de Cube wanneer toggle UIT staat")]
    public float yPositieUit =  0.00072f;
    [Tooltip("Y-positie van de Cube wanneer toggle AAN staat")]
    public float yPositieAan = -0.00107f;

    [Header("Animatie")]
    [Tooltip("Kleur die kort oplicht bij schakelen")]
    public Color flashKleur = Color.green;
    [Tooltip("Hoe lang (seconden) de groene flash duurt")]
    public float flashDuur = 0.2f;

    [Header("Toetsenbord")]
    [Tooltip("Welke toets schakelt de toggle")]
    public KeyCode toggleToets = KeyCode.T;

    // ── intern ──────────────────────────────────────────────────────
    private bool isAan = false;

    private Renderer cubeRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int kleurPropertyId;
    private Color origineleKleur = Color.white;
    private bool heeftKleurProperty = false;

    private Coroutine animatie;

    // ────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Zoek Cube automatisch als niet ingesteld
        if (cube == null)
            cube = transform.Find("Cube");

        if (cube == null)
        {
            Debug.LogWarning("[StuurToggle] Geen 'Cube' child gevonden op " + gameObject.name);
            return;
        }

        // Startpositie garanderen (toggle staat UIT bij start)
        ZetCubeY(yPositieUit);

        // Renderer ophalen
        cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer == null)
            cubeRenderer = cube.GetComponentInChildren<Renderer>(true);

        propertyBlock = new MaterialPropertyBlock();
        ResolveKleurProperty();
    }

    // ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (Input.GetKeyDown(toggleToets))
            Schakel();
    }

    // ────────────────────────────────────────────────────────────────
    /// <summary>Schakelt de toggle (ook aanroepbaar door andere scripts).</summary>
    public void Schakel()
    {
        isAan = !isAan;
        ZetCubeY(isAan ? yPositieAan : yPositieUit);

        if (animatie != null)
            StopCoroutine(animatie);
        animatie = StartCoroutine(FlashGroen());
    }

    /// <summary>Geeft de huidige toestand terug (true = AAN).</summary>
    public bool IsAan => isAan;

    // ────────────────────────────────────────────────────────────────
    // Hulpfuncties
    // ────────────────────────────────────────────────────────────────
    private void ZetCubeY(float y)
    {
        if (cube == null) return;
        Vector3 pos = cube.localPosition;
        pos.y = y;
        cube.localPosition = pos;
    }

    private IEnumerator FlashGroen()
    {
        ZetRendererKleur(flashKleur);
        yield return new WaitForSeconds(flashDuur);
        ZetRendererKleur(origineleKleur);
        animatie = null;
    }

    private void ResolveKleurProperty()
    {
        if (cubeRenderer == null) return;

        Material mat = cubeRenderer.sharedMaterial;
        if (mat == null) return;

        if (mat.HasProperty("_BaseColor"))
        {
            kleurPropertyId = Shader.PropertyToID("_BaseColor");
            origineleKleur = mat.GetColor(kleurPropertyId);
            heeftKleurProperty = true;
        }
        else if (mat.HasProperty("_Color"))
        {
            kleurPropertyId = Shader.PropertyToID("_Color");
            origineleKleur = mat.GetColor(kleurPropertyId);
            heeftKleurProperty = true;
        }
    }

    private void ZetRendererKleur(Color kleur)
    {
        if (cubeRenderer == null || !heeftKleurProperty) return;
        cubeRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(kleurPropertyId, kleur);
        cubeRenderer.SetPropertyBlock(propertyBlock);
    }

    void OnDisable()
    {
        // Herstel visueel als het object uitgeschakeld wordt
        if (animatie != null)
        {
            StopCoroutine(animatie);
            animatie = null;
        }
        ZetRendererKleur(origineleKleur);
        ZetCubeY(yPositieUit);
        isAan = false;
    }
}
