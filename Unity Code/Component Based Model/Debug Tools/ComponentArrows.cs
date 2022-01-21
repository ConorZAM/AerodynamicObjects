using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentArrows : MonoBehaviour
{
    // This script will be responsible for adding arrows to the various areodynamic components
    // each arrow will need to be properly scaled, coloured and faded
    // For this there will be a static ArrowSettings class which has all the colour information
    // as well as scaling rules and such

    public float scaling = 1f;
    public GameObject arrowHead;
    public GameObject arrowBody;


    public class Arrow
    {
        Transform body;
        Transform head;

        public Arrow(GameObject _head, GameObject _body, Color color)
        {
            body = Instantiate(_body).transform;
            head = Instantiate(_head).transform;

            // Set colour and shader to fade
            body.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = color;
            head.GetChild(0).GetComponent<MeshRenderer>().materials[0].color = color;
            body.GetChild(0).GetComponent<MeshRenderer>().materials[0].ToFadeMode();
            head.GetChild(0).GetComponent<MeshRenderer>().materials[0].ToFadeMode();

        }

        public void SetPositionAndRotation(float radius, float length, Vector3 rootPosition, Vector3 direction)
        {
            // Direction MUST BE NORMALISED

            body.position = rootPosition;
            body.up = direction;
            // Half because cyllinder
            body.localScale = new Vector3(radius, 0.5f * length, radius);

            head.position = body.position + direction * length;
            head.up = direction;
            head.localScale = new Vector3(radius, radius, radius);
        }
    }
    
    
    

    
}
