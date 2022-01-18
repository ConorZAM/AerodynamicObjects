using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class AeroCoefficientExperiment : MonoBehaviour
{

    // This script is used to extract all of the values from the aerodynamic ellipsoid model
    // based on the variation of angle of attack. We assume that the wind is being
    // resolved correctly and that the transformations have been carried out properly and so we
    // can skip to injecting the angles directly!


    Aerodynamics aero;
    Rigidbody rb;
    // If path is just a file name like this then unity will use the root of the project folder
    // to create the file. E.g. C:\Users\User Name\Unity Projects\My Project\
    public string rootPath = "Coefficients experiment";
    string path;

    // Start is called before the first frame update
    void Start()
    {
        aero = GetComponent<Aerodynamics>();
        rb = GetComponent<Rigidbody>();

        Debug.Log("Experiment Running...");

        float span = Mathf.PI;

        path = rootPath + " bc 1.txt";
        aero.transform.localScale = new Vector3(span, 1, 1);
        DoExperiment();

        path = rootPath + " bc 0_5.txt";
        aero.transform.localScale = new Vector3(span, 0.5f, 1);
        DoExperiment();

        path = rootPath + " bc 0_1.txt";
        aero.transform.localScale = new Vector3(span, 0.1f, 1);
        DoExperiment();

        path = rootPath + " bc 0.txt";
        aero.transform.localScale = new Vector3(span, 0, 1);
        DoExperiment();

        Debug.Log("Done.");
        Debug.Break();
    }

    void DoExperiment()
    {
        File.Delete(path);

        File.AppendAllText(path, "alpha\tmodel alpha\tCL pre stall\tCL post stall\tstall filter\tCL\tCoP\tCM0\tCM delta\tCD profile\tCD induced\tCD" + Environment.NewLine);

        for (float alpha = -90f; alpha <= 90f; alpha += 1f)
        {
            float alpha_rad = Mathf.Deg2Rad * alpha;

            aero.externalFlowVelocity_inEarthFrame = new Vector3(0, -Mathf.Sin(alpha_rad), Mathf.Cos(alpha_rad));

            // Just need to make sure that these are all being done appropriately!
            // Might be worth creating some functions in the aero object which call the right functions depending on use case
            // e.g. DoAerodynamicsFromAngles - would skip the derivation of alpha and beta and go from there onwards

            aero.GetReferenceFrames_1();
            aero.GetEllipsoidProperties_2();
            aero.CalculateAerodynamics_3_to_9();
            
            // This needs changing but I cba typing out alllllll those variables above!
            File.AppendAllText(path, string.Join("\t", new string[] {
                alpha.ToString("F4"), aero.alpha.ToString("F4"), aero.CL_preStall.ToString("F4"), aero.CL_postStall.ToString("F4"), aero.preStallFilter.ToString("F4"),
                aero.CL.ToString("F4"), aero.CoP_z.ToString("F4"), aero.CM_0.ToString("F4"), aero.CM_delta.ToString("F4"), aero.CD_profile.ToString("F4"),
                aero.CD_induced.ToString("F4"), aero.CD.ToString("F4"), Environment.NewLine }));
        }
    }
}