using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReferenceFrame
{
    // Useful directions - I don't think they get used at all actually...
    public Vector3 xDirection, yDirection, zDirection;  // (unit vector)

    // The rotation from the previous frame of reference to this frame of reference
    public Quaternion objectToFrameRotation = Quaternion.identity;

    // The rotation from the current frame of reference back to the previous frame
    public Quaternion inverseObjectToFrameRotation = Quaternion.identity;

    public void SetDirectionVectors(Vector3 x, Vector3 y, Vector3 z)
    {
        xDirection = x;
        yDirection = y;
        zDirection = z;
    }

    public void SetFrameRotation(Quaternion rotation)
    {
        // This was the logical approach to me... but it seems they need to be reverse
        //objectToFrameRotation = rotation;
        //inverseObjectToFrameRotation = Quaternion.Inverse(rotation);

        objectToFrameRotation = Quaternion.Inverse(rotation);
        inverseObjectToFrameRotation = rotation;
    }


    // ==================== Needs updating ===============================
    // This all needs changing so that we're not considering the wind in all
    // the reference frames. For starters, we need to take out "wind velocity"
    // from the coordinate frames. We can have the velocity of the frame itself
    // which makes much more sense than embedding a negative in there somewhere.
    // The confusing part then becomes figuring out where the wind velocity comes
    // from and how we can use it in the rest of the model. Sounds like a big job.
    //
    // I think we should look at something like:
    // 1. Earth axes
    // 2. Wind axes
    // 3. Local axes
    // 4. EAB axes
    // Where local and EAB axes are derived from the wind axes, meaning that they inherit
    // the velocity of those axes

    // Wind resolved into this coordinate frame
    public Vector3 windVelocity;                    // (m/s)
    public Vector3 windVelocity_normalised;         // (unit vector
    public Vector3 angularWindVelocity;             // (rad/s)
    public Vector3 angularWindVelocity_normalised;  // (unit vector)

    public void SetResolvedWind(Vector3 linearWind, Vector3 angularWind)
    {
        // The normalisations here are probably wasted computation... it's an
        // optimisation incase the normalised vector is needed more than once
        windVelocity = objectToFrameRotation * linearWind;
        windVelocity_normalised = windVelocity.normalized;

        angularWindVelocity = objectToFrameRotation * angularWind;
        angularWindVelocity_normalised = angularWindVelocity.normalized;
    }
}
