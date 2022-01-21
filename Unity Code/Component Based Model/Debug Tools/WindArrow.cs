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

        windArrow = new Arrow(arrowHead, arrowBody, ArrowSettings.windColour);
    }


    void FixedUpdate()
    {
        float windLength = aeroBody.earthFrame.windVelocity.magnitude / scaling;
        Vector3 windDir = -aeroBody.earthFrame.windVelocity_normalised;
        Vector3 arrowRoot = aeroBody.transform.position - windLength * windDir;

        windArrow.SetPositionAndRotation((1f/scaling) * ArrowSettings.arrowAspectRatio, windLength, arrowRoot, windDir);
    }
}
