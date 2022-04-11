using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindArrow : ComponentArrows
{
    // Wind arrow is just the wind on the aero body

    Arrow windArrow;
    AeroBody aeroBody;

    void Awake()
    {
        pointAtPoint = true;
        aeroBody = GetComponent<AeroBody>();
        windArrow = new Arrow(ArrowSettings.Singleton().windColour, "Wind Arrow", transform);
    }


    void Update()
    {
        aeroBody.ResolveWindAndDimensions_1_to_6();
        SetArrowPositionAndRotationFromVector(windArrow, -aeroBody.earthFrame.windVelocity, aeroBody.transform.position);
    }
}
