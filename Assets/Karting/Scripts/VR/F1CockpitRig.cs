using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using KartGame.KartSystems;

/// <summary>
/// Positions the F1 cockpit visual so the steering wheel anchor (child Transform)
/// always aligns with the physical Quest controller in world space.
/// The cockpit yaw follows the kart; pitch/roll are kept upright.
///
/// Setup in Unity:
///   1. Place this script on the root of your F1 cockpit prefab.
///   2. Create a child GameObject named "SteeringWheelAnchor" at the exact spot
///      where the steering wheel centre is within the cockpit mesh.
///      Assign it to the steeringWheelAnchor field.
///   3. Assign the kart's root Transform to kartTarget.
///   4. Make sure the cockpit prefab is NOT a child of the kart — it lives
///      separately in the scene so camera + cockpit move together.
/// </summary>
[DefaultExecutionOrder(50)]
public class F1CockpitRig : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Root transform of the kart (ArcadeKart GameObject).")]
    public Transform kartTarget;

    [Tooltip("3D dashboard mount inside the cockpit. Use DashboardBody for a wheel that hangs from the dash.")]
    public Transform dashboardTransform;

    [Tooltip("Optional world-space dashboard canvas. It is parented to DashboardBody so dashboard and wheel move together.")]
    public Transform dashboardCanvasTransform;

    [Tooltip("Child transform marking the steering wheel centre in the cockpit mesh.")]
    public Transform steeringWheelAnchor;

    [Tooltip("Optional visible steering wheel mesh. If empty, the child named SteeringWheelMesh is used.")]
    public Transform steeringWheelVisual;

    [Header("Controller")]
    public XRNode controllerNode = XRNode.RightHand;

    [Tooltip("Legacy mode: moves the whole cockpit until the wheel anchor sits on the controller.")]
    public bool anchorCockpitToController = false;

    [Tooltip("Moves only the visible wheel to the controller. Leave this off when the wheel must stay fixed in the kart.")]
    public bool moveWheelToController = false;

    [Header("Yaw Follow")]
    [Tooltip("Locks the cockpit directly to the kart frame without smoothing, so the dash does not drift against the car.")]
    public bool lockToKartFrame = true;

    [Range(0.01f, 1f)]
    [Tooltip("Lower = smoother/slower rotation tracking.")]
    public float rotationSmoothing = 0.04f;

    [Header("Position Follow")]
    [Tooltip("Seat/cockpit position relative to the kart root.")]
    public Vector3 cockpitOffset = new Vector3(0f, 0.75f, 0.25f);

    [Tooltip("Use the F1Cockpit transform as placed in the scene instead of forcing the cockpitOffset field.")]
    public bool preserveSceneCockpitPlacement = true;

    [Tooltip("Small extra height adjustment for the player viewpoint. Negative sits deeper in the kart.")]
    public float viewpointHeightOffset = -0.08f;

    [Range(0.01f, 1f)]
    public float positionSmoothing = 0.04f;

    [Header("Height Offset")]
    [Tooltip("Extra vertical offset added on top of the controller-derived position. " +
             "Positive moves the cockpit down relative to you (you sit higher).")]
    public float verticalBias = 0f;

    [Header("Visibility")]
    [Tooltip("Build simple cockpit geometry if this object only contains empty placeholders.")]
    public bool autoBuildVisibleCockpit = false;

    [Header("Dashboard Placement")]
    [Tooltip("Exact local dashboard mount position inside F1Cockpit. Move this to place dashboard + wheel in the car frame.")]
    public Vector3 dashboardLocalPosition = Vector3.zero;

    [Tooltip("Exact local dashboard mount rotation inside F1Cockpit.")]
    public Vector3 dashboardLocalEulerAngles = Vector3.zero;

    [Tooltip("Exact local dashboard mount scale inside F1Cockpit.")]
    public Vector3 dashboardLocalScale = Vector3.one;

    [Tooltip("Keep DashboardCanvas as a child of DashboardBody so it moves with the steering wheel.")]
    public bool parentDashboardCanvasToDashboardBody = true;

    [Tooltip("Place DashboardCanvas on the imported steering wheel so display and LEDs rotate with the wheel.")]
    public bool parentDashboardCanvasToSteeringWheel = true;

    [Tooltip("Local canvas position on SteeringWheelMesh/Stuur.")]
    public Vector3 wheelDashboardCanvasLocalPosition = new Vector3(0f, 0f, 0.05f);

    [Tooltip("Local canvas rotation on SteeringWheelMesh/Stuur.")]
    public Vector3 wheelDashboardCanvasLocalEulerAngles = Vector3.zero;

    [Tooltip("Build a visible 3D steering wheel when SteeringWheelMesh is empty.")]
    public bool autoBuildSteeringWheel = true;

    [Tooltip("Local wheel position on DashboardBody.")]
    public Vector3 steeringWheelLocalPosition = new Vector3(0f, -0.22f, 0.4f);

    [Tooltip("Local wheel tilt on DashboardBody.")]
    public Vector3 steeringWheelLocalEulerAngles = new Vector3(68f, 0f, 0f);

    [Tooltip("Use the steering wheel transform as placed in the scene instead of forcing the numeric fields below.")]
    public bool preserveSceneSteeringWheelPlacement = true;

    [Range(0.12f, 0.45f)]
    public float steeringWheelRadius = 0.26f;

    [Range(0.01f, 0.08f)]
    public float steeringWheelGripThickness = 0.035f;

    [Tooltip("Use the cockpit 3D wheel as QuestSteeringWheelInput.VirtualWheel and stop the old line wheel from spawning.")]
    public bool useAsQuestVirtualWheel = true;

    [Tooltip("Hide the original player kart renderers. Leave this off when you want to see the real kart around you.")]
    public bool hideOriginalKartRenderers = false;

    [Header("Visual Physics Safety")]
    [Tooltip("Make colliders on the steering wheel/buttons trigger-only so cockpit visuals cannot push or flip the kart.")]
    public bool makeSteeringWheelCollidersTriggers = true;

    [Tooltip("Make rigidbodies on the steering wheel/buttons kinematic/no-gravity so visual parts cannot affect kart physics.")]
    public bool disableSteeringWheelRigidbodyPhysics = true;

    [Header("Wheel Button Visuals")]
    [Tooltip("Animate steering wheel buttons from Quest input without affecting kart physics.")]
    public bool animateQuestButtonsOnWheel = true;
    public QuestControllerButton jumpVisualButton = QuestControllerButton.SecondaryButton;
    public int jumpVisualButtonNumber = 1;
    public QuestControllerButton lockVisualButton = QuestControllerButton.PrimaryButton;
    public int lockVisualButtonNumber = 2;

    private InputDevice m_Controller;
    private Quaternion m_CurrentYaw;
    private Vector3 m_PositionVelocity;

    void Start()
    {
        CaptureSceneCockpitPlacement();

        ResolveDashboardTransform();
        ResolveDashboardCanvasTransform();
        ApplyDashboardPlacement();

        if (steeringWheelVisual == null)
        {
            Transform wheel = transform.Find("SteeringWheelMesh");
            if (wheel != null) steeringWheelVisual = wheel;
        }

        CaptureSceneSteeringWheelPlacement();

        if (steeringWheelAnchor == null)
            steeringWheelAnchor = steeringWheelVisual;

        if (autoBuildVisibleCockpit)
            BuildVisibleCockpitIfEmpty();

        if (autoBuildSteeringWheel)
            BuildSteeringWheelIfNeeded();

        PrepareSteeringWheelVisualPhysics();
        SetupQuestWheelButtonVisuals();

        AttachWheelToQuestInput();

        if (hideOriginalKartRenderers)
            HideOriginalKartRenderers();

        m_CurrentYaw = GetKartYaw();
        transform.rotation = m_CurrentYaw;
        transform.position = GetKartCockpitPosition();

        if (anchorCockpitToController)
            SnapCockpitToController();
    }

    void LateUpdate()
    {
        if (kartTarget == null) return;

        ApplyDashboardPlacement();

        // 1. Smoothly track kart yaw only (keep cockpit upright like a real F1 car).
        m_CurrentYaw = Quaternion.Slerp(
            m_CurrentYaw,
            GetKartYaw(),
            1f - Mathf.Pow(rotationSmoothing, Time.deltaTime * 60f)
        );
        transform.rotation = m_CurrentYaw;

        // 2. Keep the seat/cockpit attached to the kart. The controller may move the wheel,
        // but it should not pull the whole cockpit away from the kart.
        if (anchorCockpitToController)
        {
            SnapCockpitToController();
        }
        else if (lockToKartFrame)
        {
            transform.rotation = kartTarget.rotation;
            transform.position = GetKartCockpitPosition();

            MoveWheelVisualToController();
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                GetKartCockpitPosition(),
                ref m_PositionVelocity,
                positionSmoothing
            );

            MoveWheelVisualToController();
        }
    }

    void ResolveDashboardTransform()
    {
        if (dashboardTransform != null) return;

        dashboardTransform = transform.Find("DashboardBody");
        if (dashboardTransform != null) return;

        dashboardTransform = transform.Find("DashboardCanvas");
        if (dashboardTransform != null) return;

        GameObject dashboardBody = new GameObject("DashboardBody");
        dashboardBody.transform.SetParent(transform, false);
        dashboardTransform = dashboardBody.transform;
    }

    void ResolveDashboardCanvasTransform()
    {
        if (dashboardCanvasTransform != null) return;

        Transform canvas = transform.Find("DashboardCanvas");
        if (canvas != null)
        {
            dashboardCanvasTransform = canvas;
            return;
        }

        if (dashboardTransform != null)
            dashboardCanvasTransform = dashboardTransform.Find("DashboardCanvas");
    }

    void ApplyDashboardPlacement()
    {
        if (dashboardTransform == null) return;

        dashboardTransform.localPosition = dashboardLocalPosition;
        dashboardTransform.localRotation = Quaternion.Euler(dashboardLocalEulerAngles);
        dashboardTransform.localScale = dashboardLocalScale;

        if (dashboardCanvasTransform == null)
            return;

        if (parentDashboardCanvasToSteeringWheel && steeringWheelVisual != null)
        {
            if (!dashboardCanvasTransform.IsChildOf(steeringWheelVisual))
            {
                dashboardCanvasTransform.SetParent(steeringWheelVisual, false);

                Vector3 canvasPosition = wheelDashboardCanvasLocalPosition;
                if (canvasPosition.z < 0.035f)
                    canvasPosition.z = 0.05f;

                dashboardCanvasTransform.localPosition = canvasPosition;
                dashboardCanvasTransform.localRotation = Quaternion.Euler(wheelDashboardCanvasLocalEulerAngles);
            }

            return;
        }

        if (parentDashboardCanvasToDashboardBody && dashboardCanvasTransform.parent != dashboardTransform)
        {
            dashboardCanvasTransform.SetParent(dashboardTransform, true);
        }
    }

    void PositionWheelOnDashboard()
    {
        if (steeringWheelVisual == null) return;

        Transform parent = dashboardTransform != null ? dashboardTransform : transform;
        if (steeringWheelVisual.parent != parent)
            steeringWheelVisual.SetParent(parent, false);

        steeringWheelVisual.localPosition = steeringWheelLocalPosition;
        steeringWheelVisual.localRotation = Quaternion.Euler(steeringWheelLocalEulerAngles);

        if (steeringWheelAnchor == null)
            steeringWheelAnchor = steeringWheelVisual;
        else
        {
            if (steeringWheelAnchor.parent != parent)
                steeringWheelAnchor.SetParent(parent, false);

            steeringWheelAnchor.localPosition = steeringWheelLocalPosition;
            steeringWheelAnchor.localRotation = Quaternion.Euler(steeringWheelLocalEulerAngles);
        }
    }

    void CaptureSceneSteeringWheelPlacement()
    {
        if (!preserveSceneSteeringWheelPlacement || steeringWheelVisual == null)
            return;

        steeringWheelLocalPosition = steeringWheelVisual.localPosition;
        steeringWheelLocalEulerAngles = steeringWheelVisual.localEulerAngles;
    }

    void CaptureSceneCockpitPlacement()
    {
        if (!preserveSceneCockpitPlacement || kartTarget == null)
            return;

        cockpitOffset = kartTarget.InverseTransformPoint(transform.position);
        viewpointHeightOffset = 0f;
        verticalBias = 0f;
    }

    void BuildSteeringWheelIfNeeded()
    {
        if (steeringWheelVisual == null)
        {
            steeringWheelVisual = new GameObject("SteeringWheelMesh").transform;
            PositionWheelOnDashboard();
        }

        if (steeringWheelVisual.GetComponentsInChildren<Renderer>(true).Length > 0)
        {
            if (steeringWheelAnchor == null)
                steeringWheelAnchor = steeringWheelVisual;
            return;
        }

        PositionWheelOnDashboard();

        Material rim = MakeMaterial("Runtime F1 Wheel Rim", new Color(0.012f, 0.012f, 0.014f, 1f));
        Material trim = MakeMaterial("Runtime F1 Wheel Trim", new Color(0.72f, 0.04f, 0.035f, 1f));
        Material hub = MakeMaterial("Runtime F1 Wheel Hub", new Color(0.08f, 0.085f, 0.09f, 1f));

        CreateTorus("WheelRim3D", steeringWheelVisual, steeringWheelRadius, steeringWheelGripThickness, rim);

        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f;
            Transform spoke = CreatePart("WheelSpoke3D_" + i, steeringWheelVisual, PrimitiveType.Cube,
                Vector3.zero, new Vector3(steeringWheelRadius * 1.15f, steeringWheelGripThickness * 0.55f, steeringWheelGripThickness * 0.7f), hub);

            spoke.localPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * steeringWheelRadius * 0.25f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * steeringWheelRadius * 0.25f,
                0f);
            spoke.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        Transform hubPart = CreatePart("WheelHub3D", steeringWheelVisual, PrimitiveType.Cylinder,
            Vector3.zero, new Vector3(steeringWheelRadius * 0.55f, steeringWheelGripThickness * 0.7f, steeringWheelRadius * 0.55f), hub);
        hubPart.localRotation = Quaternion.Euler(90f, 0f, 0f);

        CreatePart("WheelTopMarker3D", steeringWheelVisual, PrimitiveType.Cube,
            new Vector3(0f, steeringWheelRadius, -steeringWheelGripThickness * 0.2f),
            new Vector3(steeringWheelRadius * 0.34f, steeringWheelGripThickness * 0.75f, steeringWheelGripThickness * 0.9f), trim);

        CreatePart("DashboardSteeringColumn3D", steeringWheelVisual, PrimitiveType.Cube,
            new Vector3(0f, 0f, steeringWheelGripThickness * 3.4f),
            new Vector3(steeringWheelGripThickness * 1.6f, steeringWheelGripThickness * 1.6f, steeringWheelGripThickness * 6.5f), hub);
    }

    void PrepareSteeringWheelVisualPhysics()
    {
        if (steeringWheelVisual == null)
            return;

        if (makeSteeringWheelCollidersTriggers)
        {
            foreach (Collider collider in steeringWheelVisual.GetComponentsInChildren<Collider>(true))
            {
                collider.isTrigger = true;
            }
        }

        if (disableSteeringWheelRigidbodyPhysics)
        {
            foreach (Rigidbody body in steeringWheelVisual.GetComponentsInChildren<Rigidbody>(true))
            {
                body.useGravity = false;
                body.isKinematic = true;
            }
        }
    }

    void SetupQuestWheelButtonVisuals()
    {
        if (!animateQuestButtonsOnWheel || steeringWheelVisual == null)
            return;

        jumpVisualButton = QuestControllerButton.SecondaryButton;
        jumpVisualButtonNumber = 1;
        lockVisualButton = QuestControllerButton.PrimaryButton;
        lockVisualButtonNumber = 2;

        QuestWheelButtonVisuals visuals = GetComponent<QuestWheelButtonVisuals>();
        if (visuals == null)
            visuals = gameObject.AddComponent<QuestWheelButtonVisuals>();

        visuals.controllerNode = controllerNode;
        visuals.buttonSearchRoot = steeringWheelVisual;
        visuals.buttonThreshold = 0.2f;
        visuals.jumpButton = jumpVisualButton;
        visuals.jumpButtonNumber = jumpVisualButtonNumber;
        visuals.lockButton = lockVisualButton;
        visuals.lockButtonNumber = lockVisualButtonNumber;

        // Nieuwe namen (hernoemd door gebruiker)
        ConfigureWheelButtonAnimation("Nitro",  1);
        ConfigureWheelButtonAnimation("Honk",   2);
        ConfigureWheelButtonAnimation("Jump",   3);
        ConfigureWheelButtonAnimation("Laser",  4);
        ConfigureWheelButtonAnimation("Sms",    5);
        ConfigureWheelButtonAnimation("Wiper",  6);
        ConfigureWheelButtonAnimation("Light",  7);
        ConfigureWheelButtonAnimation("Lock",   8);
        // Oude namen als fallback (voor andere scenes)
        ConfigureWheelButtonAnimation("Knop (1)", 1);
        ConfigureWheelButtonAnimation("Knop (2)", 2);
    }

    void ConfigureWheelButtonAnimation(string buttonName, int buttonNumber)
    {
        Transform buttonTransform = FindChildByName(steeringWheelVisual, buttonName);
        if (buttonTransform == null)
            return;

        Type animationType = FindButtonAnimationType();
        if (animationType == null)
            return;

        Component animation = buttonTransform.GetComponent(animationType);
        if (animation == null)
            animation = buttonTransform.gameObject.AddComponent(animationType);

        Transform cylinder = FindChildByName(buttonTransform, "Cylinder");
        Renderer renderer = cylinder != null
            ? cylinder.GetComponentInChildren<Renderer>(true)
            : buttonTransform.GetComponentInChildren<Renderer>(true);

        SetAnimationField(animation, "knopNummer", buttonNumber);
        SetAnimationField(animation, "knopTop", cylinder);
        SetAnimationField(animation, "knopRenderer", renderer);
        SetAnimationField(animation, "indrukKleur", Color.green);
        SetAnimationField(animation, "wachttijd", 0.2f);
        SetAnimationEnumField(animation, "bewegingsAs", "LocalZ");
        float currentLocalZ = cylinder != null ? cylinder.localPosition.z : 0f;
        SetAnimationField(animation, "ingedrukteLocalWaarde", currentLocalZ);
        SetAnimationField(animation, "ingedrukteLocalZ", currentLocalZ);
        SetAnimationField(animation, "activeerMetTrigger", false);

        MethodInfo refreshMethod = animationType.GetMethod(
            "VernieuwStartWaarden",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (refreshMethod != null)
            refreshMethod.Invoke(animation, null);
    }

    static Type FindButtonAnimationType()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType("ButtonAnimation");
            if (type != null)
                return type;
        }

        return null;
    }

    static void SetAnimationField(Component animation, string fieldName, object value)
    {
        FieldInfo field = animation.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (field != null)
            field.SetValue(animation, value);
    }

    static void SetAnimationEnumField(Component animation, string fieldName, string enumName)
    {
        FieldInfo field = animation.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (field == null || !field.FieldType.IsEnum)
            return;

        field.SetValue(animation, Enum.Parse(field.FieldType, enumName));
    }

    static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }

    void AttachWheelToQuestInput()
    {
        if (!useAsQuestVirtualWheel || kartTarget == null || steeringWheelVisual == null)
            return;

        QuestSteeringWheelInput input = kartTarget.GetComponent<QuestSteeringWheelInput>();
        if (input == null)
            input = kartTarget.GetComponentInChildren<QuestSteeringWheelInput>();
        if (input == null)
            input = kartTarget.GetComponentInParent<QuestSteeringWheelInput>();
        if (input == null)
            input = FindObjectOfType<QuestSteeringWheelInput>();

        if (input == null)
            return;

        input.VirtualWheel = steeringWheelVisual;
        input.AutoCreateVirtualWheel = false;
        input.FollowControllerPosition = false;
        input.MatchControllerRotation = false;
        input.VirtualWheelAxis = QuestSteeringWheelInput.WheelAxis.LocalZ;
        input.VirtualWheelLocalPosition = steeringWheelVisual.localPosition;
        input.VirtualWheelLocalEulerAngles = steeringWheelVisual.localEulerAngles;
    }

    // ---------------------------------------------------------------------------
    // Moves the cockpit root so steeringWheelAnchor world-pos == controller world-pos.
    // This means: wherever you physically hold the controller, the cockpit's steering
    // wheel appears there, and the rest of the cockpit (dashboard, sides) follows.
    // ---------------------------------------------------------------------------
    void SnapCockpitToController()
    {
        EnsureController();
        if (!m_Controller.isValid) return;
        if (!m_Controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerWorldPos)) return;
        if (steeringWheelAnchor == null) return;

        Vector3 anchorWorldOffset = steeringWheelAnchor.position - transform.position;

        // Place cockpit root so anchorWorldOffset cancels out at controllerWorldPos.
        transform.position = controllerWorldPos - anchorWorldOffset + Vector3.up * verticalBias;
    }

    void MoveWheelVisualToController()
    {
        if (!moveWheelToController || steeringWheelVisual == null) return;

        EnsureController();
        if (!m_Controller.isValid) return;
        if (!m_Controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerWorldPos)) return;

        steeringWheelVisual.position = controllerWorldPos;

        if (m_Controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerWorldRot))
            steeringWheelVisual.rotation = controllerWorldRot;
        else
            steeringWheelVisual.rotation = m_CurrentYaw;
    }

    void EnsureController()
    {
        if (!m_Controller.isValid)
            m_Controller = InputDevices.GetDeviceAtXRNode(controllerNode);
    }

    Quaternion GetKartYaw()
    {
        if (kartTarget == null) return Quaternion.identity;
        Vector3 forward = kartTarget.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) return transform.rotation;
        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    Vector3 GetKartCockpitPosition()
    {
        if (kartTarget == null) return transform.position;
        return kartTarget.TransformPoint(cockpitOffset) + Vector3.up * (verticalBias + viewpointHeightOffset);
    }

    void HideOriginalKartRenderers()
    {
        if (kartTarget == null) return;

        foreach (Renderer renderer in kartTarget.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;
    }

    void BuildVisibleCockpitIfEmpty()
    {
        if (GetComponentsInChildren<Renderer>(true).Length > 0) return;

        Material body = MakeMaterial("Runtime F1 Cockpit Body", new Color(0.02f, 0.025f, 0.03f, 1f));
        Material trim = MakeMaterial("Runtime F1 Cockpit Trim", new Color(0.8f, 0.05f, 0.04f, 1f));
        Material wheel = MakeMaterial("Runtime F1 Wheel", new Color(0.015f, 0.015f, 0.018f, 1f));

        CreatePart("CockpitNose", transform, PrimitiveType.Cube, new Vector3(0f, -0.55f, 1.35f), new Vector3(0.32f, 0.18f, 1.6f), body);
        CreatePart("DashboardShell", transform, PrimitiveType.Cube, new Vector3(0f, -0.35f, 0.82f), new Vector3(1.35f, 0.26f, 0.25f), body);
        CreatePart("LeftCockpitSide", transform, PrimitiveType.Cube, new Vector3(-0.62f, -0.5f, 0.48f), new Vector3(0.18f, 0.35f, 1.25f), body);
        CreatePart("RightCockpitSide", transform, PrimitiveType.Cube, new Vector3(0.62f, -0.5f, 0.48f), new Vector3(0.18f, 0.35f, 1.25f), body);
        CreatePart("HaloTop", transform, PrimitiveType.Cube, new Vector3(0f, 0.24f, 0.9f), new Vector3(1.05f, 0.07f, 0.08f), trim);
        CreatePart("HaloCenter", transform, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.98f), new Vector3(0.08f, 0.5f, 0.08f), trim);

        Transform wheelRoot = steeringWheelVisual != null ? steeringWheelVisual : new GameObject("SteeringWheelMesh").transform;
        steeringWheelVisual = wheelRoot;
        steeringWheelAnchor = wheelRoot;
        PositionWheelOnDashboard();

        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            Transform segment = CreatePart("WheelRim_" + i, wheelRoot, PrimitiveType.Cube, Vector3.zero, new Vector3(0.16f, 0.035f, 0.035f), wheel);
            segment.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.24f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.24f, 0f);
            segment.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        CreatePart("WheelHub", wheelRoot, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.16f, 0.05f, 0.16f), wheel);
        CreatePart("WheelSpokeL", wheelRoot, PrimitiveType.Cube, new Vector3(-0.12f, 0f, 0f), new Vector3(0.25f, 0.035f, 0.035f), wheel);
        CreatePart("WheelSpokeR", wheelRoot, PrimitiveType.Cube, new Vector3(0.12f, 0f, 0f), new Vector3(0.25f, 0.035f, 0.035f), wheel);

    }

    Transform CreatePart(string name, Transform parent, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        return part.transform;
    }

    Transform CreateTorus(string name, Transform parent, float radius, float tubeRadius, Material material)
    {
        GameObject torus = new GameObject(name);
        torus.transform.SetParent(parent, false);

        MeshFilter meshFilter = torus.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateTorusMesh(radius, tubeRadius, 64, 12);

        MeshRenderer renderer = torus.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        return torus.transform;
    }

    Mesh CreateTorusMesh(float radius, float tubeRadius, int radialSegments, int tubeSegments)
    {
        Vector3[] vertices = new Vector3[radialSegments * tubeSegments];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[radialSegments * tubeSegments * 6];

        for (int i = 0; i < radialSegments; i++)
        {
            float radialAngle = (i / (float)radialSegments) * Mathf.PI * 2f;
            Vector3 radial = new Vector3(Mathf.Cos(radialAngle), Mathf.Sin(radialAngle), 0f);

            for (int j = 0; j < tubeSegments; j++)
            {
                float tubeAngle = (j / (float)tubeSegments) * Mathf.PI * 2f;
                Vector3 normal = radial * Mathf.Cos(tubeAngle) + Vector3.forward * Mathf.Sin(tubeAngle);
                int index = i * tubeSegments + j;

                vertices[index] = radial * radius + normal * tubeRadius;
                normals[index] = normal.normalized;
                uvs[index] = new Vector2(i / (float)radialSegments, j / (float)tubeSegments);
            }
        }

        int t = 0;
        for (int i = 0; i < radialSegments; i++)
        {
            int nextI = (i + 1) % radialSegments;
            for (int j = 0; j < tubeSegments; j++)
            {
                int nextJ = (j + 1) % tubeSegments;
                int a = i * tubeSegments + j;
                int b = nextI * tubeSegments + j;
                int c = nextI * tubeSegments + nextJ;
                int d = i * tubeSegments + nextJ;

                triangles[t++] = a;
                triangles[t++] = b;
                triangles[t++] = c;
                triangles[t++] = a;
                triangles[t++] = c;
                triangles[t++] = d;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Runtime F1 Steering Wheel Torus";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    Material MakeMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        return material;
    }
}

public class QuestWheelButtonVisuals : MonoBehaviour
{
    public XRNode controllerNode = XRNode.RightHand;
    public Transform buttonSearchRoot;
    [Range(0f, 1f)] public float buttonThreshold = 0.2f;

    public QuestControllerButton jumpButton = QuestControllerButton.PrimaryButton;
    public int jumpButtonNumber = 6;

    public QuestControllerButton lockButton = QuestControllerButton.SecondaryButton;
    public int lockButtonNumber = 7;

    InputDevice controller;
    bool jumpWasPressed;
    bool lockWasPressed;

    void Update()
    {
        EnsureController();
        if (!controller.isValid)
            return;

        UpdateMapping(jumpButton, jumpButtonNumber, ref jumpWasPressed);
        UpdateMapping(lockButton, lockButtonNumber, ref lockWasPressed);
    }

    void UpdateMapping(QuestControllerButton button, int buttonNumber, ref bool wasPressed)
    {
        bool pressed = QuestControllerButtonUtility.IsPressed(controller, button, buttonThreshold);
        if (pressed && !wasPressed)
            PressVisualButton(buttonNumber);

        wasPressed = pressed;
    }

    void PressVisualButton(int buttonNumber)
    {
        MonoBehaviour buttonAnimation = FindVisualButton(buttonNumber);
        if (buttonAnimation != null)
            buttonAnimation.SendMessage("DrukIn", SendMessageOptions.DontRequireReceiver);
    }

    MonoBehaviour FindVisualButton(int buttonNumber)
    {
        Transform root = buttonSearchRoot != null ? buttonSearchRoot : transform;
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (IsButtonAnimation(behaviour) && MatchesButtonNumber(behaviour, buttonNumber))
                return behaviour;
        }

        return null;
    }

    void EnsureController()
    {
        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);
    }

    static bool IsButtonAnimation(MonoBehaviour behaviour)
    {
        return behaviour != null && behaviour.GetType().Name == "ButtonAnimation";
    }

    static bool MatchesButtonNumber(MonoBehaviour behaviour, int buttonNumber)
    {
        MethodInfo method = behaviour.GetType().GetMethod(
            "KomtOvereenMetKnopNummer",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method != null && method.ReturnType == typeof(bool))
            return (bool)method.Invoke(behaviour, new object[] { buttonNumber });

        return false;
    }
}
