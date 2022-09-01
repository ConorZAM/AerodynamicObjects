using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ControlSurface))]
public class ControlSurfaceEditor : Editor
{
    Color leftColour = Color.red;
    Color rightColour = Color.green;

    public void OnSceneGUI()
    {
        ControlSurface controlSurface = (ControlSurface)target;

        // Draw the handles for each hinge point

        // Left
        Handles.Label(controlSurface.HingeStart, "Port");
        controlSurface.HingeStart = Handles.PositionHandle(controlSurface.HingeStart, Quaternion.identity);

        // Right
        Handles.Label(controlSurface.HingeEnd, "Starboard");
        controlSurface.HingeEnd = Handles.PositionHandle(controlSurface.HingeEnd, Quaternion.identity);

        // Connect the points
        Handles.DrawLine(controlSurface.HingeStart, controlSurface.HingeEnd);
    }
}
