using UnityEngine;

public class WiperController : MonoBehaviour
{
    public float sweepAngle = 90f;
    public float speed = 0.1f; // Trager

    private float timer = 0f;
    private Quaternion startRot;
    private ESP32Manager esp32;

    void Start()
    {
        startRot = transform.localRotation;
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void LateUpdate()
    {
        var r = GetComponentInChildren<MeshRenderer>();
        if (r != null) r.enabled = true;
    }

    void Update()
    {
        // Lees schakelaar direct (geen toggle, gewoon aan/uit)
        bool wiperAan = false;

        if (esp32 != null)
            wiperAan = esp32.wiperSchakelaar;

        // Keyboard backup
        if (Input.GetKey(KeyCode.V))
            wiperAan = true;

        if (wiperAan)
        {
            timer += Time.deltaTime * speed ;
            float t = Mathf.PingPong(timer, 1f);
            float angle = -t * sweepAngle;
            transform.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            // Rustig terugkeren naar startpositie
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                startRot,
                Time.deltaTime * 2f
            );
            // Reset timer zodat hij altijd vanaf begin start
            timer = 0f;
        }
    }
}