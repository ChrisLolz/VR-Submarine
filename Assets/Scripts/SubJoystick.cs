using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class SubJoystick : Interactable
{
    [Header("Joystick Settings")]
    public float maxDisplacement = 0.15f;
    public float smoothing = 0.2f;
    public float maxTiltAngle = 45f;
    public GameObject controllerModel;
    
    [Header("Event")]
    public UnityEvent<float> OnSpeedChanged;
    
    private bool isGripping;
    private OVRController grippingController;
    private float currentSpeed;

    private Vector3 axisWorld;
    private Vector3 uWorld;
    private Vector3 vWorld;
    
    private Vector3 gripStartPos;
    private Vector3 lastJoystickWorldPos;
    private Quaternion startLocalRot;
    private float currentTiltAngle;

    void Awake()
    {
        startLocalRot = transform.localRotation;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public override void OnGripBegin(OVRController ctrl)
    {
        isGripping = true;
        grippingController = ctrl;
        gripStartPos = ctrl.transform.position;
        lastJoystickWorldPos = transform.position;

        axisWorld = Vector3.forward;
        Vector3 reference = Mathf.Abs(Vector3.Dot(axisWorld, Vector3.up)) > 0.8f ? Vector3.right : Vector3.up;
        uWorld = Vector3.ProjectOnPlane(reference, axisWorld).normalized;
        vWorld = Vector3.Cross(axisWorld, uWorld).normalized;
        
        Vector3 dir = ctrl.transform.position - gripStartPos;
        float forwardDisplacement = Vector3.Dot(dir, axisWorld);
        currentSpeed = Mathf.Clamp(forwardDisplacement / maxDisplacement, -1f, 1f);
        currentTiltAngle = currentSpeed * maxTiltAngle;
        ApplyTilt(currentTiltAngle);
    }

    public override void OnGripEnd(OVRController ctrl)
    {
        Debug.Log("SubJoystick: Grip End");
        if (ctrl != grippingController) return;

        isGripping = false;
        currentSpeed = 0f;
        currentTiltAngle = 0f;
        OnSpeedChanged?.Invoke(0f);
        ApplyTilt(0f);
    }

    void Update()
    {
        if (isGripping && grippingController != null)
        {
            axisWorld = transform.TransformDirection(Vector3.forward);
            
            Vector3 joystickDelta = transform.position - lastJoystickWorldPos;
            gripStartPos += joystickDelta;
            lastJoystickWorldPos = transform.position;
            
            Vector3 dir = grippingController.transform.position - gripStartPos;
            float forwardDisplacement = Vector3.Dot(dir, axisWorld);
            
            float targetSpeed = Mathf.Clamp(forwardDisplacement / maxDisplacement, -1f, 1f);
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, smoothing);
            
            float targetTilt = targetSpeed * maxTiltAngle;
            currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTilt, smoothing);
            ApplyTilt(currentTiltAngle);
            
            OnSpeedChanged?.Invoke(currentSpeed);
        }
    }

    void ApplyTilt(float angle)
    {
        transform.localRotation = startLocalRot * Quaternion.AngleAxis(angle, Vector3.right);
    }
}