using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlSurface : MonoBehaviour
{
    /* Working on a generic control surface script which can automatically generate a hinge object
     * Need to clarify things like where this script should go in the hierarchy
     * 
     * Also need to test which way around the hinge should be defined to follow convention
     */


    public AeroBody aeroBody;
    public Transform moveableSurface;
    public Transform hinge;

    public float Angle = 0f;
    public float Trim = 0f;

    Quaternion defaultHingeRotation;

    public Vector3 HingeStart { get { return transform.TransformPoint(hingeStart); } set { hingeStart = transform.InverseTransformPoint(value); } }
    public Vector3 HingeEnd { get { return transform.TransformPoint(hingeEnd); } set { hingeEnd = transform.InverseTransformPoint(value); } }

    private Vector3 hingeStart = Vector3.left;
    private Vector3 hingeEnd = Vector3.right;

    public float minControlThrow = -15f;
    public float maxControlThrow = 15f;

    public float camberScale = 0.05f;


    void Awake()
    {
        if (moveableSurface && aeroBody)
        {
            UpdateHinge();
        }
        else
        {
            Debug.LogWarning(name + " is missing required components. Disabling for now.");
        }
    }


    void FixedUpdate()
    {
        //controlHinge.localRotation = trim * Quaternion.Euler(delta, 0, 0);

        float clampedAngle = Mathf.Clamp(Angle + Trim, minControlThrow, maxControlThrow);

        hinge.localRotation = defaultHingeRotation * Quaternion.Euler(0, 0, clampedAngle);

        //hinge.localEulerAngles = new Vector3(0, 0, delta);
        float camber = clampedAngle;
        if (camber > 180) camber -= 360;
        //if (camber < -180) camber += 360;

        // Minus sign here, not sure who's got things the wrong way around...
        camber = -camber * Mathf.Deg2Rad;
        aeroBody.SetFlapCamber(camberScale * camber);
    }

    public void UpdateHinge()
    {
        if(hinge == null)
        {
            GameObject go = new GameObject(name + " hinge");
            go.transform.parent = transform;
            hinge = go.transform;
        }

        moveableSurface.SetParent(null);

        // Align and position the hinge object
        hinge.forward = (HingeEnd - HingeStart).normalized;
        hinge.position = (HingeStart + HingeEnd) / 2f;

        moveableSurface.SetParent(hinge);

        defaultHingeRotation = hinge.localRotation;
    }
}
