using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Aerodynamics : MonoBehaviour
{
    // For testing purposes all variables and functions are made public
    // This allows for easy probing of the aerodynamic model


    //  -------------------------------------------------------------------------------------------
    //      Unity Related Components
    //  -------------------------------------------------------------------------------------------

    
    // Unity's physics body, used to apply physics to the object
    public Rigidbody rb;


    //  -------------------------------------------------------------------------------------------
    //      Dimensions
    //  -------------------------------------------------------------------------------------------

    // Flag to determine if the model needs to recalculate dimensions at runtime
    public bool variableShape;

    // Dimensions of the aerodynamic body in order of ascending relative size
    // Thickness chord and span are selected in order of ascending dimensions of the ellipsoid
    // i.e. Span >= Chord >= Thickness
    public float thickness_b, chord_c, span_a;                                          // (m)
    public float aspectRatio, aspectRatio_clamped, aspectRatio_prime;                   // (dimensionless)
    public float thicknessToChordRatio_bOverc, thicknessToChordRatio_bOverc_prime;      // (dimensionless)
    public float thicknessCorrection;                                                   // (UNSURE)
    public float camber, camberRatio, camberRatio_prime;                                // (dimensionless)
    public float planformArea, profileArea;                                             // (m^2)
    Vector3 areaVector;                                                                 // (m^2)
    Vector3 volumeVector;                                                               // (m^3)
    public float ellipsoidSurfaceArea;                                                  // (m^2)
    public float ex, ey, ez;                                                            // (dimensionless)
    public float ex2, ey2, ez2;                                                         // (dimensionless)

    // Ellipsoid axes - used for equivalent aerodynamic body projection
    public float ax_b_minor, ax_c_mid, ax_a_major;                                      // (m)
    public float ax_b_minor_prime, ax_c_mid_prime, ax_a_major_prime;                    // (m)


    //  -------------------------------------------------------------------------------------------
    //      Coordinate Systems and Transformations
    //  -------------------------------------------------------------------------------------------

    // The body axes used to resolve wind into local coordinates
    public Transform referenceAxesTransform;
    Quaternion localToReferenceRotation;

    // Directions of useful features
    Vector3 liftingPlaneNormal_y;   // The unit vector normal to the lifting plane  (unit vector)
    Vector3 chordAxis_z;            // The direction of the chord dimension         (unit vector)

    // Sideslip (beta) and angle of attack (alpha) vectors
    Vector3 sideslipRotationVector;                     // (UNSURE)
    Vector3 angleOfAttackRotationVector;                // (UNSURE)
    public float alpha, beta;                           // (rad)
    public float sinAlpha, cosAlpha, sinBeta, cosBeta;  // (dimensionless)


    //  -------------------------------------------------------------------------------------------
    //      Fluid Properties and Characteristics
    //  -------------------------------------------------------------------------------------------

    public Vector3 externalWind;                                    // (m/s)
    public Vector3 wind_localVelocity, wind_globalVelocity;                // (m/s)
    public Vector3 wind_localDirection;                                    // (unit vector)
    public Vector3 wind_directionAlongLiftingPlane_zprime;                 // (m/s)
    public Vector3 angularVelocity_global, angularVelocity_referenceAxes;  // (rad/s)
    public Vector3 resolvedAngularVelocitySquared;                         // (rad^2/s^2)

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

    // Lift and rotational lift
    public float CL;                            // (dimensionless)
    public float CLr;                           // (dimensionless)
    public float CLmax = 1.2f;                  // (dimensionless)
    public float CZmax = 1.2f;                  // (dimensionless)
    public float liftCurveSlope;                // (dimensionless)
    public float CL_preStall, CL_postStall;     // (dimensionless)

    // Blend between pre and post stall
    public float alpha_0, stallAngle;           // (rad)
    public float upperSigmoid, lowerSigmoid;    // (dimensionless)
    public float preStallFilter;                // (dimensionless)
    public float stallAngleMin = 15f;           // (deg)
    public float stallAngleMax = 35f;           // (deg)
    public float stallSharpness = 0.75f;        // (dimensionless)

    // Moment due to lift and camber
    public float CM;                            // (dimensionless)
    public float CM_0, CM_delta;                // (dimensionless)
    public float CoP;                           // (m)

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

    public Vector3 dragForce, liftForce, rotationalMagnusLiftForce;                    // (N)
    public Vector3 dampingTorque, pressureTorque, shearStressTorque, momentDueToLift;  // (Nm)

    public Vector3 resultantAerodynamicForce_global, resultantAerodynamicForce_local;  // (N)
    public Vector3 resultantAerodynamicMoment_global, resultantAerodynamicMoment_local;// (Nm)


    //  -------------------------------------------------------------------------------------------
    //      Aerodynamic Functions
    //  -------------------------------------------------------------------------------------------

    // These functions are marked with numbers representing the order in which they should be called
    // When inspecting various parts of the model it may make more sense to adjust values and skip
    // some of these computation steps
    // Two groups are also included:
    //      - GetEllipsoid_1_to_2() which handles the dimesions of the body
    //      - CalculateAerodynamics_3_to_11() which resolves the body into the wind and computes the coefficients and forces
    // The ApplyAerodynamics_12() function is then required to apply the forces to the rigidbody component during simulation
    public void GetBodyDimensions_1()
    {
        // Long winded approach to getting the span, chord and thickness of the body
        // based on the scale of the object - this could change in the future to be
        // inputs by the user! Or it could default to using the bounding box of the mesh

        // For now, we're just looking at the scale of the object to obtain ellipsoid axes
        float x = transform.localScale.x;
        float y = transform.localScale.y;
        float z = transform.localScale.z;

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
                liftingPlaneNormal_y = new Vector3(0, 0, 1);
                chordAxis_z = new Vector3(0, 1, 0);
                span_a = x;
                chord_c = y;
                thickness_b = z;
            }
            else
            {
                if (x >= z)
                {
                    // Then x > z > y so we don't need to do anything
                    liftingPlaneNormal_y = new Vector3(0, 1, 0);
                    chordAxis_z = new Vector3(0, 0, 1);
                    span_a = x;
                    chord_c = z;
                    thickness_b = y;
                }
                else
                {
                    // Then z > x > y so we need to swap chord and span
                    liftingPlaneNormal_y = new Vector3(0, 1, 0);
                    chordAxis_z = new Vector3(1, 0, 0);
                    span_a = z;
                    chord_c = x;
                    thickness_b = y;
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
                    liftingPlaneNormal_y = new Vector3(0, 0, 1);
                    chordAxis_z = new Vector3(1, 0, 0);
                    span_a = y;
                    chord_c = x;
                    thickness_b = z;
                }
                else
                {
                    // Then y > z > x
                    liftingPlaneNormal_y = new Vector3(1, 0, 0);
                    chordAxis_z = new Vector3(0, 0, 1);
                    span_a = y;
                    chord_c = z;
                    thickness_b = x;
                }
            }
            else
            {
                // Then z > y > x
                liftingPlaneNormal_y = new Vector3(1, 0, 0);
                chordAxis_z = new Vector3(0, 1, 0);
                span_a = z;
                chord_c = y;
                thickness_b = x;
            }
        }
        // Rotate the reference axes to line up with (span, thickness, chord) == (a, b, c)
        localToReferenceRotation = Quaternion.LookRotation(chordAxis_z, liftingPlaneNormal_y);
        referenceAxesTransform.localRotation = localToReferenceRotation;
    }

    public void GetEllipsoidProperties_2()
    {
        // Aerodynamic related properties
        aspectRatio = span_a / (Mathf.PI * chord_c);
        camberRatio = camber / chord_c;
        thicknessToChordRatio_bOverc = thickness_b / chord_c;

        // Area and volume vectors
        areaVector = Mathf.PI * new Vector3(thickness_b * chord_c, span_a * chord_c, span_a * thickness_b);
        float piOver6 = Mathf.PI / 6f;
        volumeVector.x = piOver6 * span_a * chord_c * chord_c;
        volumeVector.y = piOver6 * thickness_b * span_a * span_a;
        volumeVector.z = piOver6 * chord_c * span_a * span_a;

        // Planform area - area of the aerodynamic body in the lifting plane
        planformArea = areaVector.y;

        // Approximate surface area of ellipsoid
        ellipsoidSurfaceArea = 4f * Mathf.PI * Mathf.Pow((1f / 3f) * (Mathf.Pow(span_a * thickness_b / 4f, 1.6f) + Mathf.Pow(span_a * chord_c / 4f, 1.6f) + Mathf.Pow(thickness_b * chord_c / 4f, 1.6f)), (1f / 1.6f));

        // Eccentricities
        ex = Mathf.Sqrt(chord_c * chord_c / 4f - thickness_b * thickness_b / 4f) / (chord_c / 2f);
        ey = Mathf.Sqrt(span_a * span_a / 4f - chord_c * chord_c / 4f) / (span_a / 2f);
        ez = Mathf.Sqrt(span_a * span_a / 4f - thickness_b * thickness_b / 4f) / (span_a / 2f);

        // Squares of eccentricities
        ex2 = ex * ex;
        ey2 = ey * ey;
        ez2 = ez * ez;

        // Set the inertia of the body as a hollow ellipsoid
        rb.inertiaTensor = rb.mass * new Vector3(thickness_b * thickness_b / 16f + chord_c * chord_c / 16f, span_a * span_a / 16f + chord_c * chord_c / 16f, span_a * span_a / 16f + thickness_b * thickness_b / 16f);
    }

    public void GetLocalWind_3()
    {
        // Linear wind
        wind_localVelocity = referenceAxesTransform.InverseTransformDirection(externalWind - rb.velocity);
        wind_localDirection = wind_localVelocity.normalized;
        wind_globalVelocity = externalWind - rb.velocity;

        // Rotational wind - assuming no external vorticity
        angularVelocity_global = rb.angularVelocity;
        angularVelocity_referenceAxes = referenceAxesTransform.InverseTransformDirection(rb.angularVelocity);
        resolvedAngularVelocitySquared = referenceAxesTransform.InverseTransformDirection(Vector3.Scale(Vector3.Scale((rb.angularVelocity), rb.angularVelocity), rb.angularVelocity.normalized));
    }

    public void GetDynamicPressure_4()
    {
        dynamicPressure = 0.5f * rho * wind_localVelocity.sqrMagnitude;
    }

    public void GetReynoldsNumber_5()
    {
        // Linear - only care about the direction of flow, not resolving into axes
        // Linear uses diameter of the body
        reynoldsNum_linear = rho * wind_localVelocity.magnitude * chord_c / mu;

        // Rotational - should maybe consider doing the same as the linear flow
        // Rotational uses the circumference of the body
        reynoldsNum_x_rotational = Mathf.Abs(Mathf.PI * rho * angularVelocity_referenceAxes.x * (chord_c / 4f) * span_a / mu);
        reynoldsNum_y_rotational = Mathf.Abs(Mathf.PI * rho * angularVelocity_referenceAxes.y * (span_a / 4f) * span_a / mu);
        reynoldsNum_z_rotational = Mathf.Abs(Mathf.PI * rho * angularVelocity_referenceAxes.z * (span_a / 4f) * span_a / mu);
    }

    public void GetAeroAngles_6()
    {
        // Get the wind vector parallel to the lifting plane
        wind_directionAlongLiftingPlane_zprime = Vector3.Scale(wind_localDirection, new Vector3(1, 0, 1));

        // Sideslip
        sideslipRotationVector = Vector3.Cross(Vector3.forward, wind_directionAlongLiftingPlane_zprime) * Vector3.Dot(Vector3.forward, wind_directionAlongLiftingPlane_zprime);
        beta = Mathf.Atan2(wind_localVelocity.x, wind_localVelocity.z);
        sinBeta = Mathf.Sin(beta);
        cosBeta = Mathf.Cos(beta);

        // Angle of attack
        angleOfAttackRotationVector = Vector3.Cross(wind_directionAlongLiftingPlane_zprime, wind_localDirection) * Vector3.Dot(wind_directionAlongLiftingPlane_zprime, wind_localDirection);
        alpha = Mathf.Atan2(-wind_localVelocity.y, wind_directionAlongLiftingPlane_zprime.magnitude);
        sinAlpha = Mathf.Sin(alpha);
        cosAlpha = Mathf.Cos(alpha);
    }

    public void GetEquivalentAerodynamicBody_7()
    {
        // Work out shape of equivalent aero body
        // The body has the same aspect ratio and and span as the actual viewed shape but with zero sweep

        // Need to switch to ellipsoid radii instead of diameters
        ax_a_major = span_a / 2f;
        ax_b_minor = thickness_b / 2f;
        ax_c_mid = chord_c / 2f;

        // Resolving in sideslip direction by rotating about the normal to the lift plane
        ax_c_mid_prime = ax_a_major * ax_c_mid / Mathf.Sqrt((ax_c_mid * ax_c_mid * sinBeta * sinBeta) + (ax_a_major * ax_a_major * cosBeta * cosBeta));
        // Find major axis based on constant area of ellipse
        ax_a_major_prime = ax_a_major * ax_c_mid / ax_c_mid_prime;
        // No change to thickness
        ax_b_minor_prime = ax_b_minor;

        // Work out aero parameters of equivalent body
        aspectRatio_prime = ax_a_major_prime / ax_c_mid_prime;
        thicknessToChordRatio_bOverc_prime = ax_b_minor_prime / ax_c_mid_prime;
        camberRatio_prime = camberRatio * chord_c / (2f * ax_c_mid_prime);

        // Profile area is the projection in the wind direction
        profileArea = Vector3.Scale(areaVector, wind_localDirection).magnitude;
    }

    public void GetAerodynamicCoefficients_8()
    {
        // Prandtyl Theory
        // Clamp lower value to min AR of 2. Otherwise lift curve slope gets lower than sin 2 alpha which is non physical
        aspectRatio_clamped = Mathf.Clamp(aspectRatio_prime / (2f + aspectRatio_prime), 0, 1f);

        // This value needs checking for thickness to chord ratio of 1

        // Empirical correction to account for viscous effects across all thickness to chord ratios
        thicknessCorrection = Mathf.Exp(-thicknessToChordRatio_bOverc_prime * thicknessToChordRatio_bOverc_prime * 6f);

        // Emperical relation to allow for viscous effects
        // This could do with being in radians!
        stallAngle = stallAngleMin + (stallAngleMax - stallAngleMin) * Mathf.Exp(-aspectRatio / 2f);

        // Lifting line theory (I think?)
        liftCurveSlope = 2f * Mathf.PI * aspectRatio_clamped * thicknessCorrection;

        // Zero lift angle is set based on the amount of camber. This is physics based
        alpha_0 = -camberRatio;

        // Lift before and after stall
        CL_preStall = liftCurveSlope * (alpha - alpha_0);
        CL_postStall = 0.5f * CZmax * thicknessCorrection * Mathf.Sin(2 * (alpha - alpha_0));

        // Sigmoid function for blending between pre and post stall
        // Wasting some calulcations here by converting to degrees...
        upperSigmoid = 1f / (1f + Mathf.Exp((stallAngle - Mathf.Rad2Deg * (alpha_0 - alpha)) * stallSharpness));
        lowerSigmoid = 1f / (1f + Mathf.Exp((-stallAngle - Mathf.Rad2Deg * (alpha_0 - alpha)) * stallSharpness));
        preStallFilter = lowerSigmoid - upperSigmoid;

        CL = preStallFilter * CL_preStall + (1 - preStallFilter) * CL_postStall;

        // Pitching moment at mid chord due to camber only active pre stall
        CM_0 = 0.25f * -liftCurveSlope * alpha_0 * preStallFilter;

        // Using trig identity for cos(2a) = cos^2(a) - sin^2(a) to save on extra trig computations
        CoP = 0.5f * ax_c_mid_prime * cosAlpha * cosAlpha;

        // Pitching moment because lift is treated as acting at the centre of the body
        CM_delta = CL * CoP * cosAlpha / ax_c_mid_prime;
        CM = CM_0 + CM_delta;

        // Shear stress coefficients
        CD_shear_0aoa = 2f * Cf_linear;
        CD_shear_90aoa = thicknessToChordRatio_bOverc * 2f * Cf_linear;

        // Pressure coefficients
        CD_pressure_0aoa = thicknessToChordRatio_bOverc * CD_roughSphere;
        CD_pressure_90aoa = CD_normalFlatPlate - thicknessToChordRatio_bOverc * (CD_normalFlatPlate - CD_roughSphere);

        // An area correction factor is included for the pressure coefficient but is ommitted for the shear coefficient
        // This is because CD_shear_90aoa << CD_pressure_90aoa
        CD_profile = CD_shear_0aoa + thicknessToChordRatio_bOverc * CD_pressure_0aoa + (CD_shear_90aoa + CD_pressure_90aoa - CD_shear_0aoa - thicknessToChordRatio_bOverc * CD_pressure_0aoa) * sinAlpha * sinAlpha;
        CD_induced = (1f / (Mathf.PI * aspectRatio_prime)) * CL * CL;
        CD = CD_profile + CD_induced;
    }

    public void GetShearStressCoefficients_9()
    {
        // Linear
        Cf_linear = reynoldsNum_linear == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_linear, 1f / 7f);

        // Rotational
        Cf_x_rotational = reynoldsNum_x_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_x_rotational, 1f / 7f);
        Cf_y_rotational = reynoldsNum_y_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_y_rotational, 1f / 7f);
        Cf_z_rotational = reynoldsNum_z_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_z_rotational, 1f / 7f);
    }

    public void GetDampingTorques_10()
    {
        // How do I know which axes to use in the computation?? It can't be chord * span^4 for every axis!

        /*  Questions/Thoughts
         *  
         *      - Why 1/64? I can see 1/16 because that accounts for span = 2a while we only want a
         *      - Why use pi*a*c/4 and not /2?
         */
        float rotationalPressure_x = rho * chord_c * chord_c * chord_c * resolvedAngularVelocitySquared.x;
        float rotationalPressure_y = rho * span_a * span_a * span_a * resolvedAngularVelocitySquared.y;
        float rotationalPressure_z = rho * span_a * span_a * span_a * resolvedAngularVelocitySquared.z;

        // Pressure torques
        pressureTorque.x = (1f / 64f) * CD_normalFlatPlate * (areaVector.y / 4f) * rotationalPressure_x;
        pressureTorque.y = (1f / 64f) * CD_normalFlatPlate * (areaVector.z / 4f) * rotationalPressure_y;
        pressureTorque.z = (1f / 64f) * CD_normalFlatPlate * (areaVector.y / 4f) * rotationalPressure_z;

        // Shear stress torques
        //shearStressTorque.x = (1f / 128f) * Cf_x * ellipsoidSurfaceArea * rotationalPressure_x;
        //shearStressTorque.y = (1f / 128f) * Cf_y * ellipsoidSurfaceArea * rotationalPressure_y;
        //shearStressTorque.z = (1f / 128f) * Cf_z * ellipsoidSurfaceArea * rotationalPressure_z;
        shearStressTorque.x = (Mathf.PI / 64f) * Cf_x_rotational * ellipsoidSurfaceArea * rotationalPressure_x;
        shearStressTorque.y = (Mathf.PI / 64f) * Cf_y_rotational * ellipsoidSurfaceArea * rotationalPressure_y;
        shearStressTorque.z = (Mathf.PI / 64f) * Cf_z_rotational * ellipsoidSurfaceArea * rotationalPressure_z;

        dampingTorque.x = ex2 * pressureTorque.x + (1f - ex2) * shearStressTorque.x;
        dampingTorque.y = ey2 * pressureTorque.y + (1f - ey2) * shearStressTorque.y;
        dampingTorque.z = ez2 * pressureTorque.z + (1f - ez2) * shearStressTorque.z;

        // Damping opposes the velocity of the object
        dampingTorque *= -1f;
    }

    public void GetAerodynamicForces_11()
    {
        float qS = dynamicPressure * planformArea;

        rotationalMagnusLiftForce = -Vector3.Cross(rho * Vector3.Scale(volumeVector, angularVelocity_referenceAxes), wind_localVelocity);

        Vector3 liftDirection = Vector3.Cross(wind_localDirection, angleOfAttackRotationVector);

        liftForce = CL * qS * liftDirection;
        dragForce = CD * dynamicPressure * profileArea * wind_localDirection;
        momentDueToLift = CM * qS * chord_c * angleOfAttackRotationVector;

        resultantAerodynamicForce_local = liftForce + dragForce + rotationalMagnusLiftForce;
        resultantAerodynamicForce_global = referenceAxesTransform.TransformDirection(resultantAerodynamicForce_local);

        resultantAerodynamicMoment_local = momentDueToLift + dampingTorque;
        resultantAerodynamicMoment_global = referenceAxesTransform.TransformDirection(resultantAerodynamicMoment_local);
    }

    public void ApplyAerodynamicForces_12()
    {
        rb.AddForce(resultantAerodynamicForce_global);
        rb.AddTorque(resultantAerodynamicMoment_global);
    }

    public void GetEllipsoid_1_to_2()
    {
        GetBodyDimensions_1();
        GetEllipsoidProperties_2();
    }

    public void CalculateAerodynamics_3_to_11()
    {
        GetLocalWind_3();
        GetDynamicPressure_4();
        GetReynoldsNumber_5();
        GetAeroAngles_6();
        GetEquivalentAerodynamicBody_7();
        GetAerodynamicCoefficients_8();
        GetShearStressCoefficients_9();
        GetDampingTorques_10();
        GetAerodynamicForces_11();
    }

    public void SetAlpha_rad_3_to_6(float alpha_rad)
    {
        // Assumes the body has zero velocity
        externalWind = new Vector3(0, -Mathf.Sin(alpha_rad), Mathf.Cos(alpha_rad));

        GetLocalWind_3();
        GetDynamicPressure_4();
        GetReynoldsNumber_5();
        GetAeroAngles_6();
    }

    public void SetAlpha_deg_3_to_6(float alpha_deg)
    {
        // Assumes the body has zero velocity
        float alpha_rad = Mathf.Deg2Rad * alpha_deg;
        externalWind = new Vector3(0, -Mathf.Sin(alpha_rad), Mathf.Cos(alpha_rad));

        GetLocalWind_3();
        GetDynamicPressure_4();
        GetReynoldsNumber_5();
        GetAeroAngles_6();
    }

    public void Initialise()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogWarning("No RigidBody Component found on " + gameObject.name + ", adding one.");
            rb = gameObject.AddComponent<Rigidbody>();
            rb.angularDrag = 0;
        }

        if (!referenceAxesTransform)
        {
            GameObject go = new GameObject("Reference Axes");
            go.transform.parent = transform;
            referenceAxesTransform = go.transform;
            referenceAxesTransform.localScale = Vector3.one;
        }

        GetEllipsoid_1_to_2();
    }


    void Awake()
    {
        Initialise();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (variableShape)
        {
            GetEllipsoid_1_to_2();
        }
        CalculateAerodynamics_3_to_11();
        ApplyAerodynamicForces_12();
    }

    private void OnDrawGizmos()
    {
        if (referenceAxesTransform)
        {
            // Drag - Red       Lift - Green        Wind - Blue
            Gizmos.color = Color.red;
            Vector3 dragWorld = referenceAxesTransform.TransformDirection(dragForce);
            Gizmos.DrawLine(transform.position, transform.position + dragWorld);
            Gizmos.color = Color.green;
            Vector3 liftWorld = referenceAxesTransform.TransformDirection(liftForce);
            Gizmos.DrawLine(transform.position, transform.position + liftWorld);

            // Wind vector
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position - wind_globalVelocity);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + resultantAerodynamicForce_global);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + referenceAxesTransform.TransformDirection(angleOfAttackRotationVector));

            Gizmos.color = Color.yellow;
            Vector3 liftRotationalWorld = referenceAxesTransform.TransformDirection(rotationalMagnusLiftForce);
            Gizmos.DrawLine(transform.position, transform.position + liftRotationalWorld);

            // This doesn't work
            //Gizmos.color = Color.white;
            //Gizmos.DrawSphere(referenceAxesTransform.TransformPoint(new Vector3(0, 0, CoP)), thickness_b*1.1f);
        }
    }
}
