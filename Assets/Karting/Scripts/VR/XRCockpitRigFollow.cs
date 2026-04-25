using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRCockpitRigFollow : MonoBehaviour
{
    [Header("Kart Reference")]
    public Transform kartTarget;

    [Header("Cockpit Position")]
    public Vector3 cockpitOffset = new Vector3(0f, 0.75f, 0.25f);

    [Header("Follow Settings")]
    [Range(0.01f, 1f)]
    public float positionSmoothing = 0.02f;

    [Range(0.01f, 1f)]
    public float rotationSmoothing = 0.02f;

    [Header("Optional")]
    public bool followKartYawOnly = true;

    private Vector3 velocity;
    private Quaternion currentRotation;

    private void Start()
    {
        if (kartTarget == null)
        {
            Debug.LogError("[XRCockpitRigFollow] No kartTarget assigned.");
            enabled = false;
            return;
        }

        currentRotation = GetTargetRotation();
        transform.position = kartTarget.TransformPoint(cockpitOffset);
        transform.rotation = currentRotation;
    }

    private void LateUpdate()
    {
        if (kartTarget == null) return;

        Vector3 targetPosition = kartTarget.TransformPoint(cockpitOffset);

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
