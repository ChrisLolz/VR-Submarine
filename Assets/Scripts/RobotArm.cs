using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotArm : MonoBehaviour
{
    [Header("Rig References")]
    public Transform baseJoint;       
    public Transform shoulderJoint;   
    public Transform elbowJoint;      
    public Transform endEffector;     

    [Header("Zucconi Iterative IK parameters")]
    [Tooltip("How aggressively it moves per step. Lower this if the arm vibrates.")]
    public float learningRate = 50f;
    [Tooltip("The virtual nudge to see which direction helps")]
    public float samplingDistance = 0.5f;
    [Tooltip("If we are this close, stop moving")]
    public float distanceThreshold = 0.01f;
    [Tooltip("How many attempts per frame to find the solution. Higher = faster but costlier")]
    public int ikIterations = 20;

    [Header("Joystick Controls")]
    [Tooltip("How fast the joysticks move the arm target")]
    public float joystickMoveSpeed = 1f;
    [Tooltip("When true, arm target follows the active VR controller while gripping")]
    public bool driveFromController = true;

    public Transform sceneCubeTarget;

    [Header("Object Bounds Gate")]
    [Tooltip("Only move arm when the cube target is inside this object's world bounds")]
    public bool requireObjectBounds = true;
    [Tooltip("Assign the boundary object (collider/renderer hierarchy)")]
    public Transform boundsObject;

    [Header("Rotation Axes")]
    [Tooltip("Which way the base spins (Usually Y: 0,1,0)")]
    public Vector3 baseAxis = Vector3.up; 
    [Tooltip("Which way the shoulder bends (Try X: 1,0,0 or Z: 0,0,1)")]
    public Vector3 shoulderAxis = Vector3.right; 
    [Tooltip("Which way the elbow bends (Try X: 1,0,0 or Z: 0,0,1)")]
    public Vector3 elbowAxis = Vector3.right;

    private Transform[] joints;
    private Quaternion[] initialRotations;
    private float[] angles;
    private Vector3[] rotationAxes;

    private bool isInitialized = false;
    private Vector3 elbowToTipLocalOffset;

    // Joystick Inputs
    private float inputForward = 0f;
    private float inputRight = 0f;
    private float inputUp = 0f;
    private Transform activeController;
    private Transform cubeTarget;
    private bool hasControllerTargetOverride = false;
    private Vector3 lastAcceptedTargetPos;

    [Header("Collectable Magnet")]
    public Transform magnetTransform;
    public LayerMask collectableLayer;
    public float collectableTouchRadius = 0.1f;
    public float collectablePullSpeed = 10f;
    private bool isAttractingCollectables = false;

    public void JoystickDriveForward(float speed) { inputForward = speed; }
    public void JoystickDriveRight(float speed) { inputRight = speed; }
    public void JoystickDriveUp(float speed) { inputUp = speed; }
    public void SetActiveController(Transform controller)
    {
        activeController = controller;
    }

    public Vector3 GetCurrentTargetPosition()
    {
        if (cubeTarget != null)
        {
            return cubeTarget.position;
        }

        return endEffector != null ? endEffector.position : transform.position;
    }

    public void SetControllerTargetOverride(Vector3 targetWorldPosition)
    {
        cubeTarget.position = ClampCubeTargetPosition(targetWorldPosition);
        hasControllerTargetOverride = true;
    }

    public void ClearControllerTargetOverride()
    {
        hasControllerTargetOverride = false;
    }

    public void ClearActiveController(Transform controller)
    {
        if (activeController == controller)
        {
            activeController = null;
            ClearControllerTargetOverride();
        }
    }

    void Start()
    {
        joints = new Transform[] { baseJoint, shoulderJoint, elbowJoint };
        rotationAxes = new Vector3[] { baseAxis, shoulderAxis, elbowAxis };
        initialRotations = new Quaternion[] {
            baseJoint.localRotation,
            shoulderJoint.localRotation,
            elbowJoint.localRotation
        };

        angles = new float[] { 0f, 0f, 0f };

        elbowToTipLocalOffset = elbowJoint.InverseTransformPoint(endEffector.position);

        cubeTarget = sceneCubeTarget;
        cubeTarget.position = ClampCubeTargetPosition(endEffector.position);
        lastAcceptedTargetPos = cubeTarget.position;

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        Vector3 currentTargetPos = cubeTarget.position;

        if (Mathf.Abs(inputForward) > 0.01f || Mathf.Abs(inputRight) > 0.01f || Mathf.Abs(inputUp) > 0.01f)
        {
            Vector3 moveDir = new Vector3(inputRight, inputUp, inputForward);
            Vector3 worldMoveDir = baseJoint.TransformDirection(moveDir);

            cubeTarget.position = ClampCubeTargetPosition(cubeTarget.position + worldMoveDir * (joystickMoveSpeed * Time.deltaTime));
            currentTargetPos = GetAcceptedCubeTarget(cubeTarget.position);
        }
        else
        {
            currentTargetPos = GetAcceptedCubeTarget(currentTargetPos);
        }

        for (int i = 0; i < ikIterations; i++)
        {
            if (Vector3.Distance(GetStructuralTipPosition(), currentTargetPos) < distanceThreshold)
                break;

            for (int j = 0; j < joints.Length; j++)
            {
                float gradient = PartialGradient(currentTargetPos, j);
                angles[j] -= learningRate * gradient;
            }

            ApplyAngles();
        }

        if (isAttractingCollectables)
        {
            AttractNearbyCollectables();
        }
    }

    float PartialGradient(Vector3 targetPos, int jointIndex)
    {
        float standardAngle = angles[jointIndex];

        float f_x = Vector3.Distance(GetStructuralTipPosition(), targetPos);

        angles[jointIndex] += samplingDistance;
        ApplyAngles();
        float f_x_plus_d = Vector3.Distance(GetStructuralTipPosition(), targetPos);

        float gradient = (f_x_plus_d - f_x) / samplingDistance;

        angles[jointIndex] = standardAngle;
        ApplyAngles();

        return gradient;
    }

    Vector3 GetStructuralTipPosition()
    {
        return elbowJoint.TransformPoint(elbowToTipLocalOffset); 
    }

    void ApplyAngles()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            joints[i].localRotation = initialRotations[i] * Quaternion.AngleAxis(angles[i], rotationAxes[i]);
        }
    }

    Vector3 GetAcceptedCubeTarget(Vector3 fallback)
    {
        Vector3 candidate = cubeTarget.position;
        bool inBounds = true;
        bool objectBoundsOk = !requireObjectBounds || IsInsideObjectBounds(candidate);

        if (inBounds && objectBoundsOk)
        {
            lastAcceptedTargetPos = candidate;
            return candidate;
        }

        return lastAcceptedTargetPos;
    }

    Vector3 ClampCubeTargetPosition(Vector3 worldPosition)
    {
        if (!requireObjectBounds || boundsObject == null)
        {
            return worldPosition;
        }

        Collider[] colliders = boundsObject.GetComponentsInChildren<Collider>();
        if (colliders != null && colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Vector3 closest = colliders[i].ClosestPoint(worldPosition);
                if ((closest - worldPosition).sqrMagnitude <= 0.000001f)
                {
                    return worldPosition;
                }
            }

            float bestDist = float.MaxValue;
            Vector3 bestPoint = worldPosition;
            for (int i = 0; i < colliders.Length; i++)
            {
                Vector3 closest = colliders[i].ClosestPoint(worldPosition);
                float dist = (closest - worldPosition).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPoint = closest;
                }
            }
            return bestPoint;
        }

        if (TryGetObjectWorldBounds(boundsObject, out Bounds worldBounds))
        {
            return worldBounds.ClosestPoint(worldPosition);
        }

        return worldPosition;
    }

    public void SetAttractCollectables(bool enabled)
    {
        isAttractingCollectables = enabled;
    }

    void AttractNearbyCollectables()
    {
        if (!isAttractingCollectables || magnetTransform == null) return;

        Collider[] hits = Physics.OverlapSphere(magnetTransform.position, collectableTouchRadius, collectableLayer);
        foreach (Collider hit in hits)
        {
            float topOffset = hit.bounds.extents.y;

            Vector3 targetPos = magnetTransform.position - (hit.transform.up * topOffset);

            float speed = 10f; 
            hit.transform.position = Vector3.MoveTowards(hit.transform.position, targetPos, speed * Time.deltaTime);

            float distanceToTop = Vector3.Distance(hit.transform.position + (hit.transform.up * hit.bounds.extents.y), magnetTransform.position);

            if (distanceToTop < 0.1f)
            {
                Rigidbody rb = hit.attachedRigidbody;
                if (rb != null)
                {
                    rb.isKinematic = true;
                    hit.transform.parent.SetParent(magnetTransform, true);
                }
            }
        }
    }

    bool IsInsideObjectBounds(Vector3 worldPosition)
    {
        if (boundsObject == null)
        {
            return true;
        }

        if (!TryGetObjectWorldBounds(boundsObject, out Bounds worldBounds))
        {
            return true;
        }

        return worldBounds.Contains(worldPosition);
    }

    bool TryGetObjectWorldBounds(Transform root, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds(root.position, Vector3.zero);

        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasBounds;
    }
}
