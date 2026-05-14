using UnityEngine;

public class JumpscareObstacle : MonoBehaviour
{
    [Header("Instellingen")]
    public GameObject obstakel;          // Sleep het Cube object hierin
    public float verdwijntNa = 5f;       // Obstakel verdwijnt na X seconden

    private bool getriggerd = false;

    void OnTriggerEnter(Collider other)
    {
        // Controleer of het de kart is die erdoor rijdt
        if (!getriggerd && other.CompareTag("Player"))
        {
            getriggerd = true;
            obstakel.SetActive(true);    // Obstakel verschijnt!
            Debug.Log("JUMPSCARE!");

            // Laat obstakel na X seconden verdwijnen
            Invoke(nameof(VerbergObstakel), verdwijntNa);
        }
    }

    void VerbergObstakel()
    {
        obstakel.SetActive(false);
        getriggerd = false; // Kan opnieuw getriggerd worden
    }
}