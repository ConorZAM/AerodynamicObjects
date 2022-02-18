using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentArrows : MonoBehaviour
{
    // This script will be responsible for adding arrows to the various areodynamic components
    // each arrow will need to be properly scaled, coloured and faded
    // For this there will be a static ArrowSettings class which has all the colour information
    // as well as scaling rules and such

    // Scale, sensitivity and offset should be up here but also need to be accessible to the Arrow class
    // and I can't be bothered just passing those values in every time, maybe the functions for arrows
    // should be out here too and the Arrow class can just be a data structure.

    [Tooltip("The overall size of the arrow")]
    public float scale = 1f;
    [Tooltip("Distance between the arrow head and the point of action")]
    public float offset = 0f;
    [Tooltip("Multiplier of the arrow's total length based on the value it represents. Length (m) = sensitivty * value")]
    public float sensitivity = 1f;
    [Tooltip("Does the arrow point towards the point of action or away from it")]
    public bool pointAtPoint;

    public void SetArrowPositionAndRotation(Arrow arrow, float length, Vector3 rootPosition, Vector3 direction)
    {
        // Direction MUST BE NORMALISED
        direction.Normalize();

        length = length * sensitivity;

        if (pointAtPoint)
        {
            rootPosition = rootPosition - length * direction;
        }

        // All the offsetting is handled in here so the higher up scripts just need to specify the point of action for the arrow
        arrow.body.position = rootPosition + offset*direction;
        arrow.body.up = direction;

        // This might become a fixed value rather than scaling the head proportionally like this
        arrow.body.localScale = new Vector3(scale, (1f - ArrowSettings.Singleton().arrowHeadFractionOfTotalLength) * length, scale);

        arrow.head.position = arrow.body.position + direction * ((1f - ArrowSettings.Singleton().arrowHeadFractionOfTotalLength) * length);
        arrow.head.up = direction;
        // This might become a fixed value rather than scaling the head proportionally like this
        arrow.head.localScale = new Vector3(2 * scale, ArrowSettings.Singleton().arrowHeadFractionOfTotalLength * length, 2 * scale);

    }

    public void SetArrowPositionAndRotationFromVector(Arrow arrow, Vector3 vector, Vector3 rootPosition)
    {
        // This is just lazy so that we can pass in the initial vector and get the length and direction here instead
        // of having to do it in every function in the higher upss

        Vector3 direction = vector.normalized;
        float length = vector.magnitude * sensitivity;

        if (pointAtPoint)
        {
            rootPosition = rootPosition - length * direction;
        }

        // All the offsetting is handled in here so the higher up scripts just need to specify the point of action for the arrow
        arrow.body.position = rootPosition;
        arrow.body.up = direction;

        // This might become a fixed value rather than scaling the head proportionally like this
        arrow.body.localScale = new Vector3(scale, (1f - ArrowSettings.Singleton().arrowHeadFractionOfTotalLength) * length, scale);

        arrow.head.position = arrow.body.position + direction * ((1f - ArrowSettings.Singleton().arrowHeadFractionOfTotalLength) * length);
        arrow.head.up = direction;
        // This might become a fixed value rather than scaling the head proportionally like this
        arrow.head.localScale = new Vector3(2 * scale, ArrowSettings.Singleton().arrowHeadFractionOfTotalLength * length, 2 * scale);
    }


    public class Arrow
    {
        // Need this class because a component can have more than one arrow attached to it.

        public Transform body;
        public Transform head;

        public Arrow(Color color)
        {
            GameObject bodyGO = Resources.Load("Arrow Body") as GameObject;
            GameObject headGO = Resources.Load("Arrow Head") as GameObject;
            body = Instantiate(bodyGO).transform;
            head = Instantiate(headGO).transform;

            // Set colour and shader to fade
            body.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = color;
            head.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = color;
            body.GetChild(0).GetComponent<MeshRenderer>().materials[0].ToFadeMode();
            head.GetChild(0).GetComponent<MeshRenderer>().materials[0].ToFadeMode();
        }

        public Arrow(Color color, string name, Transform parent)
        {
            GameObject bodyGO = Resources.Load("Arrow Body") as GameObject;
            GameObject headGO = Resources.Load("Arrow Head") as GameObject;
            body = Instantiate(bodyGO).transform;
            head = Instantiate(headGO).transform;
            body.name = "Arrow Body";
            head.name = "Arrow Head";

            // Set colour and shader to fade
            body.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = color;
            head.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = color;
            body.GetChild(0).GetComponent<MeshRenderer>().materials[0].ToFadeMode();
            head.GetChild(0).GetComponent<MeshRenderer>().materials[0].ToFadeMode();

            // Create a root gameobject which holds both parts of the arrow, makes it neater in scene hierarchy
            GameObject arrow = new GameObject(name);
            Transform arrowT = arrow.transform;
            arrowT.position = parent.position;
            arrowT.rotation = parent.rotation;

            arrowT.SetParent(parent);

            body.SetParent(arrowT);
            head.SetParent(arrowT);

        }
    }
}