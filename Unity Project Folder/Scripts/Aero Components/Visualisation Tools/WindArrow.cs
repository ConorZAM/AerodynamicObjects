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
        aeroBody = GetComponent<AeroBody>();

        windArrow = new Arrow(ArrowSettings.Singleton().windColour, "Wind Arrow", transform);
    }


    void FixedUpdate()
    {
        SetArrowPositionAndRotationFromVector(windArrow, -aeroBody.earthFrame.windVelocity, aeroBody.transform.position);
    }
}