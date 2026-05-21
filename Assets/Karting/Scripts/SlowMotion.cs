using UnityEngine;

public class SlowMotion : MonoBehaviour
{
    [Header("Instellingen")]
    public float slowMotionScale = 0.3f;
    public float duurInEchteSeconden = 1f;
    public KeyCode slowMotionToets = KeyCode.M;

    private ESP32Manager esp32;
    private bool vorigeStatus = false;
    private bool bezig = false;
    private float timer = 0f;

    void Start()
    {
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.wiperSchakelaar;
        bool risingEdge = huidigeStatus && !vorigeStatus;

        if ((risingEdge || Input.GetKeyDown(slowMotionToets)) && !bezig)
        {
            StartSlowMotion();
        }

        if (bezig)
        {
            // Timer telt in echte tijd, niet game tijd!
            timer += Time.unscaledDeltaTime;
            if (timer >= duurInEchteSeconden)
            {
                StopSlowMotion();
            }
        }

        vorigeStatus = huidigeStatus;
    }

    void StartSlowMotion()
    {
        bezig = true;
        timer = 0f;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log("SLOW MOTION AAN!");
    }

    void StopSlowMotion()
    {
        bezig = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("SLOW MOTION UIT!");
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}