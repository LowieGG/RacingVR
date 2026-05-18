using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ChairEjection : MonoBehaviour
{
    public Transform carSeat;
    public float launchForce = 15f;
    public float returnSpeed = 5f;
    public AudioClip ejectSound; // Drag your sound here in the Inspector

    private Camera mainCam;
    private bool isEjected = false;
    private Vector3 velocity;
    private ESP32Manager esp32;
    private bool vorigeEjectStatus = false;
    private AudioSource _audioSource;

    void Start()
    {
        mainCam = Camera.main;
        esp32 = FindObjectOfType<ESP32Manager>();
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.ejectIngedrukt;
        bool risingEdge = huidigeStatus && !vorigeEjectStatus;

        if ((risingEdge || Input.GetKeyDown(KeyCode.E)) && !isEjected)
        {
            StartCoroutine(Eject());
        }

        vorigeEjectStatus = huidigeStatus;
    }

    IEnumerator Eject()
    {
        isEjected = true;

        // Play the assigned clip
        if (ejectSound != null)
        {
            _audioSource.PlayOneShot(ejectSound);
        }

        mainCam.transform.SetParent(null);
        velocity = Vector3.up * launchForce;

        while (mainCam.transform.position.y > carSeat.position.y || velocity.y > 0)
        {
            velocity += Physics.gravity * Time.deltaTime;
            mainCam.transform.position += velocity * Time.deltaTime;
            yield return null;
        }

        while (Vector3.Distance(mainCam.transform.position, carSeat.position) > 0.05f)
        {
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, carSeat.position, returnSpeed * Time.deltaTime);
            mainCam.transform.rotation = Quaternion.Lerp(mainCam.transform.rotation, carSeat.rotation, returnSpeed * Time.deltaTime);
            yield return null;
        }

        mainCam.transform.position = carSeat.position;
        mainCam.transform.rotation = carSeat.rotation;
        mainCam.transform.SetParent(carSeat);
        isEjected = false;
    }
}