using UnityEngine;
using UnityEngine.XR;

namespace KartGame.KartSystems
{
    public class QuestSteeringWheelInput : BaseInput
    {
        public enum WheelAxis
        {
            LocalX,
            LocalY,
            LocalZ
        }

        [Header("Controller")]
        public XRNode ControllerNode = XRNode.RightHand;

        [Header("Steering")]
        [Tooltip("Controller local axis that points through the physical wheel axle.")]
        public WheelAxis SteeringAxis = WheelAxis.LocalZ;
        [Tooltip("Maximum physical wheel angle that maps to full game steering.")]
        public float MaxWheelAngle = 180f;
        [Tooltip("Small values around center are ignored.")]
        [Range(0f, 0.3f)] public float SteeringDeadZone = 0.04f;
        [Tooltip("Invert this if turning left/right is swapped.")]
        public bool InvertSteering;
        [Tooltip("Recalibrate automatically when tracking starts.")]
        public bool CalibrateOnStart = true;

        [Header("Buttons")]
        [Tooltip("Right trigger by default: drive forward.")]
        public QuestControllerButton AccelerateButton = QuestControllerButton.Trigger;
        [Tooltip("Right grip by default: brake / reverse.")]
        public QuestControllerButton BrakeButton = QuestControllerButton.Grip;
        [Tooltip("B/Y by default: hold wheel straight and press to recalibrate.")]
        public QuestControllerButton RecalibrateButton = QuestControllerButton.SecondaryButton;
        [Tooltip("Thumbstick click by default: honk.")]
        public QuestControllerButton HornButton = QuestControllerButton.Primary2DAxisClick;
        [Range(0f, 1f)] public float ButtonThreshold = 0.2f;

        [Header("Horn")]
        public AudioSource HornAudioSource;
        public AudioClip HornClip;

        [Header("Virtual Wheel Visual")]
        [Tooltip("Optional visual steering wheel mesh in the cockpit. It rotates from its starting local rotation.")]
        public Transform VirtualWheel;
        public WheelAxis VirtualWheelAxis = WheelAxis.LocalZ;
        public bool InvertVirtualWheel;
        [Tooltip("Use this when the virtual wheel should overlap the physical 3D printed wheel in world space.")]
        public bool FollowControllerPosition;

        InputDevice m_Controller;
        Quaternion m_CalibratedRotation = Quaternion.identity;
        Quaternion m_VirtualWheelStartRotation = Quaternion.identity;
        Vector3 m_ControllerToVirtualWheelOffset;
        bool m_HasCalibration;
        bool m_RecalibrateWasPressed;
        bool m_HornWasPressed;
        float m_LastSteeringAngle;

        void Start()
        {
            if (VirtualWheel != null)
            {
                m_VirtualWheelStartRotation = VirtualWheel.localRotation;
            }
        }

        public override InputData GenerateInput()
        {
            EnsureController();

            if (!m_Controller.isValid)
            {
                return new InputData();
            }

            bool hasRotation = m_Controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
            if (!hasRotation)
            {
                return new InputData();
            }

            bool recalibratePressed = ReadButton(RecalibrateButton);
            if ((CalibrateOnStart && !m_HasCalibration) || (recalibratePressed && !m_RecalibrateWasPressed))
            {
                Calibrate(rotation);
            }
            m_RecalibrateWasPressed = recalibratePressed;
            UpdateHorn();

            float steering = 0f;
            if (m_HasCalibration)
            {
                m_LastSteeringAngle = GetSignedAngleAroundAxis(rotation);
                steering = Mathf.Clamp(m_LastSteeringAngle / Mathf.Max(1f, MaxWheelAngle), -1f, 1f);
                steering = ApplyDeadZone(steering, SteeringDeadZone);

                if (InvertSteering)
                {
                    steering *= -1f;
                }

                UpdateVirtualWheel(m_LastSteeringAngle);
            }

            return new InputData
            {
                Accelerate = ReadButton(AccelerateButton),
                Brake = ReadButton(BrakeButton),
                TurnInput = steering
            };
        }

        public void Calibrate()
        {
            EnsureController();

            if (m_Controller.isValid &&
                m_Controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                Calibrate(rotation);
            }
        }

        void Calibrate(Quaternion controllerRotation)
        {
            m_CalibratedRotation = controllerRotation;
            m_HasCalibration = true;
            m_LastSteeringAngle = 0f;

            if (VirtualWheel != null &&
                m_Controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition))
            {
                m_ControllerToVirtualWheelOffset = Quaternion.Inverse(controllerRotation) * (VirtualWheel.position - controllerPosition);
            }
        }

        void EnsureController()
        {
            if (!m_Controller.isValid)
            {
                m_Controller = InputDevices.GetDeviceAtXRNode(ControllerNode);
            }
        }

        bool ReadButton(QuestControllerButton button)
        {
            return QuestControllerButtonUtility.IsPressed(m_Controller, button, ButtonThreshold);
        }

        void UpdateHorn()
        {
            bool hornPressed = ReadButton(HornButton);
            if (hornPressed && !m_HornWasPressed)
            {
                PlayHorn();
            }

            m_HornWasPressed = hornPressed;
        }

        void PlayHorn()
        {
            if (HornAudioSource == null)
            {
                return;
            }

            if (HornClip != null)
            {
                HornAudioSource.PlayOneShot(HornClip);
            }
            else
            {
                HornAudioSource.Play();
            }
        }

        float GetSignedAngleAroundAxis(Quaternion controllerRotation)
        {
            Quaternion delta = Quaternion.Inverse(m_CalibratedRotation) * controllerRotation;
            Vector3 axis = GetAxisVector(SteeringAxis);
            Vector3 reference = GetReferenceVector(axis);

            Vector3 from = Vector3.ProjectOnPlane(reference, axis).normalized;
            Vector3 to = Vector3.ProjectOnPlane(delta * reference, axis).normalized;

            if (from.sqrMagnitude < 0.001f || to.sqrMagnitude < 0.001f)
            {
                return 0f;
            }

            return Vector3.SignedAngle(from, to, axis);
        }

        void UpdateVirtualWheel(float angle)
        {
            if (VirtualWheel == null)
            {
                return;
            }

            if (FollowControllerPosition &&
                m_Controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition) &&
                m_Controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerRotation))
            {
                VirtualWheel.position = controllerPosition + controllerRotation * m_ControllerToVirtualWheelOffset;
            }

            float visualAngle = (InvertVirtualWheel ? -angle : angle);
            VirtualWheel.localRotation = m_VirtualWheelStartRotation * Quaternion.AngleAxis(visualAngle, GetAxisVector(VirtualWheelAxis));
        }

        static float ApplyDeadZone(float value, float deadZone)
        {
            float absolute = Mathf.Abs(value);
            if (absolute <= deadZone)
            {
                return 0f;
            }

            return Mathf.Sign(value) * Mathf.InverseLerp(deadZone, 1f, absolute);
        }

        static Vector3 GetAxisVector(WheelAxis axis)
        {
            switch (axis)
            {
                case WheelAxis.LocalX:
                    return Vector3.right;
                case WheelAxis.LocalY:
                    return Vector3.up;
                default:
                    return Vector3.forward;
            }
        }

        static Vector3 GetReferenceVector(Vector3 axis)
        {
            return Mathf.Abs(Vector3.Dot(axis.normalized, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
        }
    }
}
