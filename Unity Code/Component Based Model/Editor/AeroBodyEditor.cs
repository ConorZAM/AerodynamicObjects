using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(AeroBody)), CanEditMultipleObjects]
public class AeroBodyEditor : Editor
{
    AeroBody aeroBody;

    private void OnEnable()
    {
        aeroBody = (AeroBody)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ValidationTool[] validationTools = aeroBody.gameObject.GetComponents<ValidationTool>();

        if (validationTools.Length > 0 && GUILayout.Button("Validate Models"))
        {
            // Make sure the body is ready
            aeroBody.Initialise();

            // Store position and orientation so we can revert after the validation check
            Vector3 oldPosition = aeroBody.transform.position;
            Quaternion oldRotation = aeroBody.transform.rotation;

            aeroBody.transform.rotation = Quaternion.identity;
            aeroBody.transform.position = Vector3.zero;

            // Loop through all the validation tools we found and have them validate their corresponding components
            for (int i = 0; i < validationTools.Length; i++)
            {
                AerodynamicComponent component = (AerodynamicComponent)aeroBody.gameObject.GetComponent(validationTools[i].componentType);
                if (!component)
                {
                    Debug.LogError("Component expected but not found when attempting to validate " + validationTools[i].componentType.ToString());
                    continue;
                }
                validationTools[i].ValidateModel(aeroBody, component);
            }

            aeroBody.transform.rotation = oldRotation;
            aeroBody.transform.position = oldPosition;
        }
    }
}
