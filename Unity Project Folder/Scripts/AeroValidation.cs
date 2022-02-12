using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AeroValidation : MonoBehaviour
{
    // This is going to be our testing script. It's going to get pretty hairy I imagine but
    // we just want to input various properties and see what comes out of the model. I think
    // it would be best to just show the model outputs rather than trying to compute an error
    // between the model and expected outputs as I would just be copying the model's code into
    // this script and comparing the two - not very good validation!

    Aerodynamics aero;
    Rigidbody rb;

    

    // This is the absolute difference between the alpha we use as input and the alpha the model outputs
    public AnimationCurve alphaError;

    // Plot of alpha and lift coefficient
    public AnimationCurve alphaClPlot;

    // Plot of alpha and drag coefficient
    public AnimationCurve alphaCdPlot;

    // Plot of alpha and pitch coefficient
    public AnimationCurve alphaCmPlot;

    // Plot of alpha and centre of pressure location
    public AnimationCurve alphaCopPlot;

    public void Initialise()
    {
        aero = GetComponent<Aerodynamics>();
        rb = GetComponent<Rigidbody>();

        aero.Initialise();

        // Easiest way to just clear all the plots?
        alphaError = new AnimationCurve();
        alphaClPlot = new AnimationCurve();
        alphaCdPlot = new AnimationCurve();
        alphaCmPlot = new AnimationCurve();
        alphaCopPlot = new AnimationCurve();
    }

    public void CheckAlphaRange()
    {
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
            wind = aero.TransformBodyToEarth(wind);

            // Just going to do everything for now to be thorough - this should also catch
            // any errors hiding!
            aero.GetReferenceFrames_1();
            aero.GetEllipsoidProperties_2();
            aero.SetWind_3(wind, Vector3.zero);
            aero.GetFlowCharacteristics_4();
            aero.GetAeroAngles_5();
            aero.GetEquivalentAerodynamicBody_6();
            aero.GetAerodynamicCoefficients_7();
            aero.GetDampingTorques_8();
            aero.GetAerodynamicForces_9();

            AddCoefficientPlots(alpha_in);
        }
    }

    private void AddCoefficientPlots(float alpha)
    {
        alphaError.AddKey(alpha, alpha - aero.alpha);
        alphaClPlot.AddKey(alpha, aero.CL);
        alphaCdPlot.AddKey(alpha, aero.CD);
        alphaCmPlot.AddKey(alpha, aero.CM);
        alphaCopPlot.AddKey(alpha, aero.CoP_z);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
