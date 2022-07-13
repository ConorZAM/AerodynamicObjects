using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AeroComponentAdder : MonoBehaviour
{
    // Add a menu to the Aerobody script
    [MenuItem("CONTEXT/AeroBody/Add All Aerodynamics")]
    static void AddAllAerodynamics(MenuCommand command)
    {
        AeroBody aeroBody = (AeroBody)command.context;
        GameObject go = aeroBody.gameObject;

        // Add all the components if they aren't there already

        if (!go.GetComponent<ThinAerofoilComponent>())
        {
            go.AddComponent<ThinAerofoilComponent>();
        }

        if (!go.GetComponent<TranslationalDragComponent>())
        {
            go.AddComponent<TranslationalDragComponent>();
        }

        if (!go.GetComponent<RotationalDragComponent>())
        {
            go.AddComponent<RotationalDragComponent>();
        }

        if (!go.GetComponent<MagnusEffectComponent>())
        {
            go.AddComponent<MagnusEffectComponent>();
        }
    }

    // Add a menu to the Aerobody script
    [MenuItem("CONTEXT/AeroBody/Add Arrows")]
    static void AddArrows(MenuCommand command)
    {
        AeroBody aeroBody = (AeroBody)command.context;
        GameObject go = aeroBody.gameObject;

        // Add all the components if they aren't there already

        if (go.GetComponent<ThinAerofoilComponent>())
        {
            if (!go.GetComponent<ThinAerofoilArrows>())
            {
                go.AddComponent<ThinAerofoilArrows>();
            }
        }

        if (go.GetComponent<TranslationalDragComponent>())
        {
            if (!go.GetComponent<TranslationalDragArrows>())
            {
                go.AddComponent<TranslationalDragArrows>();
            }
        }

        if (!go.GetComponent<WindArrow>())
        {
            go.AddComponent<WindArrow>();
        }

    }
}
