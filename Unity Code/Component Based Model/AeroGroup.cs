using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AeroGroup : MonoBehaviour
{
    [HideInInspector]
    public AeroBody[] aeroBodies;

    public float planformArea;
    public float scaledArea;
    public float aspectRatio;
    public float bodyTotalArea;

    float areaScale;

    private void OnValidate()
    {
        aeroBodies = GetComponentsInChildren<AeroBody>();
        AssignAeroBodiesToGroup();
        GetAreaScales();
    }

    private void GetAreaScales()
    {
        bodyTotalArea = 0f;
        // Get total areas for individual bodies
        for (int i = 0; i < aeroBodies.Length; i++)
        {
            bodyTotalArea += aeroBodies[i].planformArea;
        }

        areaScale = planformArea / bodyTotalArea;


        scaledArea = 0f;
        for (int i = 0; i < aeroBodies.Length; i++)
        {
            scaledArea += areaScale * aeroBodies[i].planformArea;
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
}
