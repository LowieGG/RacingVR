using UnityEngine;

public class KartHonk : MonoBehaviour
{
    public AudioSource audioSource;   // Assign in Inspector
    public AudioClip honkSound;       // Assign your honk clip

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Honk();
        }
    }

    void Honk()
    {
        if (audioSource != null && honkSound != null)
        {
            audioSource.PlayOneShot(honkSound);
        }
    }
}