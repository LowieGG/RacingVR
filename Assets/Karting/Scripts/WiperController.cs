using UnityEngine;

public class WiperController : MonoBehaviour
{
    public float sweepAngle = 90f;
    public float speed = 2f;

    float controlValue = 0f;
    float timer = 0f;
    Quaternion startRot;

    void Start()
    {
        startRot = transform.localRotation;
        timer = 0f;
    }

    void LateUpdate()
    {
        var r = GetComponentInChildren<MeshRenderer>();
        if (r != null) r.enabled = true;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.V))
            controlValue += Time.deltaTime * 1.5f;

        if (Input.GetKey(KeyCode.C))
            controlValue -= Time.deltaTime * 1.5f;

        controlValue = Mathf.Clamp(controlValue, 0f, 1f); // enkel positief, snelheid 0..1

        timer += Time.deltaTime * speed * controlValue * 5f;

        float t = Mathf.PingPong(timer, 1f);
        float angle = -t * sweepAngle;

        transform.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);
    }
}