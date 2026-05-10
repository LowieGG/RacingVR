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
    public bool schakelaarIngedrukt = false;

    // Publieke variabelen die andere scripts kunnen lezen
    public bool nitroIngedrukt = false;

    void Start()
    {
        // Open de seriële poort
        serialPort = new SerialPort(comPoort, baudRate);
        serialPort.Open();
        draait = true;

        // Start een aparte thread om te lezen
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
        // Verwerk de data van ESP32
        if (laasteData.Contains("NITRO:1"))
            nitroIngedrukt = true;
        else if (laasteData.Contains("NITRO:0"))
            nitroIngedrukt = false;

        if (laasteData.Contains("SWITCH:1"))
            schakelaarIngedrukt = true;
        else if (laasteData.Contains("SWITCH:0"))
            schakelaarIngedrukt = false;
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

    void OnDisable()
    {
        draait = false;
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }

}