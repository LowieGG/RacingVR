using UnityEngine;

namespace KartGame.KartSystems {

    public class KeyboardInput : BaseInput
    {
        [Tooltip("Leave this off for VR so gamepad/Quest sticks do not control the kart through the old Input Manager.")]
        public bool UseLegacyInputAxes;

        public string TurnInputName = "Horizontal";
        public string AccelerateButtonName = "Accelerate";
        public string BrakeButtonName = "Brake";

        public KeyCode AccelerateKey = KeyCode.UpArrow;
        public KeyCode AccelerateAltKey = KeyCode.W;
        public KeyCode BrakeKey = KeyCode.DownArrow;
        public KeyCode BrakeAltKey = KeyCode.S;
        public KeyCode TurnLeftKey = KeyCode.LeftArrow;
        public KeyCode TurnLeftAltKey = KeyCode.A;
        public KeyCode TurnRightKey = KeyCode.RightArrow;
        public KeyCode TurnRightAltKey = KeyCode.D;

        public override InputData GenerateInput() {
            if (UseLegacyInputAxes)
            {
                return new InputData
                {
                    Accelerate = Input.GetButton(AccelerateButtonName),
                    Brake = Input.GetButton(BrakeButtonName),
                    TurnInput = Input.GetAxis(TurnInputName)
                };
            }

            bool turnLeft = Input.GetKey(TurnLeftKey) || Input.GetKey(TurnLeftAltKey);
            bool turnRight = Input.GetKey(TurnRightKey) || Input.GetKey(TurnRightAltKey);

            return new InputData
            {
                Accelerate = Input.GetKey(AccelerateKey) || Input.GetKey(AccelerateAltKey),
                Brake = Input.GetKey(BrakeKey) || Input.GetKey(BrakeAltKey),
                TurnInput = (turnRight ? 1f : 0f) - (turnLeft ? 1f : 0f)
            };
        }
    }
}
