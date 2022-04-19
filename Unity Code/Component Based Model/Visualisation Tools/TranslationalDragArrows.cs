using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslationalDragArrows : ComponentArrows
{

    // Translational drag is an easy one, the drag acts at the aero body position

    Arrow DragArrow;

    TranslationalDragComponent component;
    AeroBody aeroBody;

    void Awake()
    {
        component = GetComponent<TranslationalDragComponent>();
        aeroBody = GetComponent<AeroBody>();

        DragArrow = new Arrow(ArrowSettings.Singleton().dragColour, "Drag Arrow", transform);
    }


    void Update()
    {
        // Taking the computational hit to get the up to date values
        aeroBody.ResolveWindAndDimensions_1_to_6();
        component.RunModel(aeroBody);

        // Draw the arrow
        SetArrowPositionAndRotationFromVector(DragArrow, component.resultantForce_earthFrame, component.forcePointOfAction_earthFrame);
    }
}
