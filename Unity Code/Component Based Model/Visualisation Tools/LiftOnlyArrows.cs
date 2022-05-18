using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiftOnlyArrows : ComponentArrows
{
    Arrow LiftArrow;

    ThinAerofoilComponent component;
    AeroBody aeroBody;

    void Awake()
    {
        if (this.enabled)
        {
            component = GetComponent<ThinAerofoilComponent>();
            aeroBody = GetComponent<AeroBody>();

            LiftArrow = new Arrow(ArrowSettings.Singleton().liftColour, "Lift Arrow", transform);
        }
    }

    private void OnDisable()
    {
        if (LiftArrow != null)
        {
            Destroy(LiftArrow.head.gameObject);
            Destroy(LiftArrow.body.gameObject);
        }
    }


    void Update()
    {
        // Taking the computational hit to get the up to date values
        aeroBody.ResolveWindAndDimensions_1_to_6();
        component.RunModel(aeroBody);

        // Get the separate lift and induced drag force vectors in earth frame
        Vector3 lift_earthFrame = aeroBody.TransformDirectionBodyToEarth(component.lift_bodyFrame);

        SetArrowPositionAndRotationFromVector(LiftArrow, lift_earthFrame, component.forcePointOfAction_earthFrame);
    }
}
