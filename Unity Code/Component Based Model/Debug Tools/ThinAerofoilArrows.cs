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

        LiftArrow = new Arrow(arrowHead, arrowBody, ArrowSettings.liftColour,"LiftArrow",aeroBody.transform);
        InducedDragArrow = new Arrow(arrowHead, arrowBody, ArrowSettings.dragColour,"InducedDragArrow", aeroBody.transform);
    }


    void FixedUpdate()
    {
        Vector3 centreOfPressure_earthFrame = aeroBody.transform.position + aeroBody.TransformEABToEarth(new Vector3(0, 0, component.CoP_z));
        Vector3 lift_earthFrame = aeroBody.TransformBodyToEarth(component.lift_bodyFrame);
        Vector3 inducedDrag_earthFrame = aeroBody.TransformBodyToEarth(component.inducedDrag_bodyFrame);
        var radius = scale;
        var length= lift_earthFrame.magnitude * sensitivity;
        var rootPosition = centreOfPressure_earthFrame + lift_earthFrame.normalized * (length + offset);
        var direction = lift_earthFrame.normalized;
        LiftArrow.SetPositionAndRotation(radius ,length , rootPosition,direction );

        radius =  scale;
        length = inducedDrag_earthFrame.magnitude * sensitivity;
        rootPosition = centreOfPressure_earthFrame + inducedDrag_earthFrame.normalized * (length+offset);
        direction = inducedDrag_earthFrame.normalized;

        InducedDragArrow.SetPositionAndRotation(radius, length, rootPosition, direction);

    }
}
