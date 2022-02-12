using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnusEffectValidation : ValidationTool
{

    public MagnusEffectValidation()
    {
        componentType = typeof(MagnusEffectComponent);
    }

    // Not sure what to validate the model with here yet - it needs a different set of stuff to the usual :/

    public override void ValidateModel(AeroBody aeroBody, AerodynamicComponent component)
    {
        base.ValidateModel(aeroBody, component);
    }
}
