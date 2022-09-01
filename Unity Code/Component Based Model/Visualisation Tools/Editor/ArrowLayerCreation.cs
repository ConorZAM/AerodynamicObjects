using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class ArrowLayerCreation
{
    static ArrowLayerCreation()
    {
        // ====================
        // The code below was taken from: https://forum.unity.com/threads/create-tags-and-layers-in-the-editor-using-script-both-edit-and-runtime-modes.732119/

        // Create the arrow layer
        string layerName = "Arrows";
        int maxLayers = 31;

        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Layers Property
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        if (!PropertyExists(layersProp, 0, maxLayers, layerName))
        {
            SerializedProperty sp;
            // Start at layer 9th index -> 8 (zero based) => first 8 reserved for unity / greyed out
            for (int i = 8, j = maxLayers; i < j; i++)
            {
                sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.stringValue == "")
                {
                    // Assign string value to layer
                    sp.stringValue = layerName;
                    Debug.Log("Layer: " + layerName + " has been added for aerodynamic force visualisation tools");
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    break;
                }
                if (i == j)
                    Debug.LogWarning("All allowed layers have been assigned. Please change one to: \'" + layerName + "\' so that aerodynamic force visualisation tools can operate correctly");
            }
        }
        // =====================
    }

    // =================
    // This function also came from: https://forum.unity.com/threads/create-tags-and-layers-in-the-editor-using-script-both-edit-and-runtime-modes.732119/
    private static bool PropertyExists(SerializedProperty property, int start, int end, string value)
    {
        for (int i = start; i < end; i++)
        {
            SerializedProperty t = property.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(value))
            {
                return true;
            }
        }
        return false;
    }
    // ===================
}
