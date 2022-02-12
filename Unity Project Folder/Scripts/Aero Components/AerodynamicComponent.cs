using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[RequireComponent(typeof(AeroBody))]
public class AerodynamicComponent : MonoBehaviour
{
    /* This component is the base class for all of the aerodynamics
     * models that are included in the Aerodynamic Objects package
     * Each component can be added to an AeroBody, increasing the
     * fidelity of the aerodynamics model used to simulate the
     * AeroBody's motion.
     */

    // The resultant forces and moments of an aerodynamic component should be
    // provided in the body reference frame so we only need to transform the entire sum
    // of forces and moments into earth coordinates once, instead of doing a transform for each component
    public Vector3 resultantForce_bodyFrame;
    public Vector3 resultantMoment_bodyFrame;


    public virtual void RunModel(AeroBody aeroBody)
    {
        // This function will use the wind and dimensions of the AeroBody to
        // compute the relevant forces and moments for the component

    }

    private void Reset()
    {
        AeroBody body = GetComponent<AeroBody>();
        body.GetAeroComponents();
    }
}
