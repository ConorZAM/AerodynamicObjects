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


        ThinAerofoilComponent thinAerofoil = go.GetComponent<ThinAerofoilComponent>();
        TranslationalDragComponent translationalDrag = go.GetComponent<TranslationalDragComponent>();

        if (thinAerofoil)
        {
            // If we have both lift and drag models attached
            if (translationalDrag)
            {
                // Add the total arrow components which require both components
                if (!go.GetComponent<LiftOnlyArrows>())
                {
                    go.AddComponent<LiftOnlyArrows>();
                }
                if (!go.GetComponent<TotalDragArrows>())
                {
                    go.AddComponent<TotalDragArrows>();
                }
            }
            else
            {
                // Otherwise, we just have the lift component, so add the arrows for that component
                if (!go.GetComponent<ThinAerofoilArrows>())
                {
                    go.AddComponent<ThinAerofoilArrows>();
                }
            }
        }
        else if (translationalDrag)
        {
            // Also need to check incase we have just the translational drag component attached
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
