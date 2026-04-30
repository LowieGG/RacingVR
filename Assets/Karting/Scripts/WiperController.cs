using UnityEngine;

public class WiperController : MonoBehaviour
{
    public float sweepAngle = 90f;
    public float speed = 2f;

    public bool isActive = false;

    float timer;
    Quaternion startRot;

    void Start()
    {
        startRot = transform.localRotation;

        // Zorg dat hij echt stil staat bij start
        timer = 0f;
        isActive = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            isActive = !isActive;

            // optioneel: reset timing bij uitschakelen
            if (!isActive)
            {
                timer = 0f;
                transform.localRotation = startRot;
            }
        }

        if (!isActive) return;

        timer += Time.deltaTime * speed;

        float angle = Mathf.Sin(timer) * sweepAngle;

        transform.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);
    }
}