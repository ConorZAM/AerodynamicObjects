using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AeroGroup))]
public class AeroGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AeroGroup aeroGroup = (AeroGroup)target;
        DrawDefaultInspector();

        if(GUILayout.Button("Get Child AeroBody Components")){
            aeroGroup.GetChildBodies();
        }
    }
}
