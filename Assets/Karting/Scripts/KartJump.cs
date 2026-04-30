using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace KartGame.KartSystems
{
    [RequireComponent(typeof(ArcadeKart))]
    public class KartJump : MonoBehaviour
    {
        public float JumpForce = 1500f;
        public float GroundCheckDistance = 1.3f;
        public KeyCode JumpKey = KeyCode.Space;
        public XRNode JumpControllerNode = XRNode.RightHand;
        public QuestControllerButton JumpButton = QuestControllerButton.PrimaryButton;
        [Range(0f, 1f)] public float ButtonThreshold = 0.2f;

        ArcadeKart kart;
        InputDevice jumpController;
        bool jumpWasPressed;

        void Start()
        {
            kart = GetComponent<ArcadeKart>();
        }

        void Update()
        {
            if ((Input.GetKeyDown(JumpKey) || GetQuestJumpDown()) && IsGrounded())
            {
                kart.Rigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            }
        }

        bool GetQuestJumpDown()
        {
            if (!jumpController.isValid)
            {
                jumpController = InputDevices.GetDeviceAtXRNode(JumpControllerNode);
            }

            bool jumpPressed = QuestControllerButtonUtility.IsPressed(jumpController, JumpButton, ButtonThreshold);
            bool jumpDown = jumpPressed && !jumpWasPressed;
            jumpWasPressed = jumpPressed;
            return jumpDown;
        }

        bool IsGrounded()
        {
            // Raycast recht naar beneden, geen layer nodig
            return Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance);
        }
    }
}
