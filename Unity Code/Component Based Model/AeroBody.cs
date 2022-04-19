using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AeroBody : MonoBehaviour
{
    /* The AeroBody class is responsible for:
     * - coordinate transformations
     * - resolution of the wind velocity
     * - tracking velocity of the body when a rigid body is not provided
     * 
     * The AeroBody maintains:
     * - dimensions of the object
     * - resolved dimensions of the equivalent aerodynamic body
     * 
     * Aerodynamics Components are added to an AeroBody to apply forces and moments to
     * the body based on its dimensions and the wind properties according to the component model
     * 
     * For testing purposes all variables and functions are made public
     * This allows for easy probing of the aerodynamic model
     * 
     */

    // Debugging Values
    public Vector3 EAB_windVel;
    public Vector3 earthVelocity;


    // Quick and dirty fix for grouping
    public AeroGroup myGroup = null;


    #region Component Computation and Apply Force Events

    /* The AeroBody has two events:
     * - Run Model Computations
     * - Apply Model Forces
     * 
     * Run Model Computations is called after all the ellipsoid properties are computed and the
     * wind has been resolved. Essentially, all the information needed by each aerodynamic component
     * is ready to be used.
     * 
     * Apply Model Forces should be called straight after the model computations are complete,
     * then each aerodynamic component applies its computed forces to the rigid body
     * 
     * This method is implemented to avoid management of all the aerodynamic components attached to the
     * object - we can also handle the addition and subtraction of components in runtime by using events
     */

    // The computation of aerodynamic forces and moments requires the information from the aero body
    public delegate void RunModelComputations(AeroBody aeroBody);
    public RunModelComputations runModelEvent;

    // After the forces and moments are computed, they need to be applied to the appropriate rigid body
    public delegate void ApplyModelForces(Rigidbody rb);
    public ApplyModelForces applyForcesEvent;

    #endregion Component Computation and Apply Force Events

    #region Reference Frames

    //  -------------------------------------------------------------------------------------------
    //      Reference Frames
    //  -------------------------------------------------------------------------------------------
    /*      Using a class for reference frames, this gives us a clear way to
     *      differentiate between wind velocity in the earth or body frame etc.
     *      The ReferenceFrame class holds the rotation between the previous frame
     *      and the current frame. This is done because all frames are in a
     *      hierarchy:
     *          1. Earth
     *          2. Object
     *          3. Body
     *          4. Equivalent Aerodynamic Body (EAB)
     *      
     *      There are helper functions in the Aerodynamics script which are used to
     *      transform directions from Body and EAB frames to the Earth frame
     */

    public class ReferenceFrame
    {
        // Useful directions - I don't think they get used at all actually...
        public Vector3 xDirection, yDirection, zDirection;  // (unit vector)


        // Wind resolved into this coordinate frame
        public Vector3 windVelocity;                    // (m/s)
        public Vector3 windVelocity_normalised;         // (unit vector
        public Vector3 angularWindVelocity;             // (rad/s)
        public Vector3 angularWindVelocity_normalised;  // (unit vector)

        // The rotation from the previous frame of reference to this frame of reference
        public Quaternion objectToFrameRotation = Quaternion.identity;

        // The rotation from the current frame of reference back to the previous frame
        public Quaternion inverseObjectToFrameRotation = Quaternion.identity;

        public void SetDirectionVectors(Vector3 x, Vector3 y, Vector3 z)
        {
            xDirection = x;
            yDirection = y;
            zDirection = z;
        }

        public void SetFrameRotation(Quaternion rotation)
        {
            // This was the logical approach to me... but it seems they need to be reverse
            //objectToFrameRotation = rotation;
            //inverseObjectToFrameRotation = Quaternion.Inverse(rotation);

            objectToFrameRotation = Quaternion.Inverse(rotation);
            inverseObjectToFrameRotation = rotation;
        }

        public void SetResolvedWind(Vector3 linearWind, Vector3 angularWind)
        {
            // The normalisations here are probably wasted computation... it's an
            // optimisation incase the normalised vector is needed more than once
            windVelocity = objectToFrameRotation * linearWind;
            windVelocity_normalised = windVelocity.normalized;

            angularWindVelocity = objectToFrameRotation * angularWind;
            angularWindVelocity_normalised = angularWindVelocity.normalized;
        }
    }

    // Converts a vector in EAB frame to earth frame
    public Vector3 TransformDirectionEABToEarth(Vector3 vector)
    {
        return TransformDirectionBodyToEarth(TransformDirectionEABToBody(vector));
    }

    // Converts a vector in EAB frame to body frame
    public Vector3 TransformDirectionEABToBody(Vector3 vector)
    {
        return equivAerobodyFrame.inverseObjectToFrameRotation * vector;
    }

    // Converts a vector in aeroBody frame to earth frame
    public Vector3 TransformDirectionBodyToEarth(Vector3 vector)
    {
        return unityObjectFrame.inverseObjectToFrameRotation * (aeroBodyFrame.inverseObjectToFrameRotation * vector);
    }

    // Update the objectFrame rotation based on the current rotation of the Transform component
    private void GetObjectAxisRotation()
    {
        unityObjectFrame.SetFrameRotation(transform.rotation);
    }

    // Earth is just Unity's global coordinates, the instance is only included here for clarity
    public ReferenceFrame earthFrame = new ReferenceFrame();

    // Object is the frame of reference defined by the GameObject's Transform component in Unity
    // it has no notion of span, chord or thickness and the rotation of the frame is effectively arbitrary
    // We need to keep track of this however as the dynamic motion of the object will generally
    // rotate the frame
    public ReferenceFrame unityObjectFrame = new ReferenceFrame();

    // AeroBodyFrame is the rotation of the object frame such that (x, y, z) align with (span, thickness, chord)
    public ReferenceFrame aeroBodyFrame = new ReferenceFrame();

    // Equivalent Aerodynamic Body is the rotation and projection of the AeroBody frame and dimensions
    // into the wind direction so that no sideslip is present
    public ReferenceFrame equivAerobodyFrame = new ReferenceFrame();

    #endregion Reference Frames

    #region Aerodynamic Bodies
    //  -------------------------------------------------------------------------------------------
    //      Aerodynamic Bodies
    //  -------------------------------------------------------------------------------------------
    /*      There are two ellipsoid bodies used for the aerodynamics simulation:
     *          1. The Aerodynamic Body (AeroBody)
     *          2. The Equivalent Aerodynamic Body (EAB)
     *      The AeroBody holds the aerodynamic properties (e.g. aspect ratio,
     *      chord, span etc) for the object being simulated. The EAB holds the
     *      aerodynamic properties of the body when resolved into the wind such
     *      that the body sees zero sideslip in the wind.
     */
    public class AerodynamicBody
    {
        // Diameters of the ellipsoid body, using aerodynamic notation
        public float span_a, thickness_b, chord_c;      // (m)

        // Radii of the ellipsoid body - for convenience so that multiples of two are neglected
        public float minorAxis, midAxis, majorAxis;     // (m)

        // Ratios
        public float aspectRatio;                       // (dimensionless)
        public float thicknessToChordRatio_bOverc;      // (dimensionless)
        public float camberRatio;                       // (dimensionless)


        public void SetDimensions(float span, float thickness, float chord)
        {
            span_a = span;
            majorAxis = span / 2f;

            chord_c = chord;
            midAxis = chord / 2f;

            thickness_b = thickness;
            minorAxis = thickness / 2f;
        }

        public void SetAerodynamicRatios(float _camber)
        {
            // The ellipse is awkward and may need to be scaled larger so that the area of the ellipse
            // and the usual cuboid is the same
            // We might not show the same ellipsoid body in gizmos as what the code uses
            aspectRatio = span_a / (Mathf.PI * chord_c);
            camberRatio = _camber / chord_c;
            thicknessToChordRatio_bOverc = thickness_b / chord_c;
        }
    }

    public AerodynamicBody aeroBody = new AerodynamicBody();
    public AerodynamicBody EAB = new AerodynamicBody();

    #endregion Aerodynamic Bodies

    #region General Properties
    //  -------------------------------------------------------------------------------------------
    //      Unity Related Components
    //  -------------------------------------------------------------------------------------------

    // Unity's physics body, used to apply physics to the object - We're going to assume that all
    // objects connected to a rigidbody are fixed and cannot move independently from the rigid body
    // This means that WE CAN'T MODEL FLAPPING WINGS OR FLEXIBLE WINGS using this method
    public Rigidbody rb;

    // Flag to determine if the model needs to recalculate dimensions at runtime
    public bool dynamicallyVariableShape;

    // Check to show this component is up and running, not necessary here but leaving it in just in case
    public bool initialised = false;

    //  -------------------------------------------------------------------------------------------
    //      General Aerodynamic Body Properties
    //  -------------------------------------------------------------------------------------------
    //      These are properties which are not changed by the projection
    //      from AeroBody to EAB and so are kept outside of the classes


    public float camber;                            // (m)

    // The projection from body to eab assumes constant areas for the ellipsoid body
    // therefore planform and profile areas are the same in each frame
    public float bodyPlanformArea;
    public float planformArea       // (m^2)
    {
        get
        {
            // This is used to scale up or down the planform area of bodies within a group so that
            // their total area equals the required planform area of the group
            if (myGroup == null) { return bodyPlanformArea; } else { return myGroup.areaScale * bodyPlanformArea; }
        }
    }
    public float profileArea;         // (m^2)
    public Vector3 areaVector;                      // (m^2)

    // Not sure if the same applies to the volumes, I would assume so though
    public Vector3 volumeVector;                    // (m^3)
    public float ellipsoidSurfaceArea;              // (m^2)
    public float lambda_x, lambda_y, lambda_z;      // (dimensionless)

    //  -------------------------------------------------------------------------------------------
    //      Aerodynamic Angles and Rotation Vectors
    //  -------------------------------------------------------------------------------------------

    // Sideslip (beta) and angle of attack (alpha) vectors
    public Vector3 angleOfAttackRotationVector;                // (unit vector)
    public float alpha, alpha_0, beta;                  // (rad)
    public float alpha_deg, beta_deg;                   // (degrees)
    public float sinAlpha, cosAlpha, sinBeta, cosBeta;  // (dimensionless)


    //  -------------------------------------------------------------------------------------------
    //      Fluid Properties and Characteristics
    //  -------------------------------------------------------------------------------------------

    // The fluid flow velocity which is not due to the object's velocity
    public Vector3 externalFlowVelocity_inEarthFrame;   // (m/s)

    // Properties
    public float dynamicPressure;      // (Pa)
    public float qS;                   // (N)
    public float rho = 1.2f;           // (kg/m^3)
    public float mu = 1.8e-5f;         // (Nm/s)

    // Resultant forces and moments to be applied to the rigid body component
    public Vector3 resultantAerodynamicForce_earthFrame, resultantAerodynamicForce_bodyFrame;                                   // (N)
    public Vector3 resultantAerodynamicMoment_earthFrame, resultantAerodynamicMoment_bodyFrame;                                 // (Nm)

    #endregion General Properties

    #region Aerodynamic Functions

    //  -------------------------------------------------------------------------------------------
    //      Aerodynamic Functions
    //  -------------------------------------------------------------------------------------------
    //      The numbers in the function names indicate the order in which they
    //      should be called when computing the aerodynamics from start to finish

    public void GetReferenceFrames_1()
    {
        /* The frames of reference and their notations in this model are:
        *  - Earth                                                     (earth)
        *  The earth frame is equivalent to Unity's global coordinates
        *  
        *  - Object                                                    (unityObject)
        *  The object frame is equivalent to Unity's local coordinates
        *  i.e. the arbitrary local (x,y,z) for the aerodynamic object
        *  This frame moves and rotates with the rigid body dynamics
        *  
        *  - Body                                                      (aeroBody)
        *  The body frame is a rotation of the object frame
        *  such that (x, y, z) are aligned with (span, thickness, chord)
        *  Thickness chord and span are selected in order of ascending
        *  dimensions of the ellipsoid, i.e. span >= chord >= thickness
        *  
        *  - Equivalent Aerodynamic Body                               (EAB)
        *  The equivalent aerodynamic body is the resolved body axes
        *  such that the equivalent body experiences no sideslip. This is
        *  both a rotation of the frame and a projection of dimensions
        *  of the body to form a new ellipsoid
        *  
        *  First we need to get the span, chord and thickness of the body
        *  based on the scale of the object - this could change in the future to be
        *  inputs by the user! Or it could default to using the bounding box of the mesh
        */

        // For now, we're just looking at the scale of the object to obtain ellipsoid dimensions
        float x = transform.localScale.x;
        float y = transform.localScale.y;
        float z = transform.localScale.z;

        // This is redundant data really but it makes everything look nice
        earthFrame.SetDirectionVectors(Vector3.right, Vector3.up, Vector3.forward);
        unityObjectFrame.SetDirectionVectors(Vector3.right, Vector3.up, Vector3.forward);


        // The object rotation will need updating at every time step as the object moves according to
        // its rigid body dynamic motion, whereas the body frame is fixed relative to the object frame
        GetObjectAxisRotation();

        // The normal to the lifting plane is aligned with the minor axis (thickness) of the ellipsoid,
        // The span is aligned to X and chord to Z
        // Coordinates are aligned so that [x, y, z] == [span, thickness, chord]

        // The order of these checks ensures that if x == y == z then they become (span, thickness, chord)
        // as defined in the theory
        if (x >= y)
        {
            if (y > z)
            {
                // Then x > y > z and we need to swap thickness and chord
                aeroBody.SetDimensions(x, z, y);
                aeroBodyFrame.SetDirectionVectors(Vector3.right, Vector3.forward, Vector3.up);
            }
            else
            {
                if (x >= z)
                {
                    // Then x > z > y so we don't need to do anything
                    aeroBody.SetDimensions(x, y, z);
                    aeroBodyFrame.SetDirectionVectors(Vector3.right, Vector3.up, Vector3.forward);
                }
                else
                {
                    // Then z > x > y so we need to swap chord and span
                    aeroBody.SetDimensions(z, y, x);
                    aeroBodyFrame.SetDirectionVectors(Vector3.forward, Vector3.up, Vector3.right);
                }
            }
        }
        else
        {
            // y > x
            if (y >= z)
            {
                if (x >= z)
                {
                    // Then y > x > z
                    aeroBody.SetDimensions(y, z, x);
                    aeroBodyFrame.SetDirectionVectors(Vector3.up, Vector3.forward, Vector3.right);
                }
                else
                {
                    // Then y > z > x
                    aeroBody.SetDimensions(y, x, z);
                    aeroBodyFrame.SetDirectionVectors(Vector3.up, Vector3.right, Vector3.forward);
                }
            }
            else
            {
                // Then z > y > x
                aeroBody.SetDimensions(z, x, y);
                aeroBodyFrame.SetDirectionVectors(Vector3.forward, Vector3.right, Vector3.up);
            }
        }
        // Rotate the reference axes to line up with (span, thickness, chord) == (a, b, c)
        aeroBodyFrame.SetFrameRotation(Quaternion.LookRotation(aeroBodyFrame.zDirection, aeroBodyFrame.yDirection));

        // Set the aerodynamic related properties
        aeroBody.SetAerodynamicRatios(camber);
    }

    public void GetEllipsoidProperties_2()
    {
        // Area and volume vectors
        // Using the appropriate ellipsoid radii here so there is no potential for confusion by dividing by 2
        areaVector = Mathf.PI * new Vector3(aeroBody.minorAxis * aeroBody.midAxis, aeroBody.majorAxis * aeroBody.midAxis, aeroBody.majorAxis * aeroBody.minorAxis);

        float fourPiOver3 = 4f * Mathf.PI / 3f;
        volumeVector.x = fourPiOver3 * aeroBody.majorAxis * aeroBody.midAxis * aeroBody.midAxis;
        volumeVector.y = fourPiOver3 * aeroBody.minorAxis * aeroBody.majorAxis * aeroBody.majorAxis;
        volumeVector.z = fourPiOver3 * aeroBody.midAxis * aeroBody.majorAxis * aeroBody.majorAxis;

        // Planform area - area of the aerodynamic body in the lifting plane
        bodyPlanformArea = areaVector.y;

        // Approximate surface area of ellipsoid
        ellipsoidSurfaceArea = 4f * Mathf.PI * Mathf.Pow((1f / 3f) * (Mathf.Pow(aeroBody.majorAxis * aeroBody.minorAxis, 1.6f)
                                                                      + Mathf.Pow(aeroBody.majorAxis * aeroBody.midAxis, 1.6f)
                                                                      + Mathf.Pow(aeroBody.minorAxis * aeroBody.midAxis, 1.6f)), (1f / 1.6f));

        // Axis Ratios
        lambda_x = aeroBody.thickness_b / aeroBody.chord_c;
        lambda_y = aeroBody.chord_c / aeroBody.span_a;
        lambda_z = aeroBody.thickness_b / aeroBody.span_a;
    }

    // Not interested in setting the inertia here, we'll just use Unity's model of the inertia based on the collider mesh
    //public void SetMassProperties_2()
    //{
    //    // Set the inertia of the body as
    //    // Solid Ellipsoid
    //    rb.inertiaTensor = 0.2f * rb.mass * new Vector3(aeroBody.thickness_b * aeroBody.thickness_b / 4f + aeroBody.chord_c * aeroBody.chord_c / 4f, aeroBody.span_a * aeroBody.span_a / 4f + aeroBody.chord_c * aeroBody.chord_c / 4f, aeroBody.span_a * aeroBody.span_a / 4f + aeroBody.thickness_b * aeroBody.thickness_b / 4f);
    //}

    public void ResolveWind_3()
    {
        // Putting this here because it's definitely going to be called and it needs doing
        // before we can resolve the wind into object, body and EAB axes
        GetObjectAxisRotation();

        // In aerodynamics, wind velocity describes the velocity of the body relative to the wind

        // Using the transform position here so we can have aero body components separate from rigid bodies
        // and everything should still work
        earthVelocity = rb.GetPointVelocity(transform.position);

        // Start with earth frame
        earthFrame.SetResolvedWind(earthVelocity - externalFlowVelocity_inEarthFrame, rb.angularVelocity);
        // Rotate to object frame
        unityObjectFrame.SetResolvedWind(earthFrame.windVelocity, earthFrame.angularWindVelocity);
        // Rotate to body frame
        aeroBodyFrame.SetResolvedWind(unityObjectFrame.windVelocity, unityObjectFrame.angularWindVelocity);
    }

    public void SetWind_3(Vector3 wind, Vector3 angularWind)
    {
        // This is an alternative to getting the wind according to the rigid body velocity
        // here the external wind is simply passed in. This is used for experiments and tests

        // Putting this here because it's definitely going to be called and it needs doing
        // before we can resolve the wind into object, body and EAB axes
        GetObjectAxisRotation();

        // In aerodynamics, wind velocity describes the velocity of the body relative to the wind

        // Start with earth frame
        earthFrame.SetResolvedWind(wind, angularWind);
        // Rotate to object frame
        unityObjectFrame.SetResolvedWind(earthFrame.windVelocity, earthFrame.angularWindVelocity);
        // Rotate to body frame
        aeroBodyFrame.SetResolvedWind(unityObjectFrame.windVelocity, unityObjectFrame.angularWindVelocity);
    }


    public void GetFlowCharacteristics_4()
    {
        // Wind square magnitude should be the same for all frames - I think! 
        dynamicPressure = 0.5f * rho * aeroBodyFrame.windVelocity.sqrMagnitude;
        qS = dynamicPressure * planformArea;
    }

    public void GetAeroAngles_5()
    {
        // Sideslip
        beta = Mathf.Atan2(aeroBodyFrame.windVelocity.x, aeroBodyFrame.windVelocity.z);
        beta_deg = Mathf.Rad2Deg * beta;
        sinBeta = Mathf.Sin(beta);
        cosBeta = Mathf.Cos(beta);

        // Equivalent Aerodynamic Body is the Body frame rotated by the sideslip angle
        // I'll bet there's a cheaper way to do this but alas...
        equivAerobodyFrame.SetFrameRotation(Quaternion.Euler(0, beta_deg, 0));

        // Resolve the body wind so we have zero sideslip
        equivAerobodyFrame.SetResolvedWind(aeroBodyFrame.windVelocity, aeroBodyFrame.angularWindVelocity);

        EAB_windVel = equivAerobodyFrame.windVelocity;

        // Angle of attack
        angleOfAttackRotationVector = Vector3.Cross(aeroBodyFrame.windVelocity_normalised, Vector3.down);

        // Include the minus sign here because alpha goes from the wind vector to the lifting plane
        alpha = -Mathf.Atan2(equivAerobodyFrame.windVelocity.y, equivAerobodyFrame.windVelocity.z);
        alpha_deg = Mathf.Rad2Deg * alpha;
        sinAlpha = Mathf.Sin(alpha);
        cosAlpha = Mathf.Cos(alpha);

        // I noticed this happen once but haven't seen it again since, so I'm leaving this just as a precaution
        if (Mathf.Abs(alpha_deg) > 90f)
        {
            Debug.LogError("Angle of attack exceeded +- 90 degrees for " + gameObject.name + ". Please send the object's position, rotation, velocity and angular velocity to Conor.");
            Debug.Break();
        }
    }

    public void GetEquivalentAerodynamicBody_6()
    {
        // The equivalent aerodynamic body has the same aspect ratio and and span
        // as the actual body's shape, but with zero sideslip

        // Resolving in sideslip direction by rotating about the normal to the lift plane
        EAB.midAxis = aeroBody.majorAxis * aeroBody.midAxis / Mathf.Sqrt((aeroBody.midAxis * aeroBody.midAxis * sinBeta * sinBeta) + (aeroBody.majorAxis * aeroBody.majorAxis * cosBeta * cosBeta));
        // Find major axis based on constant area of ellipse
        EAB.majorAxis = aeroBody.majorAxis * aeroBody.midAxis / EAB.midAxis;
        // No change to thickness
        EAB.minorAxis = aeroBody.minorAxis;

        // Store the diameters as well to save on factor of 2 in equations
        EAB.span_a = 2f * EAB.majorAxis;
        EAB.thickness_b = 2f * EAB.minorAxis;
        EAB.chord_c = 2f * EAB.midAxis;

        // Work out aero parameters of equivalent body
        EAB.SetAerodynamicRatios(camber);

        // Profile area is the projection in the wind direction
        profileArea = Vector3.Scale(areaVector, aeroBodyFrame.windVelocity_normalised).magnitude;
    }

    public void ResolveWindAndDimensions_1_to_6()
    {
        GetReferenceFrames_1();
        GetEllipsoidProperties_2();
        ResolveWind_3();
        GetFlowCharacteristics_4();
        GetAeroAngles_5();
        GetEquivalentAerodynamicBody_6();
    }

    public void GetComponentForces_7()
    {
        // Call the event, any components attached to this game object will be subscribed
        runModelEvent?.Invoke(this);
    }


    public void ApplyAerodynamicForces_8()
    {
        // Call the event, any components attached to this game object will be subscribed
        applyForcesEvent?.Invoke(rb);
    }

    public void GetEllipsoid_1_to_2()
    {
        GetReferenceFrames_1();
        GetEllipsoidProperties_2();
    }

    public void AerodynamicBody_3_to_6()
    {
        ResolveWind_3();
        GetFlowCharacteristics_4();
        GetAeroAngles_5();
        GetEquivalentAerodynamicBody_6();
    }

    public void SetAlpha_rad_3_to_6(float alpha_rad)
    {
        // Assumes the body has zero velocity
        externalFlowVelocity_inEarthFrame = new Vector3(0, -Mathf.Sin(alpha_rad), Mathf.Cos(alpha_rad));

        ResolveWind_3();
        GetFlowCharacteristics_4();
        GetAeroAngles_5();
        GetEquivalentAerodynamicBody_6();
    }

    public void SetAlpha_deg_3_to_6(float alpha_deg)
    {
        // Assumes the body has zero velocity
        float alpha_rad = Mathf.Deg2Rad * alpha_deg;
        externalFlowVelocity_inEarthFrame = new Vector3(0, -Mathf.Sin(alpha_rad), Mathf.Cos(alpha_rad));

        ResolveWind_3();
        GetFlowCharacteristics_4();
        GetAeroAngles_5();
        GetEquivalentAerodynamicBody_6();
    }

    #endregion Aerodynamic Functions

    public void Initialise()
    {
        // At the moment, we need a rigid body for our velocity etc. Eventually I might be persuaded
        // into adding a kinematic component which keeps track of velocity for us.
        if(!rb)
        {
            // Check if there's a rigid body attached to the same game object
            rb = GetComponent<Rigidbody>();

            if (!rb)
            {
                // Check all the parents of this object for a rigid body component
                rb = GetComponentInParent<Rigidbody>();

                if (!rb)
                {
                    // Add a rigid body if we can't find one anywhere
                    Debug.LogWarning("No RigidBody Component found for " + gameObject.name + ", adding one.");
                    rb = gameObject.AddComponent<Rigidbody>();

                    // Don't use Unity's filthy drag model - bah!
                    rb.angularDrag = 0;
                    rb.drag = 0;
                }
            }
        }

        GetEllipsoid_1_to_2();
        initialised = true;
    }

    void Awake()
    {
        Initialise();
    }

    void FixedUpdate()
    {
        if (dynamicallyVariableShape)
        {
            GetEllipsoid_1_to_2();
        }
        AerodynamicBody_3_to_6();
        GetComponentForces_7();
        ApplyAerodynamicForces_8();

    }

    // This could be offloaded to a separate class with a singleton so we don't have to store it in
    // memory for every aero body component
    Mesh gizmoMesh;
    private void OnDrawGizmos()
    {
        // This is a very hacky way to draw a squashed sphere mesh as an ellipsoid
        // I keep getting errors when the project opens because it tries to destroy things and yeah...
        // Would be nice to be able to reference just the mesh as a resource instead of the game object
        if (!gizmoMesh)
        {
            gizmoMesh = Resources.Load<Mesh>("Sphere");
            GetReferenceFrames_1();
            //GameObject go = Resources.Load("Sphere") as GameObject;
            //gizmoMesh = go.GetComponent<MeshFilter>().sharedMesh;
            //DestroyImmediate(go);
        }

        // This is a lazy way to update the aerobody in the editor when the transform component is changed
        // We need to do this because the scale of the transform is used to determine the ellipsoid properties
        // I think we should allow people to adjust this but there are so many ways to derive the body that it
        // will take a while to enable them all or decide which ones are best
        if (!Application.isPlaying && transform.hasChanged)
        {
            GetReferenceFrames_1();
            transform.hasChanged = false;
        }
        
        // Draw the ellipsoid with the same rotation as the aero body, need to do this because
        // the transform won't necessarily be the same rotation as the span, thickness, chord axes
        Gizmos.color = Color.green;
        Gizmos.DrawWireMesh(gizmoMesh, transform.position, transform.rotation * aeroBodyFrame.inverseObjectToFrameRotation, new Vector3(aeroBody.span_a, aeroBody.thickness_b, aeroBody.chord_c));
    }

    private void Reset()
    {
        GetReferenceFrames_1();
    }
}
