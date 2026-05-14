using UnityEngine;

namespace KartGame.KartSystems
{
    public class ESP32Input : BaseInput
    {
        private ESP32Manager esp32;

        void Start()
        {
            esp32 = FindObjectOfType<ESP32Manager>();
        }

        public override InputData GenerateInput()
        {
            if (esp32 == null)
                return new InputData();

            return new InputData
            {
                Accelerate = esp32.gasIngedrukt,
                Brake = esp32.remIngedrukt,
                // TurnInput = esp32.stuurHoek / 100f  // Rotary encoder - later toevoegen
                TurnInput = Input.GetAxis("Horizontal") // Tijdelijk keyboard sturen
            };
        }
    }
}
