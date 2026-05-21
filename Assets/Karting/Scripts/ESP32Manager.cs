using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ESP32Manager : MonoBehaviour
{
    [Header("Instellingen")]
    public string comPoort = "COM3";
    public int baudRate = 115200;

    [Header("Audio Instellingen")]
    public AudioClip nitroSound;
    public AudioClip smsSound;
    public AudioClip ejectSound;
    public AudioClip honkSound;
    public AudioClip jumpSound;
    public AudioClip lichtenSound;
    public AudioClip wiperSound;
    public AudioClip laserSound;
    public AudioClip gasSound;
    public AudioClip remSound;
    
    private AudioSource audioSource;

    private SerialPort serialPort;
    private Thread leesThread;
    private string laasteData = "";
    private bool draait = false;

    public bool nitroIngedrukt = false;
    public bool smsKnopIngedrukt = false;
    public bool ejectIngedrukt = false;
    public bool honkIngedrukt = false;
    public bool jumpIngedrukt = false;
    public bool lichtenIngedrukt = false;
    public bool wiperSchakelaar = false;
    public bool laserIngedrukt = false;
    public bool gasIngedrukt = false;
    public bool remIngedrukt = false;

    // Vorige staat van knoppen voor edge detection
    private bool vorige_nitro = false;
    private bool vorige_sms = false;
    private bool vorige_eject = false;
    private bool vorige_honk = false;
    private bool vorige_jump = false;
    private bool vorige_lichten = false;
    private bool vorige_wiper = false;
    private bool vorige_laser = false;
    private bool vorige_gas = false;
    private bool vorige_rem = false;

    // Rotary encoder - later toevoegen
    public float stuurHoek = 0f;

    void Start()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        serialPort = new SerialPort(comPoort, baudRate);
        serialPort.Open();
        draait = true;

        leesThread = new Thread(LeesData);
        leesThread.Start();

        Debug.Log("ESP32 verbonden op " + comPoort);
    }

    void LeesData()
    {
        while (draait)
        {
            try
            {
                string data = serialPort.ReadLine();
                laasteData = data;
            }
            catch { }
        }
    }

    void Update()
    {
        // NITRO
        if (laasteData.Contains("NITRO:1"))
            nitroIngedrukt = true;
        else if (laasteData.Contains("NITRO:0"))
            nitroIngedrukt = false;
        
        if (nitroIngedrukt && !vorige_nitro && nitroSound != null)
            audioSource.PlayOneShot(nitroSound);
        vorige_nitro = nitroIngedrukt;

        // SMS
        if (laasteData.Contains("SMS:1"))
            smsKnopIngedrukt = true;
        else if (laasteData.Contains("SMS:0"))
            smsKnopIngedrukt = false;
        
        if (smsKnopIngedrukt && !vorige_sms && smsSound != null)
            audioSource.PlayOneShot(smsSound);
        vorige_sms = smsKnopIngedrukt;

        // EJECT
        if (laasteData.Contains("EJECT:1"))
            ejectIngedrukt = true;
        else if (laasteData.Contains("EJECT:0"))
            ejectIngedrukt = false;
        
        if (ejectIngedrukt && !vorige_eject && ejectSound != null)
            audioSource.PlayOneShot(ejectSound);
        vorige_eject = ejectIngedrukt;

        // HONK
        if (laasteData.Contains("HONK:1"))
            honkIngedrukt = true;
        else if (laasteData.Contains("HONK:0"))
            honkIngedrukt = false;
        
        if (honkIngedrukt && !vorige_honk && honkSound != null)
            audioSource.PlayOneShot(honkSound);
        vorige_honk = honkIngedrukt;

        // JUMP
        if (laasteData.Contains("JUMP:1"))
            jumpIngedrukt = true;
        else if (laasteData.Contains("JUMP:0"))
            jumpIngedrukt = false;
        
        if (jumpIngedrukt && !vorige_jump && jumpSound != null)
            audioSource.PlayOneShot(jumpSound);
        vorige_jump = jumpIngedrukt;

        // LICHTEN
        if (laasteData.Contains("LICHT:1"))
            lichtenIngedrukt = true;
        else if (laasteData.Contains("LICHT:0"))
            lichtenIngedrukt = false;
        
        if (lichtenIngedrukt && !vorige_lichten && lichtenSound != null)
            audioSource.PlayOneShot(lichtenSound);
        vorige_lichten = lichtenIngedrukt;

        // WIPER
        if (laasteData.Contains("WIPER:1"))
            wiperSchakelaar = true;
        else if (laasteData.Contains("WIPER:0"))
            wiperSchakelaar = false;
        
        if (wiperSchakelaar && !vorige_wiper && wiperSound != null)
            audioSource.PlayOneShot(wiperSound);
        vorige_wiper = wiperSchakelaar;

        // LASER
        if (laasteData.Contains("LASER:1"))
            laserIngedrukt = true;
        else if (laasteData.Contains("LASER:0"))
            laserIngedrukt = false;
        
        if (laserIngedrukt && !vorige_laser && laserSound != null)
            audioSource.PlayOneShot(laserSound);
        vorige_laser = laserIngedrukt;

        // GAS
        if (laasteData.Contains("GAS:1"))
            gasIngedrukt = true;
        else if (laasteData.Contains("GAS:0"))
            gasIngedrukt = false;
        
        if (gasIngedrukt && !vorige_gas && gasSound != null)
            audioSource.PlayOneShot(gasSound);
        vorige_gas = gasIngedrukt;

        // REM
        if (laasteData.Contains("REM:1"))
            remIngedrukt = true;
        else if (laasteData.Contains("REM:0"))
            remIngedrukt = false;
        
        if (remIngedrukt && !vorige_rem && remSound != null)
            audioSource.PlayOneShot(remSound);
        vorige_rem = remIngedrukt;

        // Rotary encoder - later toevoegen
        // if (laasteData.Contains("STUUR:"))
        // {
        //     string[] delen = laasteData.Split(',');
        //     foreach (string deel in delen)
        //     {
        //         if (deel.StartsWith("STUUR:"))
        //         {
        //             float.TryParse(deel.Replace("STUUR:", ""), out stuurHoek);
        //         }
        //     }
        // }
    }

    void OnDisable()
    {
        draait = false;
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }

    void OnApplicationQuit()
    {
        draait = false;
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }

    public void StuurVibratie()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.WriteLine("VIBRATE");
            Debug.Log("Vibratie gestuurd!");
        }
    }
}