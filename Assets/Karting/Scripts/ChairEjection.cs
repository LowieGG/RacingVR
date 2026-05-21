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
    private Vector3 originalSeatPosition;
    private Quaternion originalSeatRotation;
    private Vector3 positionOffsetFromCar;
    private Quaternion rotationOffsetFromCar;

    void Start()
    {
        mainCam = Camera.main;
        esp32 = FindObjectOfType<ESP32Manager>();
        _audioSource = GetComponent<AudioSource>();

        // Debug: Log the starting positions
        Debug.Log($"Camera position: {mainCam.transform.position}");
        Debug.Log($"CarSeat position: {carSeat.position}");
        Debug.Log($"Camera parent: {mainCam.transform.parent.name}");
        Debug.Log($"CarSeat parent: {carSeat.parent.name}");

        // Store the initial offset from the car
        positionOffsetFromCar = carSeat.InverseTransformDirection(mainCam.transform.position - carSeat.position);
        rotationOffsetFromCar = Quaternion.Inverse(carSeat.rotation) * mainCam.transform.rotation;

        Debug.Log($"Position offset: {positionOffsetFromCar}");
        Debug.Log($"Rotation offset: {rotationOffsetFromCar}");
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.ejectIngedrukt;
        bool risingEdge = huidigeStatus && !vorigeEjectStatus;

        if ((risingEdge || Input.GetKeyDown(KeyCode.E)) && !isEjected)
        {
            StartCoroutine(Eject());
        }

        // Follow the car when not ejected
        if (!isEjected && carSeat != null)
        {
            mainCam.transform.position = carSeat.position + carSeat.TransformDirection(positionOffsetFromCar);
            mainCam.transform.rotation = carSeat.rotation * rotationOffsetFromCar;
        }

        vorigeEjectStatus = huidigeStatus;
    }

    IEnumerator Eject()
    {
        isEjected = true;

        // Store the world position and rotation before ejection
        originalSeatPosition = mainCam.transform.position;
        originalSeatRotation = mainCam.transform.rotation;

        // Play the assigned clip
        if (ejectSound != null)
        {
            _audioSource.PlayOneShot(ejectSound);
        }

        mainCam.transform.SetParent(null);
        velocity = Vector3.up * launchForce;

        // Launch phase
        while (mainCam.transform.position.y > originalSeatPosition.y || velocity.y > 0)
        {
            velocity += Physics.gravity * Time.deltaTime;
            mainCam.transform.position += velocity * Time.deltaTime;
            yield return null;
        }

        // Landing phase - return to the original world position
        while (Vector3.Distance(mainCam.transform.position, originalSeatPosition) > 0.05f)
        {
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, originalSeatPosition, returnSpeed * Time.deltaTime);
            mainCam.transform.rotation = Quaternion.Lerp(mainCam.transform.rotation, originalSeatRotation, returnSpeed * Time.deltaTime);
            yield return null;
        }

        // Ensure exact final position
        mainCam.transform.position = originalSeatPosition;
        mainCam.transform.rotation = originalSeatRotation;
        
        isEjected = false;
    }
}