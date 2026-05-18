using UnityEngine;
using UnityEngine.XR;

namespace KartGame.KartSystems
{
    [DefaultExecutionOrder(100)]
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
        [Tooltip("Multiplies the measured wheel angle before sending it to the kart.")]
        [Range(0.25f, 5f)] public float SteeringSensitivity = 2f;
        [Tooltip("Small values around center are ignored.")]
        [Range(0f, 0.3f)] public float SteeringDeadZone = 0.04f;
        [Tooltip("Invert this if turning left/right is swapped.")]
        public bool InvertSteering = true;
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
        [Tooltip("When on, the virtual wheel uses the tracked controller rotation directly.")]
        public bool MatchControllerRotation;
        public bool HideVirtualWheelWithoutController = true;
        [Tooltip("Local position on the kart when the wheel is fixed to the cockpit.")]
        public Vector3 VirtualWheelLocalPosition = new Vector3(0f, 0.45f, 0.75f);
        public Vector3 VirtualWheelLocalEulerAngles;
        public Vector3 VirtualWheelPositionOffset;
        public Vector3 VirtualWheelRotationOffset;

        [Header("Auto Visual")]
        public bool AutoCreateVirtualWheel = true;
        [Range(0.05f, 0.5f)] public float AutoWheelRadius = 0.18f;
        [Range(0.003f, 0.05f)] public float AutoWheelLineWidth = 0.018f;
        public Color AutoWheelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        InputDevice m_Controller;
        Quaternion m_CalibratedRotation = Quaternion.identity;
        Quaternion m_VirtualWheelStartRotation = Quaternion.identity;
        Vector3 m_ControllerToVirtualWheelOffset;
        bool m_HasControllerToVirtualWheelOffset;
        bool m_HasCalibration;
        bool m_RecalibrateWasPressed;
        bool m_HornWasPressed;
        bool m_AutoCreatedWheel;
        float m_LastSteeringAngle;

        void Start()
        {
            if (VirtualWheel == null && AutoCreateVirtualWheel)
            {
                VirtualWheel = CreateDefaultVirtualWheel();
                m_AutoCreatedWheel = true;
            }

            if (VirtualWheel != null)
            {
                if (m_AutoCreatedWheel && !FollowControllerPosition)
                {
                    VirtualWheel.localPosition = VirtualWheelLocalPosition;
                    VirtualWheel.localRotation = Quaternion.Euler(VirtualWheelLocalEulerAngles);
                }

                m_VirtualWheelStartRotation = VirtualWheel.localRotation;
            }
        }

        public override InputData GenerateInput()
        {
            EnsureController();

            if (!m_Controller.isValid)
            {
                SetVirtualWheelVisible(false);
                return new InputData();
            }
            SetVirtualWheelVisible(true);

            bool accelerate = ReadButton(AccelerateButton);
            bool brake = ReadButton(BrakeButton);
            UpdateHorn();

            bool hasRotation = m_Controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
            if (!hasRotation)
            {
                return new InputData
                {
                    Accelerate = accelerate,
                    Brake = brake,
                    TurnInput = 0f
                };
            }

            TryCaptureVirtualWheelPositionOffset(rotation);

            bool recalibratePressed = ReadButton(RecalibrateButton);
            bool autoCalibrate = CalibrateOnStart && !m_HasCalibration;
            bool manualRecalibrate = recalibratePressed && !m_RecalibrateWasPressed;
            if (autoCalibrate || manualRecalibrate)
            {
                Calibrate(rotation);
            }
            m_RecalibrateWasPressed = recalibratePressed;

            float steering = 0f;
            if (m_HasCalibration)
            {
                m_LastSteeringAngle = GetSignedAngleAroundAxis(rotation);
                steering = Mathf.Clamp((m_LastSteeringAngle / Mathf.Max(1f, MaxWheelAngle)) * SteeringSensitivity, -1f, 1f);
                steering = ApplyDeadZone(steering, SteeringDeadZone);

                if (InvertSteering)
                {
                    steering *= -1f;
                }

                UpdateVirtualWheel(m_LastSteeringAngle);
            }

            return new InputData
            {
                Accelerate = accelerate,
                Brake = brake,
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
        }

        void TryCaptureVirtualWheelPositionOffset(Quaternion controllerRotation)
        {
            if (m_HasControllerToVirtualWheelOffset || VirtualWheel == null)
            {
                return;
            }

            if (!m_Controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition))
            {
                return;
            }

            m_ControllerToVirtualWheelOffset = Quaternion.Inverse(controllerRotation) * (VirtualWheel.position - controllerPosition);
            m_HasControllerToVirtualWheelOffset = true;
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
                Vector3 offset = VirtualWheelPositionOffset;
                if (!AutoCreateVirtualWheel && offset == Vector3.zero && m_HasControllerToVirtualWheelOffset)
                {
                    offset = m_ControllerToVirtualWheelOffset;
                }

                VirtualWheel.position = controllerPosition + controllerRotation * offset;

                if (MatchControllerRotation)
                {
                    VirtualWheel.rotation = controllerRotation * Quaternion.Euler(VirtualWheelRotationOffset);
                    return;
                }
            }

            float visualAngle = (InvertVirtualWheel ? -angle : angle);
            VirtualWheel.localRotation = m_VirtualWheelStartRotation * Quaternion.AngleAxis(visualAngle, GetAxisVector(VirtualWheelAxis));
        }

        void SetVirtualWheelVisible(bool visible)
        {
            if (!HideVirtualWheelWithoutController || VirtualWheel == null)
            {
                return;
            }

            GameObject wheelObject = VirtualWheel.gameObject;
            if (wheelObject.activeSelf != visible)
            {
                wheelObject.SetActive(visible);
            }
        }

        Transform CreateDefaultVirtualWheel()
        {
            GameObject wheel = new GameObject("QuestVirtualSteeringWheel");
            wheel.transform.SetParent(transform, false);
            wheel.transform.localPosition = VirtualWheelLocalPosition;
            wheel.transform.localRotation = Quaternion.Euler(VirtualWheelLocalEulerAngles);

            Material material = CreateWheelMaterial();
            AddCircleLine(wheel.transform, "QuestWheelRim", AutoWheelRadius, AutoWheelLineWidth, material, true);

            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Vector3 end = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (AutoWheelRadius * 0.82f);
                AddSpokeLine(wheel.transform, "QuestWheelSpoke", Vector3.zero, end, AutoWheelLineWidth * 0.75f, material);
            }

            AddCircleLine(wheel.transform, "QuestWheelHub", AutoWheelRadius * 0.18f, AutoWheelLineWidth, material, true);
            return wheel.transform;
        }

        Material CreateWheelMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            material.color = AutoWheelColor;
            return material;
        }

        void AddCircleLine(Transform parent, string objectName, float radius, float width, Material material, bool loop)
        {
            const int segments = 64;
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = segments;
            line.startWidth = width;
            line.endWidth = width;
            line.material = material;
            line.startColor = AutoWheelColor;
            line.endColor = AutoWheelColor;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        void AddSpokeLine(Transform parent, string objectName, Vector3 start, Vector3 end, float width, Material material)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width;
            line.material = material;
            line.startColor = AutoWheelColor;
            line.endColor = AutoWheelColor;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
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
