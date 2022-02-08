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

        windArrow = new Arrow(arrowHead, arrowBody, ArrowSettings.windColour,"WindArrow", aeroBody.transform);
    }


    void FixedUpdate()
    {
        float windLength = aeroBody.earthFrame.windVelocity.magnitude ;
        Vector3 windDir = -aeroBody.earthFrame.windVelocity_normalised;
        Vector3 arrowRoot = aeroBody.transform.position - offset* windDir;

        windArrow.SetPositionAndRotation(scale, windLength*sensitivity, arrowRoot, windDir);
    }
}
