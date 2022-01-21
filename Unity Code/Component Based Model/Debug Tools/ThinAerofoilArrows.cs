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

        LiftArrow = new Arrow(arrowHead, arrowBody, ArrowSettings.liftColour);
        InducedDragArrow = new Arrow(arrowHead, arrowBody, ArrowSettings.dragColour);
    }


    void FixedUpdate()
    {
        Vector3 centreOfPressure_earthFrame = aeroBody.transform.position + aeroBody.TransformEABToEarth(new Vector3(0, 0, component.CoP_z));
        Vector3 lift_earthFrame = aeroBody.TransformBodyToEarth(component.lift_bodyFrame);
        Vector3 inducedDrag_earthFrame = aeroBody.TransformBodyToEarth(component.inducedDrag_bodyFrame);
        LiftArrow.SetPositionAndRotation((1f / scaling) * ArrowSettings.arrowAspectRatio, lift_earthFrame.magnitude / scaling, centreOfPressure_earthFrame, lift_earthFrame.normalized);
        InducedDragArrow.SetPositionAndRotation((1f / scaling) * ArrowSettings.arrowAspectRatio, inducedDrag_earthFrame.magnitude / scaling, centreOfPressure_earthFrame, inducedDrag_earthFrame.normalized);
    }
}
