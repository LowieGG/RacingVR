using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KartGame.KartSystems
{
    [RequireComponent(typeof(ArcadeKart))]
    public class KartJump : MonoBehaviour
    {
        public float JumpForce = 1500f;
        public float GroundCheckDistance = 1.3f;
        public KeyCode JumpKey = KeyCode.Space;

        private ArcadeKart kart;
        private ESP32Manager esp32;
        private bool vorigeJumpStatus = false;

        void Start()
        {
            kart = GetComponent<ArcadeKart>();
            esp32 = FindObjectOfType<ESP32Manager>();
        }

        void Update()
        {
            bool huidigeStatus = esp32 != null && esp32.jumpIngedrukt;
            bool risingEdge = huidigeStatus && !vorigeJumpStatus;

            if ((risingEdge || Input.GetKeyDown(JumpKey)) && IsGrounded())
            {
                kart.Rigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
                Debug.Log("JUMP!");
            }

            vorigeJumpStatus = huidigeStatus;
        }

        bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance);
        }
    }
}