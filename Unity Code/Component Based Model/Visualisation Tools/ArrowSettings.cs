using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

class ArrowSettings : ScriptableObject
{
    // Using a horrible serialisation method here so that we can have our settings
    // appear in the project settings window. It is ugly as hell.
    public const string k_ArrowSettingsPath = "Assets/Editor/ArrowSettings.asset";

    static ArrowSettings singleton;
    public static ArrowSettings Singleton()
    {
        if (singleton == null)
            GetSerializedSettings();

        return singleton;
    }

    [SerializeField]
    public float arrowAspectRatio = 0.05f;
    [SerializeField]
    public float arrowHeadFractionOfTotalLength = 0.2f;
    [SerializeField]
    public float arrowAlpha = 0.5f;
    [SerializeField]
    public float wedgeAlpha = 0.5f;
    [SerializeField]
    public Color liftColour = new Color(0, 1, 0, 1);
    [SerializeField]
    public Color dragColour = new Color(1, 0, 0, 1);
    [SerializeField]
    public Color windColour = new Color(0, 1, 1, 1);
    [SerializeField]
    public Color alphaColour = new Color(1, 1, 0, 1);
    [SerializeField]
    public Color betaColour = new Color(1, 0, 1, 1);

    internal static ArrowSettings GetOrCreateSettings()
    {
        ArrowSettings settings = AssetDatabase.LoadAssetAtPath<ArrowSettings>(k_ArrowSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ArrowSettings>();

            // Default values
            settings.arrowAspectRatio = 0.05f;
            settings.arrowHeadFractionOfTotalLength = 0.2f;
            settings.arrowAlpha = 0.5f;
            settings.wedgeAlpha = 0.4f;
            settings.liftColour = new Color(0, 1, 0, settings.arrowAlpha);
            settings.dragColour = new Color(1, 0, 0, settings.arrowAlpha);
            settings.windColour = new Color(0, 1, 1, settings.arrowAlpha);
            settings.alphaColour = new Color(1, 1, 0, settings.wedgeAlpha);
            settings.betaColour = new Color(1, 0, 1, settings.wedgeAlpha);

            AssetDatabase.CreateAsset(settings, k_ArrowSettingsPath);
            AssetDatabase.SaveAssets();
        }

        ArrowSettings.singleton = settings;

        return settings;
    }



    internal static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
}

// Register a SettingsProvider using IMGUI for the drawing framework:
static class MyCustomSettingsIMGUIRegister
{
    [SettingsProvider]
    public static SettingsProvider CreateMyCustomSettingsProvider()
    {
        // First parameter is the path in the Settings window.
        // Second parameter is the scope of this setting: it only appears in the Project Settings window.
        var provider = new SettingsProvider("Project/AerodynamicArrows", SettingsScope.Project)
        {
            // By default the last token of the path is used as display name if no label is provided.
            label = "Aerodynamic Arrows",
            // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
            guiHandler = (searchContext) =>
            {
                var settings = ArrowSettings.GetSerializedSettings();

                EditorGUILayout.PropertyField(settings.FindProperty("arrowAspectRatio"), new GUIContent("Aspect Ratio"));
                EditorGUILayout.PropertyField(settings.FindProperty("arrowHeadFractionOfTotalLength"), new GUIContent("Head length as a fraction of total length"));
                //EditorGUILayout.PropertyField(settings.FindProperty("arrowAlpha"), new GUIContent("Alpha"));
                EditorGUILayout.PropertyField(settings.FindProperty("liftColour"), new GUIContent("Lift Colour"));
                EditorGUILayout.PropertyField(settings.FindProperty("dragColour"), new GUIContent("Drag Colour"));
                EditorGUILayout.PropertyField(settings.FindProperty("windColour"), new GUIContent("Wind Colour"));
                EditorGUILayout.PropertyField(settings.FindProperty("alphaColour"), new GUIContent("Angle of Attack Colour"));
                EditorGUILayout.PropertyField(settings.FindProperty("betaColour"), new GUIContent("Sideslip Angle Colour"));

                settings.ApplyModifiedPropertiesWithoutUndo();
            },

            // Populate the search keywords to enable smart search filtering and label highlighting:
            keywords = new HashSet<string>(new[] { "Arrow" })
        };

        return provider;
    }
}
