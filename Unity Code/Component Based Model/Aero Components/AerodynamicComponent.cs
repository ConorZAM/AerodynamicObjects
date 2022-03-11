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
        rigidbody.AddForceAtPosition(resultantForce_earthFrame, forcePointOfAction_earthFrame);
    }

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
}