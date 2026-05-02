using UnityEngine;
using KartGame.KartSystems;

public class NitroController : MonoBehaviour
{
    [Header("Nitro Instellingen")]
    public KeyCode nitroToets = KeyCode.N;
    public float nitroTopSpeedBoost = 30f;
    public float nitroAccelerationBoost = 5f;
    public float nitroDuur = 3f;

    private ArcadeKart kart;
    private bool nitroActief = false;

    void Start()
    {
        kart = GetComponent<ArcadeKart>();
    }

    void Update()
    {
        if (Input.GetKeyDown(nitroToets) && !nitroActief)
        {
            StartNitro();
        }
    }

    void StartNitro()
    {
        nitroActief = true;

        var powerup = new ArcadeKart.StatPowerup
        {
            PowerUpID = "Nitro",
            MaxTime = nitroDuur,
            ElapsedTime = 0f,
            modifiers = new ArcadeKart.Stats
            {
                TopSpeed = nitroTopSpeedBoost,
                Acceleration = nitroAccelerationBoost,
            }
        };

        kart.AddPowerup(powerup);
        Debug.Log("NITRO ACTIEF!");

        Invoke(nameof(StopNitro), nitroDuur);
    }

    void StopNitro()
    {
        nitroActief = false;
        Debug.Log("Nitro gestopt.");
    }
}