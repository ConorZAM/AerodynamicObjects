using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomAirfoilComponent : AerodynamicComponent
{
    // Thin Aero Foil aerodynamics uses a moving centre of pressure
    // and a normal force model to determine the lift and moment due to lift
    // Only induced drag is added by this component!

    public AnimationCurve liftCurve;
    public AnimationCurve dragCurve;
    public AnimationCurve pitchingMomentCurve;

    public float alpha0, effectiveAlpha;

    // Coefficients
    public float CL, CD, CM;

    public Vector3 lift_bodyFrame;              // (Nm)
    public Vector3 inducedDrag_bodyFrame;       // (Nm)
    public Vector3 centreOfPressure_earth;      // (m)

    public override void RunModel(AeroBody aeroBody)
    {
        // Zero lift angle is set based on the amount of camber. This is physics based
        alpha0 = -aeroBody.EAB.camberRatio;
        effectiveAlpha = aeroBody.alpha - alpha0;

        CL = liftCurve.Evaluate(effectiveAlpha);
        CD = dragCurve.Evaluate(effectiveAlpha);
        CM = pitchingMomentCurve.Evaluate(effectiveAlpha);

        // Convert coefficients to forces and moments
        float qS = aeroBody.dynamicPressure * aeroBody.planformArea;
        Vector3 liftDirection = Vector3.Cross(aeroBody.aeroBodyFrame.windVelocity_normalised, aeroBody.angleOfAttackRotationVector).normalized;
        lift_bodyFrame = qS * CL * liftDirection;
        inducedDrag_bodyFrame = -CD * qS * aeroBody.aeroBodyFrame.windVelocity_normalised;
        resultantForce_bodyFrame = lift_bodyFrame + inducedDrag_bodyFrame;

        // Forces are applied at the centre of the wing - we have a pitching moment coefficient too
        forcePointOfAction_earthFrame = aeroBody.transform.position;
        resultantForce_earthFrame = aeroBody.TransformDirectionBodyToEarth(resultantForce_bodyFrame);

        resultantMoment_bodyFrame = new Vector3(CM * qS * aeroBody.EAB.chord_c, 0, 0);
        resultantMoment_bodyFrame = aeroBody.TransformDirectionEABToBody(resultantMoment_bodyFrame);
        resultantMoment_earthFrame = aeroBody.TransformDirectionBodyToEarth(resultantMoment_bodyFrame);
    }
}

