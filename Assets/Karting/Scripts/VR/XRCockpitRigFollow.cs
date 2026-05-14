using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using KartGame.KartSystems;

public class XRCockpitRigFollow : MonoBehaviour
{
    [Header("Kart Reference")]
    public Transform kartTarget;

    [Header("Cockpit Position")]
    public Vector3 cockpitOffset = new Vector3(0f, 0.75f, 0.25f);
    public bool preserveSceneCockpitPlacement = true;

    [Header("Follow Settings")]
    [Range(0.01f, 1f)]
    public float positionSmoothing = 0.02f;

    [Range(0.01f, 1f)]
    public float rotationSmoothing = 0.02f;

    [Header("Optional")]
    public bool followKartYawOnly = true;

    [Header("Driver Position From Controller")]
    public bool moveSeatBackFromControllerReach = true;
    public XRNode reachControllerNode = XRNode.RightHand;

    [Tooltip("Prefer the live Quest device position. Leave this on when steering works but seat movement does not.")]
    public bool preferTrackedDevicePosition = true;

    public Transform reachControllerTransform;
    public Transform headTransform;

    [Tooltip("Optional button to make the current controller/headset distance the neutral sitting position. Leave None so B only recalibrates steering rotation.")]
    public QuestControllerButton resetSeatBaselineButton = QuestControllerButton.None;

    [Range(0f, 1f)]
    public float buttonThreshold = 0.2f;

    [Tooltip("Sensitivity slider: meters the driver/camera moves backward/forward for each meter the right controller moves away/toward the headset.")]
    [FormerlySerializedAs("reachToSeatBackMultiplier")]
    [Range(0f, 2f)]
    public float controllerReachSensitivity = 1f;

    [Tooltip("Meters the driver/camera moves up/down for each meter the right controller moves up/down relative to the headset.")]
    [Range(0f, 2f)]
    public float controllerHeightSensitivity = 0.35f;

    [Tooltip("Meters the driver/camera moves left/right for each meter the right controller moves left/right relative to the headset.")]
    [Range(0f, 2f)]
    public float controllerSideSensitivity = 0.2f;

    [Header("Controller Mount Offset")]
    [Tooltip("Meters the controller is mounted to the right of the steering wheel rotation point. Positive means the real wheel center is left of the controller.")]
    [Range(-0.5f, 0.5f)]
    public float controllerRightOfRotationPoint = 0f;

    [Tooltip("Meters the controller is mounted above the steering wheel rotation point. Positive means the real wheel center is below the controller.")]
    [Range(-0.5f, 0.5f)]
    public float controllerAboveRotationPoint = 0f;

    [Tooltip("Meters the controller is mounted forward of the steering wheel rotation point. Positive means the real wheel center is behind the controller.")]
    [Range(-0.5f, 0.5f)]
    public float controllerForwardOfRotationPoint = 0f;

    [Tooltip("Rotate the mount offset with the controller, useful when the controller is fixed into a physical steering wheel.")]
    public bool rotateMountOffsetWithController = true;

    [Tooltip("Maximum extra backward movement of the driver/camera in the kart.")]
    [Range(0f, 1.5f)]
    public float maxSeatBackOffset = 0.6f;

    [Tooltip("Maximum extra forward movement of the driver/camera in the kart when the controller comes closer.")]
    [Range(0f, 1.5f)]
    public float maxSeatForwardOffset = 0.25f;

    [Tooltip("Maximum up/down movement of the driver/camera from controller height.")]
    [Range(0f, 1.5f)]
    public float maxSeatVerticalOffset = 0.35f;

    [Tooltip("Maximum left/right movement of the driver/camera from controller side movement.")]
    [Range(0f, 1.5f)]
    public float maxSeatSideOffset = 0.25f;

    [Range(0.01f, 1f)]
    public float seatBackSmoothing = 0.08f;

    private Vector3 velocity;
    private Quaternion currentRotation;
    private InputDevice reachController;
    private bool hasReachBaseline;
    private bool resetSeatBaselineWasPressed;
    private Vector3 reachBaselineLocal;
    private Vector3 currentSeatOffset;

    private void Start()
    {
        if (kartTarget == null)
        {
            Debug.LogError("[XRCockpitRigFollow] No kartTarget assigned.");
            enabled = false;
            return;
        }

        CaptureSceneCockpitPlacement();
        currentRotation = GetTargetRotation();
        transform.position = kartTarget.TransformPoint(GetDynamicCockpitOffset());
        transform.rotation = currentRotation;
    }

    private void LateUpdate()
    {
        if (kartTarget == null) return;

        Vector3 targetPosition = kartTarget.TransformPoint(GetDynamicCockpitOffset());

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            positionSmoothing
        );

        currentRotation = Quaternion.Slerp(
            currentRotation,
            GetTargetRotation(),
            1f - Mathf.Pow(rotationSmoothing, Time.deltaTime * 60f)
        );

        transform.rotation = currentRotation;
    }

    private Vector3 GetDynamicCockpitOffset()
    {
        return cockpitOffset + GetSeatOffset();
    }

    private void CaptureSceneCockpitPlacement()
    {
        if (!preserveSceneCockpitPlacement)
            return;

        cockpitOffset = kartTarget.InverseTransformPoint(transform.position);
    }

    private Vector3 GetSeatOffset()
    {
        if (!moveSeatBackFromControllerReach)
            return Vector3.zero;

        Transform head = GetHeadTransform();
        if (head == null || !TryGetControllerWorldPosition(out Vector3 controllerPosition))
        {
            return currentSeatOffset;
        }

        Vector3 wheelCenterPosition = GetWheelCenterWorldPosition(controllerPosition);
        Vector3 currentControllerLocal = Quaternion.Inverse(GetTargetRotation()) * (wheelCenterPosition - head.position);

        if (!hasReachBaseline)
        {
            SetSeatBaseline(currentControllerLocal);
        }

        bool resetPressed = QuestControllerButtonUtility.IsPressed(reachController, resetSeatBaselineButton, buttonThreshold);
        if (resetPressed && !resetSeatBaselineWasPressed)
            SetSeatBaseline(currentControllerLocal);
        resetSeatBaselineWasPressed = resetPressed;

        Vector3 delta = currentControllerLocal - reachBaselineLocal;
        Vector3 targetSeatOffset = new Vector3(
            Mathf.Clamp(-delta.x * controllerSideSensitivity, -maxSeatSideOffset, maxSeatSideOffset),
            Mathf.Clamp(-delta.y * controllerHeightSensitivity, -maxSeatVerticalOffset, maxSeatVerticalOffset),
            -Mathf.Clamp(delta.z * controllerReachSensitivity, -maxSeatForwardOffset, maxSeatBackOffset)
        );

        float t = 1f - Mathf.Pow(seatBackSmoothing, Time.deltaTime * 60f);
        currentSeatOffset = Vector3.Lerp(currentSeatOffset, targetSeatOffset, t);

        return currentSeatOffset;
    }

    private void SetSeatBaseline(Vector3 controllerLocal)
    {
        reachBaselineLocal = controllerLocal;
        currentSeatOffset = Vector3.zero;
        hasReachBaseline = true;
    }

    private Vector3 GetWheelCenterWorldPosition(Vector3 controllerPosition)
    {
        Vector3 controllerToWheelCenter = new Vector3(
            controllerRightOfRotationPoint,
            controllerAboveRotationPoint,
            controllerForwardOfRotationPoint
        );

        if (controllerToWheelCenter.sqrMagnitude < 0.000001f)
            return controllerPosition;

        Quaternion offsetRotation = GetTargetRotation();
        if (rotateMountOffsetWithController && TryGetControllerWorldRotation(out Quaternion controllerRotation))
            offsetRotation = controllerRotation;

        return controllerPosition - offsetRotation * controllerToWheelCenter;
    }

    private void EnsureReachController()
    {
        if (!reachController.isValid)
            reachController = InputDevices.GetDeviceAtXRNode(reachControllerNode);
    }

    private bool TryGetControllerWorldPosition(out Vector3 controllerPosition)
    {
        if (preferTrackedDevicePosition && TryGetTrackedControllerWorldPosition(out controllerPosition))
        {
            return true;
        }

        if (reachControllerTransform != null)
        {
            controllerPosition = reachControllerTransform.position;
            return true;
        }

        return TryGetTrackedControllerWorldPosition(out controllerPosition);
    }

    private bool TryGetTrackedControllerWorldPosition(out Vector3 controllerPosition)
    {
        EnsureReachController();
        if (!reachController.isValid)
        {
            controllerPosition = Vector3.zero;
            return false;
        }

        if (reachController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 trackingSpacePosition))
        {
            Transform trackingSpace = GetTrackingSpaceTransform();
            controllerPosition = trackingSpace != null
                ? trackingSpace.TransformPoint(trackingSpacePosition)
                : transform.TransformPoint(trackingSpacePosition);
            return true;
        }

        controllerPosition = Vector3.zero;
        return false;
    }

    private bool TryGetControllerWorldRotation(out Quaternion controllerRotation)
    {
        EnsureReachController();
        if (reachController.isValid &&
            reachController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion trackingSpaceRotation))
        {
            Transform trackingSpace = GetTrackingSpaceTransform();
            controllerRotation = trackingSpace != null
                ? trackingSpace.rotation * trackingSpaceRotation
                : transform.rotation * trackingSpaceRotation;
            return true;
        }

        if (reachControllerTransform != null)
        {
            controllerRotation = reachControllerTransform.rotation;
            return true;
        }

        controllerRotation = Quaternion.identity;
        return false;
    }

    private Transform GetTrackingSpaceTransform()
    {
        if (reachControllerTransform != null && reachControllerTransform.parent != null)
            return reachControllerTransform.parent;

        Transform head = GetHeadTransform();
        if (head != null && head.parent != null)
            return head.parent;

        return transform;
    }

    private Transform GetHeadTransform()
    {
        if (headTransform != null)
            return headTransform;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            headTransform = mainCamera.transform;

        return headTransform;
    }

    private Quaternion GetTargetRotation()
    {
        if (!followKartYawOnly)
            return kartTarget.rotation;

        Vector3 forward = kartTarget.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return transform.rotation;

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }
}
