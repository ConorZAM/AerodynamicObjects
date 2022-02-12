using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AeroValidation))]
public class ValidationEditor : Editor
{
    AeroValidation aeroValidation;

    private void OnEnable()
    {
        aeroValidation = (AeroValidation)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Validate Model"))
        {
            aeroValidation.Initialise();
            aeroValidation.CheckAlphaRange();
        }
    }
}
