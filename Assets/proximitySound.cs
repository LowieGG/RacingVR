using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ExplodingSheep : MonoBehaviour
{
    [Header("Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    
    [Header("Audio Settings")]
    public float explosionVolume = 2f;  // Louder than default
    public float deepBassVolume = 1.2f; // Bass layer volume
    public float deepBassPitch = 0.5f;  // Lower pitch for deep sound
    
    private AudioSource _audioSource;
    private bool _isShattered = false;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Safety: Ensure it doesn't play the moment the game starts
        _audioSource.Stop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only trigger if hit by the Player and we haven't exploded yet
        if (collision.gameObject.CompareTag("Player") && !_isShattered)
        {
            Shatter();
        }
    }

    void Shatter()
    {
        _isShattered = true;

        // 1. Sound Logic
        // Play main explosion sound (loud)
        if (_audioSource.clip != null)
        {
            AudioSource.PlayClipAtPoint(_audioSource.clip, transform.position, explosionVolume);
            
            // Add deep bass layer for more impact
            PlayDeepBassLayer();
        }

        // 2. Shatter Logic
        // Loop through all children (legs, head, etc.)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            
            // Detach child from the parent
            child.parent = null;

            // Ensure the piece has physics
            Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = child.gameObject.AddComponent<Rigidbody>();
            }

            // Ensure the piece has a collider so it hits the floor
            if (child.gameObject.GetComponent<Collider>() == null)
            {
                child.gameObject.AddComponent<BoxCollider>();
            }

            // Launch the piece outward
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            
            // Cleanup: Remove the pieces after 5 seconds to save performance
            Destroy(child.gameObject, 5f);
        }

        // 3. Destroy the main sheep container
        Destroy(gameObject);
    }

    void PlayDeepBassLayer()
    {
        // Create a temporary audio source for the deep bass layer
        GameObject bassObject = new GameObject("ExplosionBass");
        bassObject.transform.position = transform.position;
        
        AudioSource bassSource = bassObject.AddComponent<AudioSource>();
        bassSource.clip = _audioSource.clip;
        bassSource.pitch = deepBassPitch;        // Lower pitch = deeper sound
        bassSource.volume = deepBassVolume;
        bassSource.Play();
        
        // Destroy after the sound finishes playing
        float soundDuration = _audioSource.clip.length / deepBassPitch;
        Destroy(bassObject, soundDuration + 0.1f);
    }
}