using UnityEngine;
using System.Collections;

public class ChairEjection : MonoBehaviour
{
    public Transform carSeat;        // where the camera returns to
    public KeyCode ejectKey = KeyCode.E;

    public float launchForce = 15f;
    public float returnSpeed = 5f;

    private Camera mainCam;
    private bool isEjected = false;
    private Vector3 velocity;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(ejectKey) && !isEjected)
        {
            StartCoroutine(Eject());
        }
    }

    IEnumerator Eject()
    {
        isEjected = true;

        // Detach camera from car
        mainCam.transform.SetParent(null);
        velocity = Vector3.up * launchForce;

        // Launch phase — apply gravity manually
        while (mainCam.transform.position.y > carSeat.position.y || velocity.y > 0)
        {
            velocity += Physics.gravity * Time.deltaTime;
            mainCam.transform.position += velocity * Time.deltaTime;
            yield return null;
        }

        // Return phase — lerp back to seat
        while (Vector3.Distance(mainCam.transform.position, carSeat.position) > 0.05f)
        {
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, carSeat.position, returnSpeed * Time.deltaTime);
            mainCam.transform.rotation = Quaternion.Lerp(mainCam.transform.rotation, carSeat.rotation, returnSpeed * Time.deltaTime);
            yield return null;
        }

        // Snap back and re-attach to car
        mainCam.transform.position = carSeat.position;
        mainCam.transform.rotation = carSeat.rotation;
        mainCam.transform.SetParent(carSeat);

        isEjected = false;
    }
}