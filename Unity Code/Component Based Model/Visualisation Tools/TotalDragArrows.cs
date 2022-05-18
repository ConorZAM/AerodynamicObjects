using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotalDragArrows : ComponentArrows
{
    ThinAerofoilComponent inducedDragComponent;
    TranslationalDragComponent translationalDragComponent;
    AeroBody aeroBody;

    Arrow DragArrow;

    void Awake()
    {
        if (this.enabled)
        {
            inducedDragComponent = GetComponent<ThinAerofoilComponent>();
            translationalDragComponent = GetComponent<TranslationalDragComponent>();
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
        inducedDragComponent.RunModel(aeroBody);
        translationalDragComponent.RunModel(aeroBody);

        // Get the separate lift and induced drag force vectors in earth frame
        Vector3 inducedDrag_earthFrame = aeroBody.TransformDirectionBodyToEarth(inducedDragComponent.inducedDrag_bodyFrame);

        // This isn't completely accurate as the induced drag acts at the centre of pressure while translational
        // drag acts at the geometric centre in the AO model. But it's a happy medium for now.
        SetArrowPositionAndRotationFromVector(DragArrow, translationalDragComponent.resultantForce_earthFrame + inducedDrag_earthFrame, translationalDragComponent.forcePointOfAction_earthFrame);
    }
}
