using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AeroGroup : MonoBehaviour
{
    public AeroBody[] aeroBodies = new AeroBody[0];

    public float planformArea;
    public float scaledArea;
    public float aspectRatio;
    public float bodyTotalArea;

    public float areaScale;

    private void OnValidate()
    {
        if (aeroBodies.Length > 0)
        {
            AssignAeroBodiesToGroup();
            GetAreaScales();
        }
    }

    private void GetAreaScales()
    {
        bodyTotalArea = 0f;
        // Get total areas for individual bodies
        for (int i = 0; i < aeroBodies.Length; i++)
        {
            bodyTotalArea += aeroBodies[i].bodyPlanformArea;
        }

        // areaScale tells us how much the aero body planform areas need to be scaled by to
        // make their total area equal to the wing's planform area
        areaScale = planformArea / bodyTotalArea;


        scaledArea = 0f;
        for (int i = 0; i < aeroBodies.Length; i++)
        {
            scaledArea += areaScale * aeroBodies[i].bodyPlanformArea;
        }
    }

    void AssignAeroBodiesToGroup()
    {
        for (int i = 0; i < aeroBodies.Length; i++)
        {
            aeroBodies[i].myGroup = this;
            aeroBodies[i].GetEllipsoid_1_to_2();
        }
    }

    public void GetChildBodies()
    {
        aeroBodies = GetComponentsInChildren<AeroBody>();

        int length = aeroBodies.Length;
        switch (length)
        {
            case 0:
                Debug.Log("No aero bodies were found in children of " + gameObject.name);
                break;
            case 1:
                Debug.Log("1 aero body found and added to group.");
                AssignAeroBodiesToGroup();
                GetAreaScales();
                break;
            default:
                Debug.Log(aeroBodies.Length + " aero bodies found and added to group.");
                AssignAeroBodiesToGroup();
                GetAreaScales();
                break;
        }

    }
}
