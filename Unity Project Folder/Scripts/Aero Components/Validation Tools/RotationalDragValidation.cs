using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationalDragValidation : ValidationTool
{

    public RotationalDragValidation()
    {
        componentType = typeof(RotationalDragComponent);
    }

    public override void ValidateModel(AeroBody aeroBody, AerodynamicComponent component)
    {
        // Unsure what to do for this as it needs to be some kind of spin down experiment
        // we could do some of our own integration here... Sounds like a paaaaaiiiinnnn though
        // and might be misleading as the validation would then give slightly different answers to Unity
    }
}
