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

        ArcadeKart kart;

        void Start()
        {
            kart = GetComponent<ArcadeKart>();
        }

        void Update()
        {
            if (Input.GetKeyDown(JumpKey) && IsGrounded())
            {
                kart.Rigidbody.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            }
        }

        bool IsGrounded()
        {
            // Raycast recht naar beneden, geen layer nodig
            return Physics.Raycast(transform.position, Vector3.down, GroundCheckDistance);
        }
    }
}
