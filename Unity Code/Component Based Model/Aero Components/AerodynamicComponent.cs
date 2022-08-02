using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[RequireComponent(typeof(AeroBody))]
public class AerodynamicComponent : MonoBehaviour
{
    /* This component is the base class for all of the aerodynamics
     * models that are included in the Aerodynamic Objects package
     * 
     * Components can be added to an AeroBody, increasing the
     * fidelity of the overall aerodynamics model used to simulate the
     * AeroBody's motion. Note that including components like this means
     * the models cannot interact with each other, a component must include
     * all interactions within itself. For example, the lift induced drag
     * must be included in the lift model component, not the drag component
     */

    // The resultant forces and moments of an aerodynamic component should be
    // provided in the body reference frame so we only need to transform the entire sum
    // of forces and moments into earth coordinates once, instead of doing a transform for each component
    public Vector3 resultantForce_bodyFrame;
    public Vector3 resultantMoment_bodyFrame;
    public Vector3 resultantMoment_earthFrame;

    // These components make more sense for applying forces to rigidbodies which do not align with
    // the aero body frame!
    public Vector3 resultantForce_earthFrame;
    public Vector3 forcePointOfAction_earthFrame;

    public virtual void RunModel(AeroBody aeroBody)
    {
        // This function will use the wind and dimensions of the AeroBody to
        // compute the relevant forces and moments for the component
    }

    public virtual void ApplyForces(Rigidbody rigidbody)
    {
        // This function allows for centre of pressure force application
        // Not sure how to account for moment due to camber though...

        if (Vector3ContainsNaN(resultantForce_earthFrame))
        {
            Debug.LogWarning("Resultant force contains NaN, applying no force instead");
        }
        else
        {
            rigidbody.AddForceAtPosition(resultantForce_earthFrame, forcePointOfAction_earthFrame);
        }

        if (Vector3ContainsNaN(resultantMoment_earthFrame))
        {
            Debug.LogWarning("Resultant torque contains NaN, applying no torque instead");
        }
        else
        {
            // I need to make sure the body and rigid body frames line up, if not then I need to correct for this
            rigidbody.AddTorque(resultantMoment_earthFrame);
        }
    }

    // ================================ EXPERIMENTAL ===============================================

    //public float delta = 0.9f;
    //public void SaturateForces(AeroBody body)
    //{
    //    // Acceleration due to the force (a = F/m)
    //    float acceleration = resultantForce_earthFrame.magnitude / body.rb.mass;

    //    // Assuming forward Euler integration, get the velocity change due to "impulse"
    //    float deltaVelocity = acceleration * Time.fixedDeltaTime;

    //    // If that velocity is going to change the direction...
    //    if (deltaVelocity > body.aeroBodyFrame.windVelocity.magnitude)
    //    {
    //        // Set force to be just the delta velocity which can't overshoot
    //        resultantForce_earthFrame = delta * (body.aeroBodyFrame.windVelocity.magnitude / Time.fixedDeltaTime) * body.rb.mass * resultantForce_earthFrame.normalized;
    //    }
    //}

    // ==============================================================================================

    private void OnEnable()
    {
        SubscribeToAeroEvents();
    }

    private void OnDisable()
    {
        UnSubscribeFromAeroEvents();
    }

    void SubscribeToAeroEvents()
    {
        // Don't need to check for null reference here as these components require an AeroBody
        GetComponent<AeroBody>().runModelEvent += RunModel;
        GetComponent<AeroBody>().applyForcesEvent += ApplyForces;
    }

    void UnSubscribeFromAeroEvents()
    {
        // Don't need to check for null reference here as these components require an AeroBody
        GetComponent<AeroBody>().runModelEvent -= RunModel;
        GetComponent<AeroBody>().applyForcesEvent -= ApplyForces;
    }

    public static bool Vector3ContainsNaN(Vector3 vector)
    {
        if (float.IsNaN(vector.x))
            return true;
        if (float.IsNaN(vector.y))
            return true;
        if (float.IsNaN(vector.z))
            return true;

        return false;
    }
}