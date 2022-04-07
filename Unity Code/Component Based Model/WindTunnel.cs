using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindTunnel : MonoBehaviour
{
    Rigidbody rb;
    ConfigurableJoint joint;
    
    [Header("Properties")]
    public float planformArea = 1f;

    [Space(10)]
    [Header("Forces")]
    public Vector3 jointForce;
    public float lift;
    public float drag;
    //public Vector3 jointMoment;

    [Space(10)]
    [Header("Coefficients")]
    public float CL;
    public float CD;

    [Space(10)]
    [Header("Wind")]
    public Vector3 earthWind;
    public float rho = 1.2f;

    [Space(10)]
    [Header("Clamping")]
    public bool canRotateX;
    public bool canRotateY;
    public bool canRotateZ;

    [Space(10)]
    [Header("Body Rotation")]
    public Vector3 angularVelocity;
    public Vector3 rotation;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        ResetJoint();

        // Lock the necessary rotations on the joint
        SetJoint();
    }


    void FixedUpdate()
    {
        SetJoint();
        rb.angularVelocity = angularVelocity;

        CalculateForcesAndCoefficients();
    }

    public void SetRotation()
    {
        RemoveJoint();

        angularVelocity = Vector3.zero;
        rb.angularVelocity = angularVelocity;
        transform.rotation = Quaternion.Euler(rotation);

        // Then clamp the joint
        canRotateX = false;
        canRotateY = false;
        canRotateZ = false;

        AddJoint();
        SetJoint();
    }

    void ResetJoint()
    {
        RemoveJoint();
        AddJoint();
    }

    void RemoveJoint()
    {
        joint = GetComponent<ConfigurableJoint>();
        if (joint)
        {
            DestroyImmediate(joint);
        }
    }

    void AddJoint()
    {
        joint = gameObject.AddComponent<ConfigurableJoint>();
        
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = Vector3.zero;

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        SetJoint();
    }

    void CalculateForcesAndCoefficients()
    {
        jointForce = joint.currentForce;
        float weight = rb.mass * 9.81f;
        lift = weight - jointForce.y;

        Vector3 horizontalForce = jointForce;
        horizontalForce.y = 0;
        drag = horizontalForce.magnitude;

        float q = 0.5f * rho * earthWind.sqrMagnitude;
        float S = planformArea;

        CL = lift / (q * S);
        CD = drag / (q * S);
    }

    void CheckConflicts()
    {
        // Priorities go:
        // If we have some angular velocity set, then body can rotate
        // if we have no angular velocity set, then leave the rotate as it was
        canRotateX = angularVelocity.x != 0f || canRotateX;
        canRotateY = angularVelocity.y != 0f || canRotateY;
        canRotateZ = angularVelocity.z != 0f || canRotateZ;
    }

    void SetJoint()
    {
        // Compare angular velocity values with allowed rotations
        CheckConflicts();

        joint.angularXMotion = canRotateX ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
        joint.angularYMotion = canRotateY ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
        joint.angularZMotion = canRotateZ ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
    }
}
