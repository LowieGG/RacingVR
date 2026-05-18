using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ExplodingSheep : MonoBehaviour
{
    [Header("Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    
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
        // We use PlayClipAtPoint so the sound continues even after the sheep is destroyed
        if (_audioSource.clip != null)
        {
            AudioSource.PlayClipAtPoint(_audioSource.clip, transform.position);
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
}