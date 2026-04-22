using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Lever : Interactable
{
    public enum Axis { X, Y, Z }

    [Header("Pivot (optional but recommended)")]
    [Tooltip("Point about which the lever rotates. If null, uses this transform.")]
    public Transform hingePivot;

    [Header("Hinge Axis")]
    public Axis hingeAxis = Axis.X;

    [Header("3-Position Lever")]
    [Tooltip("Lever angle when pulled DOWN (positive).")]
    public float downAngle = 45f;

    [Tooltip("Lever angle at MIDDLE (neutral).")]
    public float middleAngle = 0f;

    [Tooltip("Lever angle when pulled UP (negative).")]
    public float upAngle = -45f;

    [Header("Motion")]
    [Tooltip("Max angular speed (deg/sec).")]
    public float maxDegreesPerSecond = 900f;

    [Tooltip("Snap distance: if within this many degrees of a slot, snap to it.")]
    public float snapThreshold = 15f;

    [Header("Events")]
    [Tooltip("Fires with -1 (down), 0 (middle), or 1 (up).")]
    public UnityEvent<float> onValueChanged;

    [Header("Haptics")]
    public bool haptics = false;
    [Range(0f, 1f)] public float dragHaptics = 0.2f;
    public float hapticInterval = 0.05f;

    Quaternion startLocalRot;
    float currentAngle;
    float targetAngle;
    OVRController controller;
    bool isGripping;
    bool isSnapping;
    Vector3 axisWorld;
    Vector3 uWorld;
    Vector3 vWorld;

    float angleAtGrab;
    float ctrlAngleAtGrab;

    float lastHapticTime;

    void Awake()
    {
        startLocalRot = transform.localRotation;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (hingePivot == null) hingePivot = transform;

        currentAngle = middleAngle;
        targetAngle = middleAngle;
        ApplyRotation(currentAngle);
        EmitValue(currentAngle);
    }

    public override void OnGripBegin(OVRController ctrl)
    {
        controller = ctrl;
        isGripping = true;
        isSnapping = false;

        axisWorld = GetAxisWorld();
        Vector3 reference = Mathf.Abs(Vector3.Dot(axisWorld, Vector3.up)) > 0.8f ? Vector3.right : Vector3.up;
        uWorld = Vector3.ProjectOnPlane(reference, axisWorld).normalized;
        vWorld = Vector3.Cross(axisWorld, uWorld).normalized;

        angleAtGrab = currentAngle;
        ctrlAngleAtGrab = ComputeControllerAngle(ctrl.transform.position);

        if (haptics) ctrl.HapticClick(0.2f, 0.02f);
        lastHapticTime = Time.time;
    }

    public override void OnGripEnd(OVRController ctrl)
    {
        if (ctrl == controller)
        {
            isGripping = false;
            controller = null;

            // Find nearest slot and snap to it
            float distToDown = Mathf.Abs(Mathf.DeltaAngle(currentAngle, downAngle));
            float distToMiddle = Mathf.Abs(Mathf.DeltaAngle(currentAngle, middleAngle));
            float distToUp = Mathf.Abs(Mathf.DeltaAngle(currentAngle, upAngle));

            if (distToDown <= distToMiddle && distToDown <= distToUp)
                targetAngle = downAngle;
            else if (distToMiddle <= distToUp)
                targetAngle = middleAngle;
            else
                targetAngle = upAngle;

            isSnapping = true;
        }
    }

    void Update()
    {
        if (isGripping && controller != null)
        {
            float ctrlAngleNow = ComputeControllerAngle(controller.transform.position);
            float delta = Mathf.DeltaAngle(ctrlAngleAtGrab, ctrlAngleNow);
            targetAngle = angleAtGrab + delta;

            targetAngle = Mathf.Clamp(targetAngle, Mathf.Min(downAngle, upAngle), Mathf.Max(downAngle, upAngle));

            float distToDown = Mathf.Abs(Mathf.DeltaAngle(targetAngle, downAngle));
            float distToMiddle = Mathf.Abs(Mathf.DeltaAngle(targetAngle, middleAngle));
            float distToUp = Mathf.Abs(Mathf.DeltaAngle(targetAngle, upAngle));

            if (distToDown <= snapThreshold && distToDown <= distToMiddle && distToDown <= distToUp)
                targetAngle = downAngle;
            else if (distToMiddle <= snapThreshold && distToMiddle <= distToUp)
                targetAngle = middleAngle;
            else if (distToUp <= snapThreshold)
                targetAngle = upAngle;

            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, maxDegreesPerSecond * Time.deltaTime);
            ApplyRotation(currentAngle);
            EmitValue(currentAngle);

            if (haptics && Time.time - lastHapticTime >= hapticInterval)
            {
                lastHapticTime = Time.time;
                controller.HapticTick(Mathf.Clamp01(dragHaptics), 0.015f);
            }
        }
        else if (isSnapping)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, maxDegreesPerSecond * Time.deltaTime);
            ApplyRotation(currentAngle);
            EmitValue(currentAngle);

            if (Mathf.Abs(currentAngle - targetAngle) < 0.1f)
                isSnapping = false;
        }
    }

    float ComputeControllerAngle(Vector3 ctrlWorldPos)
    {
        Vector3 dir = ctrlWorldPos - hingePivot.position;
        float x = Vector3.Dot(dir, uWorld);
        float y = Vector3.Dot(dir, vWorld);
        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    void ApplyRotation(float angle)
    {
        transform.localRotation = startLocalRot * Quaternion.AngleAxis(angle, GetAxisLocal());
    }

    void EmitValue(float angle)
    {
        float distToDown = Mathf.Abs(Mathf.DeltaAngle(angle, downAngle));
        float distToMiddle = Mathf.Abs(Mathf.DeltaAngle(angle, middleAngle));
        float distToUp = Mathf.Abs(Mathf.DeltaAngle(angle, upAngle));

        if (distToDown <= distToMiddle && distToDown <= distToUp)
            onValueChanged?.Invoke(-1f);  // Down/Descend
        else if (distToMiddle <= distToUp)
            onValueChanged?.Invoke(0f);   // Middle/Neutral
        else
            onValueChanged?.Invoke(1f);   // Up/Ascend
    }

    Vector3 GetAxisLocal()
    {
        return hingeAxis == Axis.X ? Vector3.right :
               hingeAxis == Axis.Y ? Vector3.up :
                                     Vector3.forward;
    }

    Vector3 GetAxisWorld()
    {
        return transform.parent
            ? transform.parent.TransformDirection(GetAxisLocal()).normalized
            : transform.TransformDirection(GetAxisLocal()).normalized;
    }
}