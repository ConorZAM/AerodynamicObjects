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
        if (this.enabled)
        {
            component = GetComponent<TranslationalDragComponent>();
            aeroBody = GetComponent<AeroBody>();

            DragArrow = new Arrow(ArrowSettings.Singleton().dragColour, "Drag Arrow", transform);
        }
    }

    private void OnDisable()
    {
        if (DragArrow != null)
        {
            Destroy(DragArrow.head.gameObject);
            Destroy(DragArrow.body.gameObject);
        }
    }


    void Update()
    {
        // Taking the computational hit to get the up to date values
        aeroBody.ResolveWindAndDimensions_1_to_6();
        component.RunModel(aeroBody);

        if (useCoefficientForScale)
        {
            // Draw the arrow using the normalised force vector, scaled up by the drag coefficient
            // Need to use the absolute value of the coefficient because we already have the direction from the force
            SetArrowPositionAndRotationFromVector(DragArrow, Mathf.Abs(component.CD) * component.resultantForce_earthFrame.normalized, component.forcePointOfAction_earthFrame);
        }
        else
        {
            // Draw the arrow
            SetArrowPositionAndRotationFromVector(DragArrow, component.resultantForce_earthFrame, component.forcePointOfAction_earthFrame);
        }

        
    }
}
