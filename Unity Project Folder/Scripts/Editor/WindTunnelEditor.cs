using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WindTunnel))]
public class WindTunnelEditor : Editor
{
    WindTunnel windTunnel;

    private void OnEnable()
    {
        windTunnel = (WindTunnel)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Set Rotation"))
        {
            windTunnel.SetRotation();
        }
    }
}
