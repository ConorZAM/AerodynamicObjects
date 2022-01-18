using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Aerodynamics : MonoBehaviour
{
    // Debugging Values
    public Vector3 EAB_windVel;


    // For testing purposes all variables and functions are made public
    // This allows for easy probing of the aerodynamic model


    //  -------------------------------------------------------------------------------------------
    //      Unity Related Components
    //  -------------------------------------------------------------------------------------------


    // Unity's physics body, used to apply physics to the object
    public Rigidbody rb;

    // Flag to determine if the model needs to recalculate dimensions at runtime
    public bool dynamicallyVariableShape;

    // Magnus effect is pretty dominant at the moment...
    public bool includeMagnusEffect = false;

    public bool initialised = false;


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
        public Quaternion objectToFrameRotation;

        // The rotation from the current frame of reference back to the previous frame
        public Quaternion inverseObjectToFrameRotation;

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
    public Vector3 TransformEABToEarth(Vector3 vector)
    {
        return TransformBodyToEarth(TransformEABToBody(vector));
    }

    // Converts a vector in EAB frame to body frame
    public Vector3 TransformEABToBody(Vector3 vector)
    {
        return equivAerobodyFrame.inverseObjectToFrameRotation * vector;
    }

    // Converts a vector in aeroBody frame to earth frame
    public Vector3 TransformBodyToEarth(Vector3 vector)
    {
        return unityObjectFrame.inverseObjectToFrameRotation * (aeroBodyFrame.inverseObjectToFrameRotation * vector);
    }

    // Update the objectFrame rotation based on the current rotation of the Transform component
    private void GetObjectAxisRotation()
    {
        unityObjectFrame.SetFrameRotation(transform.rotation);
    }

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

    // Earth is just Unity's global coordinates, the instance is only included here for clarity
    public ReferenceFrame earthFrame = new ReferenceFrame();

    // Object is the frame of reference defined by the GameObject's Transform component in Unity
    // it has no notion of span, chord or thickness and the rotation of the frame is effectively arbitrary
    // We need to keep track of this however as the dynamic motion of the object will generally
    // rotate the frame
    public ReferenceFrame unityObjectFrame = new ReferenceFrame();

    // AeroBodyFrame is the rotation of the object frame such that (x, y, z) align with (span, thickness, chord)
    public ReferenceFrame aeroBodyFrame = new ReferenceFrame();

    // The aeroBody holds the object's aerodynamic dimensions (span, chord, aspect ratio etc)
    public AerodynamicBody aeroBody = new AerodynamicBody();

    // Equivalent Aerodynamic Body is the rotation and projection of the AeroBody frame and dimensions
    // into the wind direction so that no sideslip is present
    public ReferenceFrame equivAerobodyFrame = new ReferenceFrame();
    public AerodynamicBody EAB = new AerodynamicBody();



    //  -------------------------------------------------------------------------------------------
    //      General Aerodynamic Body Properties
    //  -------------------------------------------------------------------------------------------
    //      These are properties which are not changed by the projection
    //      from AeroBody to EAB and so are kept outside of the classes

    
    public float camber;                            // (m)

    // The projection from body to eab assumes constant areas for the ellipsoid body
    // therefore planform and profile areas are the same in each frame
    public float planformArea, profileArea;         // (m^2)
    public Vector3 areaVector;                      // (m^2)

    // Not sure if the same applies to the volumes, I would assume so though
    public Vector3 volumeVector;                    // (m^3)
    public float ellipsoidSurfaceArea;              // (m^2)
    public float lambda_x, lambda_y, lambda_z;      // (dimensionless)

    //  -------------------------------------------------------------------------------------------
    //      Aerodynamic Angles and Rotation Vectors
    //  -------------------------------------------------------------------------------------------

    // Sideslip (beta) and angle of attack (alpha) vectors
    Vector3 angleOfAttackRotationVector;                // (unit vector)
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
    public float rho = 1.2f;           // (kg/m^3)
    public float mu = 1.8e-5f;         // (Nm/s)

    // Rotational and linear flow characteristics
    public float reynoldsNum_linear;                                                           // (dimensionless)
    public float Cf_linear;                                                                    // (dimensionless)
    public float reynoldsNum_x_rotational, reynoldsNum_y_rotational, reynoldsNum_z_rotational; // (dimensionless)
    public float Cf_x_rotational, Cf_y_rotational, Cf_z_rotational;                            // (dimensionless)


    //  -------------------------------------------------------------------------------------------
    //      Aerodynamic Coefficients
    //  -------------------------------------------------------------------------------------------

    // Corrections for EAB
    public float aspectRatioCorrection_kAR, thicknessCorrection_kt;    // (dimensionless)
    const float thicknessCorrection_labdat = 6f;                // (dimensionless)

    // Lift and rotational lift
    public float CL;                            // (dimensionless)
    public Vector3 CLr;                           // (dimensionless)
    public float CLmax = 1.2f;                  // (dimensionless)
    public float CZmax = 1.2f;                  // (dimensionless)
    public float liftCurveSlope;                // (dimensionless)
    public float CL_preStall, CL_postStall;     // (dimensionless)

    // Blend between pre and post stall
    public float stallAngle;           // (rad)
    public float upperSigmoid, lowerSigmoid;    // (dimensionless)
    public float preStallFilter;                // (dimensionless)
    public float stallAngleMin = 15f;           // (deg)
    public float stallAngleMax = 35f;           // (deg)
    public float stallSharpness = 0.75f;        // (dimensionless)

    // Moment due to lift and camber
    public float CM;                            // (dimensionless)
    public float CM_0, CM_delta;                // (dimensionless)
    public float CoP_z;                         // (m)

    // Drag
    public float CD;                                    // (dimensionless)
    public float CD_induced, CD_profile, CD_0;          // (dimensionless)
    public float CD_pressure_0aoa, CD_pressure_90aoa;   // (dimensionless)
    public float CD_shear_0aoa, CD_shear_90aoa;         // (dimensionless)
    public float CD_normalFlatPlate = 1.2f;             // (dimensionless)
    public float CD_roughSphere = 0.5f;                 // (dimensionless)


    //  -------------------------------------------------------------------------------------------
    //      Aerodynamic Forces and Torques
    //  -------------------------------------------------------------------------------------------

    // The names of these variables are still quite painful, but I'm not sure how to fix it
    // They could be included in the frames but then it may become confusing as we don't
    // calculate them in EAB frame - so no forces would exist there
    public Vector3 dragForce_bodyFrame, liftForce_bodyFrame, rotationalMagnusLiftForce_axbody;                                  // (N)
    public Vector3 dampingTorque_bodyFrame, pressureTorque_bodyFrame, shearStressTorque_bodyFrame, momentDueToLift_bodyFrame;   // (Nm)
    public Vector3 momentDueToLift_eabFrame;                                                                                    // (Nm)

    public Vector3 resultantAerodynamicForce_earthFrame, resultantAerodynamicForce_bodyFrame;                                   // (N)
    public Vector3 resultantAerodynamicMoment_earthFrame, resultantAerodynamicMoment_bodyFrame;                                 // (Nm)


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
        planformArea = areaVector.y;

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

        // Start with earth frame
        earthFrame.SetResolvedWind(rb.velocity - externalFlowVelocity_inEarthFrame, rb.angularVelocity);
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
        // Linear - only care about the direction of flow, not resolving into axes
        // Bill says that really we should be looking at linear reynolds number in each axis separately
        // Linear uses diameter of the body - note we use the EAB chord as wind is resolved along this direction
        reynoldsNum_linear = rho * aeroBodyFrame.windVelocity.magnitude * EAB.chord_c / mu;

        // Rotational uses the circumference of the body
        reynoldsNum_x_rotational = Mathf.Abs(Mathf.PI * rho * aeroBodyFrame.angularWindVelocity.x * aeroBody.midAxis * aeroBody.majorAxis / mu);
        reynoldsNum_y_rotational = Mathf.Abs(Mathf.PI * rho * aeroBodyFrame.angularWindVelocity.y * aeroBody.majorAxis * aeroBody.majorAxis / mu);
        reynoldsNum_z_rotational = Mathf.Abs(Mathf.PI * rho * aeroBodyFrame.angularWindVelocity.z * aeroBody.majorAxis * aeroBody.majorAxis / mu);

        // Shear coefficient
        // Linear
        Cf_linear = reynoldsNum_linear == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_linear, 1f / 7f);

        // Rotational
        Cf_x_rotational = reynoldsNum_x_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_x_rotational, 1f / 7f);
        Cf_y_rotational = reynoldsNum_y_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_y_rotational, 1f / 7f);
        Cf_z_rotational = reynoldsNum_z_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_z_rotational, 1f / 7f);

        // Wind square magnitude should be the same for all frames - I think! 
        dynamicPressure = 0.5f * rho * aeroBodyFrame.windVelocity.sqrMagnitude;
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
        if(Mathf.Abs(alpha_deg) > 90f)
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

    
    public void GetAerodynamicCoefficients_7()
    {
        // Prandtyl Theory
        // Clamp lower value to min AR of 2. Otherwise lift curve slope gets lower than sin 2 alpha which is non physical
        // Check for the divide by zero, although I think a zero AR means bigger problems anyway
        aspectRatioCorrection_kAR = EAB.aspectRatio == 0f ? 0f : Mathf.Clamp(EAB.aspectRatio / (2f + EAB.aspectRatio), 0f, 1f);

        // This value needs checking for thickness to chord ratio of 1

        // Empirical correction to account for viscous effects across all thickness to chord ratios
        thicknessCorrection_kt = Mathf.Exp(-thicknessCorrection_labdat * EAB.thicknessToChordRatio_bOverc * EAB.thicknessToChordRatio_bOverc);

        // Emperical relation to allow for viscous effects
        // This could do with being in radians!
        stallAngle = stallAngleMin + (stallAngleMax - stallAngleMin) * Mathf.Exp(-EAB.aspectRatio / 2f);

        // Lifting line theory
        liftCurveSlope = 2f * Mathf.PI * aspectRatioCorrection_kAR * thicknessCorrection_kt;

        // Zero lift angle is set based on the amount of camber. This is physics based
        alpha_0 = -EAB.camberRatio;

        // Lift before and after stall
        CL_preStall = liftCurveSlope * (alpha - alpha_0);
        CL_postStall = 0.5f * CZmax * thicknessCorrection_kt * Mathf.Sin(2f * (alpha - alpha_0));

        // Sigmoid function for blending between pre and post stall
        // Wasting some calulcations here by converting to degrees...
        upperSigmoid = 1f / (1f + Mathf.Exp((stallAngle - Mathf.Rad2Deg * (alpha_0 - alpha)) * stallSharpness));
        lowerSigmoid = 1f / (1f + Mathf.Exp((-stallAngle - Mathf.Rad2Deg * (alpha_0 - alpha)) * stallSharpness));
        preStallFilter = lowerSigmoid - upperSigmoid;

        CL = preStallFilter * CL_preStall + (1 - preStallFilter) * CL_postStall;

        // Pitching moment at mid chord due to camber only active pre stall
        CM_0 = 0.25f * -liftCurveSlope * alpha_0 * preStallFilter;

        // Original equation for this is: z_cop = c/8 * (cos(2a) + 1)
        // Using trig identity for cos(2a) =  2*cos^2(a) - 1 to save on extra trig computations
        // Also, ax_c is the axis, not diameter so we x2 again
        CoP_z = 0.5f * EAB.midAxis * cosAlpha * cosAlpha;

        // Pitching moment because lift is applied at the centre of the body
        CM_delta = CL * CoP_z * cosAlpha / EAB.midAxis;
        CM = CM_0 + CM_delta;

        // Shear stress coefficients
        CD_shear_0aoa = 2f * Cf_linear;
        CD_shear_90aoa = EAB.thicknessToChordRatio_bOverc * 2f * Cf_linear;

        // Pressure coefficients
        CD_pressure_0aoa = EAB.thicknessToChordRatio_bOverc * CD_roughSphere;
        CD_pressure_90aoa = CD_normalFlatPlate - EAB.thicknessToChordRatio_bOverc * (CD_normalFlatPlate - CD_roughSphere);

        // An area correction factor is included for the pressure coefficient but is ommitted for the shear coefficient
        // This is because CD_shear_90aoa << CD_pressure_90aoa
        CD_profile = CD_shear_0aoa + EAB.thicknessToChordRatio_bOverc * CD_pressure_0aoa + (CD_shear_90aoa + CD_pressure_90aoa - CD_shear_0aoa - EAB.thicknessToChordRatio_bOverc * CD_pressure_0aoa) * sinAlpha * sinAlpha;
        CD_induced = (1f / (Mathf.PI * EAB.aspectRatio)) * CL * CL;
        CD = CD_profile + CD_induced;



        // This isn't really necessary.. I mean most of the coefficient stuff could probably be made more
        // efficient as the model just needs the forces. However, having the coefficients helps to validate
        // the model and make sure that the forces coming out of it are reasonable!

        // Magnus effect coefficient
        Vector3 RHO = Vector3.Scale(volumeVector, aeroBodyFrame.angularWindVelocity);
        CLr = 4f * Vector3.Cross(aeroBodyFrame.windVelocity, RHO) / (aeroBodyFrame.windVelocity.sqrMagnitude * planformArea);
    }

    public void GetDampingTorques_8()
    {
        // Scale by the direction here to preserve the signs of the angular velocity
        Vector3 directionalAngularVelocitySquared_bodyFrame = Vector3.Scale(Vector3.Scale(aeroBodyFrame.angularWindVelocity, aeroBodyFrame.angularWindVelocity), aeroBodyFrame.angularWindVelocity_normalised);

        float rotationalPressure_x = 0.5f * rho * aeroBody.midAxis * aeroBody.midAxis * aeroBody.midAxis * directionalAngularVelocitySquared_bodyFrame.x;
        float rotationalPressure_y = 0.5f * rho * aeroBody.majorAxis * aeroBody.majorAxis * aeroBody.majorAxis * directionalAngularVelocitySquared_bodyFrame.y;
        float rotationalPressure_z = 0.5f * rho * aeroBody.majorAxis * aeroBody.majorAxis * aeroBody.majorAxis * directionalAngularVelocitySquared_bodyFrame.z;

        // Pressure torques
        pressureTorque_bodyFrame.x = CD_normalFlatPlate * (areaVector.y / 2f) * rotationalPressure_x;
        pressureTorque_bodyFrame.y = CD_normalFlatPlate * (areaVector.z / 2f) * rotationalPressure_y;
        pressureTorque_bodyFrame.z = CD_normalFlatPlate * (areaVector.y / 2f) * rotationalPressure_z;

        // Shear stress torques
        shearStressTorque_bodyFrame.x = Cf_x_rotational * ellipsoidSurfaceArea * rotationalPressure_x;
        shearStressTorque_bodyFrame.y = Cf_y_rotational * ellipsoidSurfaceArea * rotationalPressure_y;
        shearStressTorque_bodyFrame.z = Cf_z_rotational * ellipsoidSurfaceArea * rotationalPressure_z;

        // Blending drag effects based on axis ratios
        dampingTorque_bodyFrame.x = (1 - lambda_x) * pressureTorque_bodyFrame.x + lambda_x * shearStressTorque_bodyFrame.x;
        dampingTorque_bodyFrame.y = (1 - lambda_y) * pressureTorque_bodyFrame.y + lambda_y * shearStressTorque_bodyFrame.y;
        dampingTorque_bodyFrame.z = (1 - lambda_z) * pressureTorque_bodyFrame.z + lambda_z * shearStressTorque_bodyFrame.z;

    }

    public void GetAerodynamicForces_9()
    {
        float qS = dynamicPressure * planformArea;

        // Minus sign here because... I don't really know ha!
        // Not sure why this is required though as the angular velocity is the same thing
        // Also, including the option to turn off this force as it seems to be much larger than regular lift even for small angular velocity
        rotationalMagnusLiftForce_axbody = includeMagnusEffect ? -rho * Vector3.Cross(aeroBodyFrame.windVelocity, 2f * Vector3.Scale(volumeVector, aeroBodyFrame.angularWindVelocity)) : Vector3.zero;

        Vector3 liftDirection = Vector3.Cross(aeroBodyFrame.windVelocity_normalised, angleOfAttackRotationVector);

        liftForce_bodyFrame = CL * qS * liftDirection;
        dragForce_bodyFrame = -CD * dynamicPressure * profileArea * aeroBodyFrame.windVelocity_normalised;

        // The minus sign here is dirty but I can't figure out why the pitching moment is always in the wrong direction?!
        momentDueToLift_eabFrame = new Vector3(-CM * qS * EAB.chord_c, 0, 0);
        momentDueToLift_bodyFrame = TransformEABToBody(momentDueToLift_eabFrame);

        // Transform forces and moments to earth frame
        resultantAerodynamicForce_bodyFrame = liftForce_bodyFrame + dragForce_bodyFrame + rotationalMagnusLiftForce_axbody;
        resultantAerodynamicForce_earthFrame = TransformBodyToEarth(resultantAerodynamicForce_bodyFrame);
        // Note the minus sign here because damping torque opposes the rotational velocity (angular wind) of the body
        resultantAerodynamicMoment_bodyFrame = momentDueToLift_bodyFrame - dampingTorque_bodyFrame;
        resultantAerodynamicMoment_earthFrame = TransformBodyToEarth(resultantAerodynamicMoment_bodyFrame);
    }

    public void ApplyAerodynamicForces_10()
    {
        rb.AddForce(resultantAerodynamicForce_earthFrame);
        rb.AddTorque(resultantAerodynamicMoment_earthFrame);
    }

    public void GetEllipsoid_1_to_2()
    {
        GetReferenceFrames_1();
        GetEllipsoidProperties_2();
    }

    public void CalculateAerodynamics_3_to_9()
    {
        ResolveWind_3();
        GetFlowCharacteristics_4();
        GetAeroAngles_5();
        GetEquivalentAerodynamicBody_6();
        GetAerodynamicCoefficients_7();
        GetDampingTorques_8();
        GetAerodynamicForces_9();
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

    public void Initialise()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogWarning("No RigidBody Component found on " + gameObject.name + ", adding one.");
            rb = gameObject.AddComponent<Rigidbody>();
            rb.angularDrag = 0;
            rb.drag = 0;
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
        CalculateAerodynamics_3_to_9();
        ApplyAerodynamicForces_10();
    }

    private void OnDrawGizmosSelected()
    {
        if (initialised) { 
            // Drag - Red       Lift - Green        Wind - Blue
            Gizmos.color = Color.red;
            Vector3 dragForce_axearth = TransformBodyToEarth(dragForce_bodyFrame);
            Gizmos.DrawLine(transform.position, transform.position + dragForce_axearth);

            // Lift
            Gizmos.color = Color.green;
            Vector3 liftForce_axearth = TransformBodyToEarth(liftForce_bodyFrame);
            Gizmos.DrawLine(transform.position, transform.position + liftForce_axearth);

            // Wind vector
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + earthFrame.windVelocity);

            // Resultant aerodynamic force
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + resultantAerodynamicForce_earthFrame);

            if (includeMagnusEffect)
            {
                // Magnus Lift
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, transform.position + TransformBodyToEarth(rotationalMagnusLiftForce_axbody));
            }
            //// Angle of attack rotation
            //Gizmos.color = Color.magenta;
            //Gizmos.DrawLine(transform.position, transform.position + TransformBodyToEarth(angleOfAttackRotationVector));

            //// Pitching moment
            //Gizmos.color = Color.black;
            //Gizmos.DrawLine(transform.position, transform.position + TransformBodyToEarth(pitchingMomentAxis));

            // EAB Forward
            Gizmos.color = Color.black;
            Gizmos.DrawLine(transform.position, transform.position + TransformEABToEarth(Vector3.forward));

        }
    }
}
