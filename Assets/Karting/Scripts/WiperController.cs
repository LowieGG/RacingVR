using UnityEngine;

public class WiperController : MonoBehaviour
{
    public float sweepAngle = 90f;
    public float speed = 0.3f;

    private float timer = 0f;
    private Quaternion startRot;
    private bool wiperAan = false;
    private ESP32Manager esp32;
    private bool vorigeWiperStatus = false;

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
        // Rising edge detectie voor toggle
        bool huidigeStatus = esp32 != null && esp32.wiperSchakelaar;
        bool risingEdge = huidigeStatus && !vorigeWiperStatus;

        if (risingEdge || Input.GetKeyDown(KeyCode.V))
        {
            wiperAan = !wiperAan;
            Debug.Log("Ruitenwisser: " + (wiperAan ? "AAN" : "UIT"));
        }

        vorigeWiperStatus = huidigeStatus;

        if (wiperAan)
        {
            timer += Time.deltaTime * speed * 2f;
            float t = Mathf.PingPong(timer, 1f);
            float angle = -t * sweepAngle;
            transform.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                startRot,
                Time.deltaTime * 2f
            );
            timer = 0f;
        }
    }
}