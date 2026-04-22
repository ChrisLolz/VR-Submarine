using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RobotJoystick : Interactable
{
    public float maxDisplacement = 0.15f;
    public float smoothing = 0.2f;
    public float maxTiltAngle = 45f;
    public GameObject controllerModel;

    public RobotArm armAnimator;
    public bool snapControllerToArmOnGrip = false;
    public OVRInput.Button attractionButton = OVRInput.Button.PrimaryIndexTrigger;
    public bool swapControllerXZ = true;
    public bool invertForwardBackward = true;
    public float controllerToCubeSensitivity = 2.0f;
    
    private bool isGripping;
    private OVRController grippingController;
    private float currentForwardValue;
    private float currentRightValue;

    private Vector3 axisForward;
    private Vector3 axisRight;
    private Vector3 gripStartPos;
    private Vector3 lastJoystickWorldPos;
    private Quaternion startLocalRot;
    private float currentTiltForward;
    private float currentTiltRight;

    private Vector3 controllerGripStartWorldPos;
    private Vector3 cubeGripStartWorldPos;

    private bool wasAttracting = false;

    void Awake()
    {
        startLocalRot = transform.localRotation;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public override void OnGripBegin(OVRController ctrl)
    {
        controllerGripStartWorldPos = ctrl.transform.position;
        if (armAnimator != null)
        {
            if (snapControllerToArmOnGrip && armAnimator.endEffector != null)
            {
                cubeGripStartWorldPos = armAnimator.endEffector.position;
            }
            else
            {
                cubeGripStartWorldPos = armAnimator.GetCurrentTargetPosition();
            }
        }
        else
        {
            cubeGripStartWorldPos = controllerGripStartWorldPos;
        }

        isGripping = true;
        grippingController = ctrl;
        gripStartPos = ctrl.transform.position;
        lastJoystickWorldPos = transform.position;

        startLocalRot = transform.localRotation;
        axisForward = transform.TransformDirection(Vector3.forward);
        axisRight = transform.TransformDirection(Vector3.right);
        
        Vector3 dir = ctrl.transform.position - gripStartPos;
        float forwardDisplacement = Vector3.Dot(dir, axisForward);
        float rightDisplacement = Vector3.Dot(dir, axisRight);
        currentForwardValue = Mathf.Clamp(forwardDisplacement / maxDisplacement, -1f, 1f);
        currentRightValue = Mathf.Clamp(rightDisplacement / maxDisplacement, -1f, 1f);
        currentTiltForward = currentForwardValue * maxTiltAngle;
        currentTiltRight = currentRightValue * maxTiltAngle;
        ApplyTilt(currentTiltForward, currentTiltRight);

        if (armAnimator != null)
        {
            armAnimator.SetActiveController(ctrl.transform);
            armAnimator.SetControllerTargetOverride(cubeGripStartWorldPos);
        }
    }

    public override void OnGripEnd(OVRController ctrl)
    {
        if (ctrl != grippingController) return;

        isGripping = false;
        currentForwardValue = 0f;
        currentRightValue = 0f;
        currentTiltForward = 0f;
        currentTiltRight = 0f;
        SendArmInput(0f, 0f);
        ApplyTilt(0f, 0f);

        if (armAnimator != null)
        {
            armAnimator.ClearActiveController(ctrl.transform);
            armAnimator.ClearControllerTargetOverride();
            armAnimator.SetControllerTargetOverride(armAnimator.endEffector.position);
        }
    }

    void Update()
    {
        if (isGripping && grippingController != null)
        {
            axisForward = transform.TransformDirection(Vector3.forward);
            axisRight = transform.TransformDirection(Vector3.right);
            
            Vector3 joystickDelta = transform.position - lastJoystickWorldPos;
            gripStartPos += joystickDelta;
            lastJoystickWorldPos = transform.position;
            
            Vector3 dir = grippingController.transform.position - gripStartPos;
            float forwardDisplacement = Vector3.Dot(dir, axisForward);
            float rightDisplacement = Vector3.Dot(dir, axisRight);
            
            float targetForwardValue = Mathf.Clamp(forwardDisplacement / maxDisplacement, -1f, 1f);
            float targetRightValue = Mathf.Clamp(rightDisplacement / maxDisplacement, -1f, 1f);
            currentForwardValue = Mathf.Lerp(currentForwardValue, targetForwardValue, smoothing);
            currentRightValue = Mathf.Lerp(currentRightValue, targetRightValue, smoothing);
            
            float targetTiltForward = targetForwardValue * maxTiltAngle;
            float targetTiltRight = targetRightValue * maxTiltAngle;
            currentTiltForward = Mathf.Lerp(currentTiltForward, targetTiltForward, smoothing);
            currentTiltRight = Mathf.Lerp(currentTiltRight, targetTiltRight, smoothing);
            ApplyTilt(currentTiltForward, -currentTiltRight);

            if (armAnimator != null)
            {
                Vector3 rawDelta = grippingController.transform.position - controllerGripStartWorldPos;
                Vector3 remappedDelta = rawDelta;

                if (swapControllerXZ)
                {
                    remappedDelta = new Vector3(rawDelta.z, rawDelta.y, rawDelta.x);
                }

                if (invertForwardBackward)
                {
                    remappedDelta.z = -remappedDelta.z;
                }

                remappedDelta *= Mathf.Max(0f, controllerToCubeSensitivity);

                Vector3 snappedControllerPos = cubeGripStartWorldPos + remappedDelta;

                armAnimator.SetControllerTargetOverride(snappedControllerPos);

                bool shouldAttract = OVRInput.Get(attractionButton);

                wasAttracting = shouldAttract;
                armAnimator.SetAttractCollectables(shouldAttract);
            }

            SendArmInput(currentForwardValue, currentRightValue);
        }
        else
        {
            if (armAnimator != null)
                armAnimator.SetAttractCollectables(false);
        }
    }

    void ApplyTilt(float forwardAngle, float rightAngle)
    {
        transform.localRotation = startLocalRot * Quaternion.Euler(forwardAngle, 0, rightAngle);
    }

    void SendArmInput(float forwardValue, float rightValue)
    {
        if (armAnimator == null) return;

        armAnimator.JoystickDriveForward(forwardValue);
        armAnimator.JoystickDriveRight(rightValue);
    }
}
