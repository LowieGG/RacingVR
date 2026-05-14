using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ESP32Manager : MonoBehaviour
{
    [Header("Instellingen")]
    public string comPoort = "COM3";
    public int baudRate = 9600;

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

    // Rotary encoder - later toevoegen
    public float stuurHoek = 0f;

    void Start()
    {
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
        if (laasteData.Contains("NITRO:1"))
            nitroIngedrukt = true;
        else if (laasteData.Contains("NITRO:0"))
            nitroIngedrukt = false;

        if (laasteData.Contains("SMS:1"))
            smsKnopIngedrukt = true;
        else if (laasteData.Contains("SMS:0"))
            smsKnopIngedrukt = false;

        if (laasteData.Contains("EJECT:1"))
            ejectIngedrukt = true;
        else if (laasteData.Contains("EJECT:0"))
            ejectIngedrukt = false;

        if (laasteData.Contains("HONK:1"))
            honkIngedrukt = true;
        else if (laasteData.Contains("HONK:0"))
            honkIngedrukt = false;

        if (laasteData.Contains("JUMP:1"))
            jumpIngedrukt = true;
        else if (laasteData.Contains("JUMP:0"))
            jumpIngedrukt = false;

        if (laasteData.Contains("LICHT:1"))
            lichtenIngedrukt = true;
        else if (laasteData.Contains("LICHT:0"))
            lichtenIngedrukt = false;

        if (laasteData.Contains("WIPER:1"))
            wiperSchakelaar = true;
        else if (laasteData.Contains("WIPER:0"))
            wiperSchakelaar = false;

        if (laasteData.Contains("LASER:1"))
            laserIngedrukt = true;
        else if (laasteData.Contains("LASER:0"))
            laserIngedrukt = false;

        if (laasteData.Contains("GAS:1"))
            gasIngedrukt = true;
        else if (laasteData.Contains("GAS:0"))
            gasIngedrukt = false;

        if (laasteData.Contains("REM:1"))
            remIngedrukt = true;
        else if (laasteData.Contains("REM:0"))
            remIngedrukt = false;

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