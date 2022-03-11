using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThinAerofoilArrows : ComponentArrows
{

    /* Thin aerofoil has the following forces, moments:
     *  - Lift
     *  - Induced Drag
     *  - Pitching moment
     */

    Arrow LiftArrow;
    Arrow InducedDragArrow;

    ThinAerofoilComponent component;
    AeroBody aeroBody;

    void Awake()
    {
        component = GetComponent<ThinAerofoilComponent>();
        aeroBody = GetComponent<AeroBody>();

        LiftArrow = new Arrow(ArrowSettings.Singleton().liftColour, "Lift Arrow", transform);
        InducedDragArrow = new Arrow(ArrowSettings.Singleton().dragColour, "Induced Drag Arrow", transform);
    }


    void FixedUpdate()
    {
        // Lift and induced drag act at the centre of pressure location
        Vector3 centreOfPressure_earthFrame = component.forcePointOfAction_earthFrame; // aeroBody.transform.position + aeroBody.TransformEABToEarth(new Vector3(0, 0, component.CoP_z));
        
        // Get the lift and induced drag force vectors in earth frame
        Vector3 lift_earthFrame = aeroBody.TransformDirectionBodyToEarth(component.lift_bodyFrame);
        Vector3 inducedDrag_earthFrame = aeroBody.TransformDirectionBodyToEarth(component.inducedDrag_bodyFrame);

        SetArrowPositionAndRotationFromVector(LiftArrow, lift_earthFrame, centreOfPressure_earthFrame);
        SetArrowPositionAndRotationFromVector(InducedDragArrow, inducedDrag_earthFrame, centreOfPressure_earthFrame);
    }
}
