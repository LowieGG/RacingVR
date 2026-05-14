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
        public float GroundCheckRadius = 0.35f;
        public float GroundCheckStartOffset = 0.25f;
        public float JumpBufferTime = 0.2f;
        public float CoyoteTime = 0.12f;
        public KeyCode JumpKey = KeyCode.Space;
        public XRNode JumpControllerNode = XRNode.RightHand;
        public QuestControllerButton JumpButton = QuestControllerButton.PrimaryButton;
        [Range(0f, 1f)] public float ButtonThreshold = 0.2f;

        ArcadeKart kart;
        InputDevice jumpController;
        bool jumpWasPressed;
        float lastJumpRequestTime = -999f;
        float lastGroundedTime = -999f;

        void Start()
        {
            kart = GetComponent<ArcadeKart>();
        }

        void Update()
        {
            if (Input.GetKeyDown(JumpKey) || GetQuestJumpDown())
            {
                lastJumpRequestTime = Time.time;
            }
        }

        void FixedUpdate()
        {
            if (IsGrounded())
                lastGroundedTime = Time.time;

            bool jumpBuffered = Time.time - lastJumpRequestTime <= JumpBufferTime;
            bool recentlyGrounded = Time.time - lastGroundedTime <= CoyoteTime;
            if (jumpBuffered && recentlyGrounded)
            {
                lastJumpRequestTime = -999f;
                lastGroundedTime = -999f;
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
            // Ignore triggers and the kart's own colliders; cockpit visuals/buttons should not count as ground.
            Vector3 origin = transform.position + Vector3.up * GroundCheckStartOffset;
            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                GroundCheckRadius,
                Vector3.down,
                GroundCheckDistance + GroundCheckStartOffset,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                    continue;

                if (hitCollider.attachedRigidbody == kart.Rigidbody)
                    continue;

                if (hitCollider.transform.IsChildOf(transform))
                    continue;

                return true;
            }

            return false;
        }
    }
}
