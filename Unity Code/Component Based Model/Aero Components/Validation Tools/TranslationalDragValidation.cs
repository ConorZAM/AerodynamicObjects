using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslationalDragValidation : ValidationTool
{

    public AnimationCurve alphaCdPressure0Plot;
    public AnimationCurve alphaCdPressure90Plot;
    public AnimationCurve alphaCdSkinFriction0Plot;
    public AnimationCurve alphaCdSkinFriction90Plot;
    public AnimationCurve alphaCdPlot;

    public TranslationalDragValidation()
    {
        componentType = typeof(TranslationalDragComponent);
    }


    public override void ValidateModel(AeroBody aeroBody, AerodynamicComponent component)
    {
        TranslationalDragComponent linearDragComponent = (TranslationalDragComponent)component;
        ResetPlots();
        // The veloicty of the body, we're assuming that the body and earth frames are
        // lined up when running this experiment for simplicity. We could force the rotation
        // but that might lead to other issues down the line so just remember for now!
        Vector3 wind = Vector3.zero;

        // The angle of attack passed to the model - this will range from -90 <= a <= 90 degrees
        // any angle of attack > 90 degrees would revert to lower than 90 degrees and incur
        // a sideslip angle to rotate the aerodynamic body to face into the wind
        float alpha_in;

        // Check the range of feasible angles of attack
        for (alpha_in = -Mathf.PI / 2f; alpha_in <= Mathf.PI / 2f; alpha_in += 0.01f)
        {
            // As the wind is actually defined to be the body's velocity (weird I know)
            // we need a positive angle of attack to correspond with a negative vertical
            // component of the wind. The horizontal component should always be positive as
            // we resolve the body into the wind
            wind.z = Mathf.Cos(alpha_in);
            // Note the minus sign here to ensure +ve alpha has -ve vertical wind
            wind.y = -Mathf.Sin(alpha_in);
            wind.x = 0; // Just in case...

            // This accounts for the body not having conventional span, thickness, chord as x, y, z
            wind = aeroBody.TransformBodyToEarth(wind);

            // Just going to do everything for now to be thorough - this should also catch
            // any errors hiding!
            aeroBody.GetEllipsoid_1_to_2();
            aeroBody.SetWind_3(wind, Vector3.zero);
            aeroBody.GetFlowCharacteristics_4();
            aeroBody.GetAeroAngles_5();
            aeroBody.GetEquivalentAerodynamicBody_6();

            linearDragComponent.RunModel(aeroBody);

            alphaCdPressure0Plot.AddKey(alpha_in, linearDragComponent.CD_pressure_0aoa);
            alphaCdPressure90Plot.AddKey(alpha_in, linearDragComponent.CD_pressure_90aoa);
            alphaCdSkinFriction0Plot.AddKey(alpha_in, linearDragComponent.CD_shear_0aoa);
            alphaCdSkinFriction90Plot.AddKey(alpha_in, linearDragComponent.CD_shear_90aoa);
            alphaCdPlot.AddKey(alpha_in, linearDragComponent.CD);
        }
    }

    void ResetPlots()
    {
        alphaCdPressure0Plot = new AnimationCurve();
        alphaCdPressure90Plot = new AnimationCurve();
        alphaCdSkinFriction0Plot = new AnimationCurve();
        alphaCdSkinFriction90Plot = new AnimationCurve();
        alphaCdPlot = new AnimationCurve();
    }
}
