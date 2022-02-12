using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnusEffectComponent : AerodynamicComponent
{
    // Lift generated due to rotation of the body

    Vector3 RHO;
    Vector3 CLr;
    float vSquared;

    public override void RunModel(AeroBody aeroBody)
    {
        // I think, in the future this should just be 2*pi*alpha_m where alpha_m is the apparent alpha due to rotation

        // Magnus effect coefficient
        vSquared = aeroBody.aeroBodyFrame.windVelocity.sqrMagnitude;
        if (vSquared == 0f)
        {
            resultantForce_bodyFrame = Vector3.zero;
            resultantMoment_bodyFrame = Vector3.zero;
        }
        else
        {
            RHO = Vector3.Scale(aeroBody.volumeVector, aeroBody.aeroBodyFrame.angularWindVelocity);
            CLr = 4f * Vector3.Cross(aeroBody.aeroBodyFrame.windVelocity, RHO) / (aeroBody.aeroBodyFrame.windVelocity.sqrMagnitude * aeroBody.planformArea);

            // Proper hacky way to clamp the size of the magnus coefficient - without this CLr can reach 40+
            if (CLr.magnitude > 3f)
            {
                CLr = 3f * CLr.normalized;
            }

            resultantForce_bodyFrame = -CLr * aeroBody.qS;
            resultantMoment_bodyFrame = Vector3.zero;
        }
    }
}
