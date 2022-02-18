using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindRotation : MonoBehaviour
{
    [Tooltip("Degrees per second")]
    public float rotationSpeed = 5f;
    public float initialWindSpeed = 2f;
    AeroBody aeroBody;
    // Start is called before the first frame update
    void Start()
    {
        aeroBody = GetComponent<AeroBody>();
        aeroBody.externalFlowVelocity_inEarthFrame = new Vector3(0, 0, initialWindSpeed);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        aeroBody.externalFlowVelocity_inEarthFrame = Quaternion.Euler(0, -rotationSpeed * Time.fixedDeltaTime, 0) * aeroBody.externalFlowVelocity_inEarthFrame;
    }
}
