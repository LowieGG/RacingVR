using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SheepHitSound : MonoBehaviour
{
    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    // This runs when a physics object bumps into this collider
    private void OnCollisionEnter(Collision collision)
    {
        // Optional: Check if the hitting object has a specific tag
        // if (collision.gameObject.CompareTag("Projectile")) 
        
        if (!_audioSource.isPlaying)
        {
            // Randomize pitch slightly so multiple hits don't sound robotic
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            _audioSource.Play();
        }
    }
}