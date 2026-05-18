using UnityEngine;

public class KartCollisionSound : MonoBehaviour
{
    [Header("Audio Instellingen")]
    public AudioClip collisionSound;
    public float minCollisionForce = 1f; // Minimale kracht om geluid af te spelen
    public float soundCooldown = 0.2f; // Minimale tijd tussen geluidjes

    private AudioSource audioSource;
    private float lastCollisionTime = 0f;
    private Rigidbody kartRigidbody;

    void Start()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // Get Rigidbody component
        kartRigidbody = GetComponent<Rigidbody>();
        if (kartRigidbody == null)
        {
            Debug.LogWarning("KartCollisionSound: Geen Rigidbody gevonden op " + gameObject.name);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Controleer of genoeg tijd is verstreken sinds laatste botsing
        if (Time.time - lastCollisionTime < soundCooldown)
            return;

        // Bereken de botsingssnelheid
        float collisionForce = 0f;
        if (kartRigidbody != null)
        {
            collisionForce = kartRigidbody.velocity.magnitude;
        }

        // Speel geluid af als kracht groot genoeg is
        if (collisionForce >= minCollisionForce && collisionSound != null)
        {
            // Varieer het volume op basis van botsingssterkte
            float volume = Mathf.Clamp01(collisionForce / 10f); // Deel door 10 voor normalisering
            audioSource.PlayOneShot(collisionSound, volume);

            lastCollisionTime = Time.time;

            Debug.Log($"Botsing gedetecteerd! Kracht: {collisionForce:F2}, Volume: {volume:F2}");
        }
    }

    // Optioneel: Ook OnCollisionStay voor voortdurende contact
    void OnCollisionStay(Collision collision)
    {
        // Dit zorgt ervoor dat herhaalde geluiden worden afgespeeld
        // tijdens voortdurende contact (niet nodig als je slechts 1 geluid wilt)
    }
}