using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class DimensionsExperiment : MonoBehaviour
{

    // This script is used to extract all of the values from the aerodynamic ellipsoid model
    // based on the variation of angle of attack. We assume that the wind is being
    // resolved correctly and that the transformations have been carried out properly and so we
    // can skip to injecting the angles directly!


    Aerodynamics aero;
    Rigidbody rb;
    // If path is just a file name like this then unity will use the root of the project folder
    // to create the file. E.g. C:\Users\User Name\Unity Projects\My Project\
    public string path = "Dimensions experiment.txt";


    // Start is called before the first frame update
    void Start()
    {
        aero = GetComponent<Aerodynamics>();
        rb = GetComponent<Rigidbody>();

        File.Delete(path);
        File.AppendAllText(path, "AR\tkAR\tb over c\tthickness correction" + Environment.NewLine);

        for (float ii = 0f; ii <= 1f; ii += 0.02f)
        {
            float AR = ii * 10f;
            float boverc = ii;

            // Do aspect ratio first, remember: AR = span / area and area = pi * span * chord because it's an ellipse
            aero.transform.localScale = new Vector3(Mathf.PI * AR, 0, 1);

            // Just need to make sure that these are all being done appropriately!
            // Might be worth creating some functions in the aero object which call the right functions depending on use case
            // e.g. DoAerodynamicsFromAngles - would skip the derivation of alpha and beta and go from there onwards

            aero.GetReferenceFrames_1();
            aero.externalFlowVelocity_inEarthFrame = Vector3.back;
            aero.CalculateAerodynamics_3_to_9();

            string kAR = aero.aspectRatioCorrection_kAR.ToString("F4");

            // Then do thickness over chord
            aero.transform.localScale = new Vector3(1, Mathf.Clamp(boverc, 0, 1), 1);

            aero.GetReferenceFrames_1();
            aero.externalFlowVelocity_inEarthFrame = Vector3.back;
            aero.CalculateAerodynamics_3_to_9();

            string kt = aero.thicknessCorrection_kt.ToString("F4");

            File.AppendAllText(path, string.Join("\t", new string[] {
                AR.ToString("F4"), kAR, boverc.ToString("F4"), kt, Environment.NewLine }));
        }


        Debug.Log("Done.");
        Debug.Break();
    }

}