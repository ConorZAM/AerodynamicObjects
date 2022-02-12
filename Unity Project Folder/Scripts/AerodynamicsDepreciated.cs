using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class AerodynamicsDepreciated : MonoBehaviour
{
    // For testing purposes all variables and functions are made public
    // This allows for easy probing of the aerodynamic model


    //  -------------------------------------------------------------------------------------------
    //      Unity Related Components
    //  -------------------------------------------------------------------------------------------

    
    // Unity's physics body, used to apply physics to the object
    public Rigidbody rb;

    
    //  -------------------------------------------------------------------------------------------
    //      Dimensions and Frames of Reference
    //  -------------------------------------------------------------------------------------------

    // Flag to determine if the model needs to recalculate dimensions at runtime
    public bool dynamicallyVariableShape;

    public class ReferenceFrame
    {
        /*  Using a reference frame class to hold the coordinate transform for
         *  each frame of reference used in the aerodynamics model. This class
         *  also contains the dimensions of the object projected into the frame.
         *  This is only really required for the equivalent aerodynamic body
         *  which is used to account for the zero sideslip case
         */

        // Diameters of the ellipsoid body, using aerodynamic notation
        public float span_a, thickness_b, chord_c;      // (m)

        // Radii of the ellipsoid body - for convenience so that multiples of two are neglected
        public float minorAxis, midAxis, majorAxis;     // (m)

        // Ratios
        public float aspectRatio;                       // (dimensionless)
        public float thicknessToChordRatio_bOverc;      // (dimensionless)
        public float camberRatio;                       // (dimensionless)

        // Wind resolved into this coordinate frame
        public Vector3 windVelocity;                    // (m/s)
        public Vector3 windVelocity_normalised;         // (unit vector
        public Vector3 angularWindVelocity;             // (rad/s)
        public Vector3 angularWindVelocity_normalised;  // (unit vector)

        // The rotation from the object's frame of reference to this frame of reference
        public Quaternion objectToFrameRotation;

        public ReferenceFrame()
        {

        }

    }

    /* The frames of reference and their notations in this model are:
     *  - Earth                                                     (axearth)
     *  The earth frame is equivalent to Unity's global coordinates
     *  
     *  - Object                                                    (axobject)
     *  The object frame is equivalent to Unity's local coordinates
     *  i.e. the arbitrary local (x,y,z) for the aerodynamic object
     *  
     *  - Body                                                      (axbody)
     *  The body frame is a rotation of the object frame
     *  such that (x, y, z) are aligned with (span, thickness, chord)
     *  Thickness chord and span are selected in order of ascending
     *  dimensions of the ellipsoid, i.e. span >= chord >= thickness
     *  
     *  - Equivalent Aerodynamic Body                               (axeab)
     *  The equivalent aerodynamic body is the resolved body axes
     *  such that the equivalent body has zero sideslip. This is
     *  both a rotation of the frame and a projection of dimensions
     *  of the body
     */


    //  -----------------------------------------------------------------
    //      Body Frame

    // The body transform and rotation which are used to resolve the wind from earth coordinates
    public Transform bodyAxesTransform;
    Quaternion objectToBodyRotation;

    // Diameters of the ellipsoid body, using aerodynamic notation
    public float span_a_axbody, thickness_b_axbody, chord_c_axbody;   // (m)

    // Radii of the ellipsoid body - for convenience so that multiples of two are neglected
    public float minorAxis_axbody, midAxis_axbody, majorAxis_axbody;  // (m)

    // Ratios
    public float aspectRatio_axbody;                        // (dimensionless)
    public float thicknessToChordRatio_bOverc_axbody; // (dimensionless)
    public float camber_axbody, camberRatio_axbody; // (dimensionless)

    // The projection from body to eab assumes constant areas for the ellipsoid body
    // therefore planform and profile areas are the same in each frame
    public float planformArea, profileArea;                                             // (m^2)
    Vector3 areaVector;                                                                 // (m^2)

    // Not sure if the same applies to the volumes, I would assume so though
    Vector3 volumeVector;                                                               // (m^3)
    public float ellipsoidSurfaceArea;                                                  // (m^2)
    public float lambda_x, lambda_y, lambda_z;                                  // (dimensionless)
    //public float ex2, ey2, ez2;                                                         // (dimensionless)

    //  -----------------------------------------------------------------
    //      Equivalent Aerodynamic Body Frame

    // Diameters of the ellipsoid body, using aerodynamic notation
    public float span_a_axeab, thickness_b_axeab, chord_c_axeab;      // (m)

    // Radii of the ellipsoid body - for convenience so that multiples of two are neglected
    public float minorAxis_axeab, midAxis_axeab, majorAxis_axeab; // (m)

    // Ratios
    public float aspectRatio_clamped_axeab, aspectRatio_axeab;                   // (dimensionless)
    public float thicknessToChordRatio_bOverc_axeab, thicknessCorrection_axeab;                                                   // (dimensionless)
    public float camberRatio_axeab;                                // (dimensionless)


    //  -------------------------------------------------------------------------------------------
    //      Coordinate Systems and Transformations
    //  -------------------------------------------------------------------------------------------

    

    // Directions of useful features
    Vector3 axbody_y;   // The unit vector normal to the lifting plane  (unit vector)
    Vector3 axbody_z;            // The direction of the chord dimension         (unit vector)

    // Sideslip (beta) and angle of attack (alpha) vectors
    Vector3 sideslipRotationVector;                     // (UNSURE)
    Vector3 angleOfAttackRotationVector;                // (UNSURE)
    public float alpha, beta;                           // (rad)
    public float alpha_deg, beta_deg;                   // (degrees)
    public float sinAlpha, cosAlpha, sinBeta, cosBeta;  // (dimensionless)


    //  -------------------------------------------------------------------------------------------
    //      Fluid Properties and Characteristics
    //  -------------------------------------------------------------------------------------------

    public Vector3 externalWind_axearth;                                    // (m/s)
    public Vector3 windVelocity_axbody, windVelocity_axearth;                // (m/s)
    public Vector3 windVelocity_normalised_axbody;                                    // (unit vector)
    public Vector3 windVelocityAlongLiftingPlane_axbody;                 // (m/s)
    public Vector3 windVelocityAlongLiftingPlane_normalised_axbody;                 // (unit vector)
    float windVelocity_axeab_z;     // (m/s)
    public Vector3 angularVelocity_axearth, angularVelocity_axbody;  // (rad/s)
    public Vector3 angularVelocitySquared_withDirection_axbody;                         // (rad^2/s^2)

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
    public float eab_CoP_chordwise;                           // (m)

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

    public Vector3 dragForce_axbody, liftForce_axbody, rotationalMagnusLiftForce_axbody;                    // (N)
    public Vector3 dampingTorque_axbody, pressureTorque_axbody, shearStressTorque_axbody, momentDueToLift_axbody;  // (Nm)

    public Vector3 resultantAerodynamicForce_axearth, resultantAerodynamicForce_axbody;  // (N)
    public Vector3 resultantAerodynamicMoment_axearth, resultantAerodynamicMoment_axbody;// (Nm)


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
                axbody_y = new Vector3(0, 0, 1);
                axbody_z = new Vector3(0, 1, 0);
                SetBodyDimensions(x, z, y);
            }
            else
            {
                if (x >= z)
                {
                    // Then x > z > y so we don't need to do anything
                    axbody_y = new Vector3(0, 1, 0);
                    axbody_z = new Vector3(0, 0, 1);
                    SetBodyDimensions(x, y, z);
                }
                else
                {
                    // Then z > x > y so we need to swap chord and span
                    axbody_y = new Vector3(0, 1, 0);
                    axbody_z = new Vector3(1, 0, 0);
                    SetBodyDimensions(z, y, x);
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
                    axbody_y = new Vector3(0, 0, 1);
                    axbody_z = new Vector3(1, 0, 0);
                    SetBodyDimensions(y, z, x);
                }
                else
                {
                    // Then y > z > x
                    axbody_y = new Vector3(1, 0, 0);
                    axbody_z = new Vector3(0, 0, 1);
                    SetBodyDimensions(y, x, z);
                }
            }
            else
            {
                // Then z > y > x
                axbody_y = new Vector3(1, 0, 0);
                axbody_z = new Vector3(0, 1, 0);
                SetBodyDimensions(z, x, y);
            }
        }
        // Rotate the reference axes to line up with (span, thickness, chord) == (a, b, c)
        objectToBodyRotation = Quaternion.LookRotation(axbody_z, axbody_y);
        bodyAxesTransform.localRotation = objectToBodyRotation;
    }

    private void SetBodyDimensions(float span, float thickness, float chord)
    {
        span_a_axbody = span;
        majorAxis_axbody = span / 2f;

        chord_c_axbody = chord;
        midAxis_axbody = chord / 2f;

        thickness_b_axbody = thickness;
        minorAxis_axbody = thickness / 2f;
    }

    public void GetEllipsoidProperties_2()
    {
        // Aerodynamic related properties
        aspectRatio_axbody = span_a_axbody / (Mathf.PI * chord_c_axbody);
        camberRatio_axbody = camber_axbody / chord_c_axbody;
        thicknessToChordRatio_bOverc_axbody = thickness_b_axbody / chord_c_axbody;

        // Area and volume vectors
        areaVector = Mathf.PI * new Vector3(thickness_b_axbody * chord_c_axbody, span_a_axbody * chord_c_axbody, span_a_axbody * thickness_b_axbody);
        float piOver6 = Mathf.PI / 6f;
        volumeVector.x = piOver6 * span_a_axbody * chord_c_axbody * chord_c_axbody;
        volumeVector.y = piOver6 * thickness_b_axbody * span_a_axbody * span_a_axbody;
        volumeVector.z = piOver6 * chord_c_axbody * span_a_axbody * span_a_axbody;

        // Planform area - area of the aerodynamic body in the lifting plane
        planformArea = areaVector.y;

        // Approximate surface area of ellipsoid
        ellipsoidSurfaceArea = 4f * Mathf.PI * Mathf.Pow((1f / 3f) * (Mathf.Pow(span_a_axbody * thickness_b_axbody / 4f, 1.6f) + Mathf.Pow(span_a_axbody * chord_c_axbody / 4f, 1.6f) + Mathf.Pow(thickness_b_axbody * chord_c_axbody / 4f, 1.6f)), (1f / 1.6f));


        // Axis Ratios
        lambda_x = thickness_b_axbody / chord_c_axbody;
        lambda_y = chord_c_axbody / span_a_axbody;
        lambda_z = thickness_b_axbody / span_a_axbody;


        // I'm putting this here so I don't have to go back and redo all my other function calls
        // However, I see that these functions should be internal in the future and that
        // other classes interacting with the aerodynamics object should see more general functions
        // such as GetPhysicalProperties, GetWind, GetCoefficients... etc
        SetMassProperties_2();
    }

    public void SetMassProperties_2()
    {
        // Set the inertia of the body as
        // Solid Ellipsoid
        rb.inertiaTensor = 0.2f * rb.mass * new Vector3(thickness_b_axbody * thickness_b_axbody / 4f + chord_c_axbody * chord_c_axbody / 4f, span_a_axbody * span_a_axbody / 4f + chord_c_axbody * chord_c_axbody / 4f, span_a_axbody * span_a_axbody / 4f + thickness_b_axbody * thickness_b_axbody / 4f);
    }

    public void GetLocalWind_3()
    {
        // Linear wind
        windVelocity_axearth = externalWind_axearth - rb.velocity;
        windVelocity_axbody = bodyAxesTransform.InverseTransformDirection(windVelocity_axearth);
        windVelocity_normalised_axbody = windVelocity_axbody.normalized;

        // Get the component of the wind vector which is parallel to the lifting plane
        windVelocityAlongLiftingPlane_axbody = new Vector3(windVelocity_axbody.x, 0, windVelocity_axbody.z);
        windVelocityAlongLiftingPlane_normalised_axbody = windVelocityAlongLiftingPlane_normalised_axbody.normalized;

        // We can take the magnitude here as we only consider alpha ranging from -90 to 90 degrees
        // thus, the direction of the z component is not needed - only the vertical direction of wind is required
        windVelocity_axeab_z = windVelocityAlongLiftingPlane_axbody.magnitude;

        // This check is here because if zprime is (0,0,0) then the angle of attack becomes 0 when it should be +-90 deg
        // This check might be unnecessary
        if (windVelocityAlongLiftingPlane_normalised_axbody == Vector3.zero)
        {
            windVelocityAlongLiftingPlane_normalised_axbody = Vector3.forward;
        }

        // Rotational wind - assuming no external vorticity
        angularVelocity_axearth = rb.angularVelocity;
        angularVelocity_axbody = bodyAxesTransform.InverseTransformDirection(angularVelocity_axearth);
        angularVelocitySquared_withDirection_axbody = bodyAxesTransform.InverseTransformDirection(Vector3.Scale(Vector3.Scale((rb.angularVelocity), rb.angularVelocity), rb.angularVelocity.normalized));
    }

    public void GetDynamicPressure_4()
    {
        dynamicPressure = 0.5f * rho * windVelocity_axbody.sqrMagnitude;
    }

    public void GetReynoldsNumber_5()
    {
        // Linear - only care about the direction of flow, not resolving into axes
        // Linear uses diameter of the body
        reynoldsNum_linear = rho * windVelocity_axbody.magnitude * chord_c_axbody / mu;

        // Rotational - should maybe consider doing the same as the linear flow
        // Rotational uses the circumference of the body
        reynoldsNum_x_rotational = Mathf.Abs(Mathf.PI * rho * angularVelocity_axbody.x * (chord_c_axbody / 4f) * span_a_axbody / mu);
        reynoldsNum_y_rotational = Mathf.Abs(Mathf.PI * rho * angularVelocity_axbody.y * (span_a_axbody / 4f) * span_a_axbody / mu);
        reynoldsNum_z_rotational = Mathf.Abs(Mathf.PI * rho * angularVelocity_axbody.z * (span_a_axbody / 4f) * span_a_axbody / mu);
    }

    public void GetAeroAngles_6()
    {
        // Sideslip
        sideslipRotationVector = Vector3.Cross(Vector3.forward, windVelocityAlongLiftingPlane_normalised_axbody) * Vector3.Dot(Vector3.forward, windVelocityAlongLiftingPlane_normalised_axbody);
        beta = Mathf.Atan2(windVelocity_axbody.x, windVelocity_axbody.z);
        beta_deg = Mathf.Rad2Deg * beta;
        sinBeta = Mathf.Sin(beta);
        cosBeta = Mathf.Cos(beta);

        // Angle of attack
        angleOfAttackRotationVector = Vector3.Cross(windVelocityAlongLiftingPlane_normalised_axbody, Vector3.down);

        // Include the minus sign here because alpha goes from the wind vector to the lifting plane
        alpha = -Mathf.Atan2(windVelocity_axbody.y, windVelocity_axeab_z);
        //alpha_deg = Vector3.SignedAngle(windVelocity_axbody, windVelocityAlongLiftingPlane_normalised_axbody, angleOfAttackRotationVector);
        alpha_deg = Mathf.Rad2Deg * alpha;
        sinAlpha = Mathf.Sin(alpha);
        cosAlpha = Mathf.Cos(alpha);
    }

    public void GetEquivalentAerodynamicBody_7()
    {
        // Work out shape of equivalent aero body
        // The body has the same aspect ratio and and span as the actual viewed shape but with zero sweep

        // Resolving in sideslip direction by rotating about the normal to the lift plane
        midAxis_axeab = majorAxis_axbody * midAxis_axbody / Mathf.Sqrt((midAxis_axbody * midAxis_axbody * sinBeta * sinBeta) + (majorAxis_axbody * majorAxis_axbody * cosBeta * cosBeta));
        // Find major axis based on constant area of ellipse
        majorAxis_axeab = majorAxis_axbody * midAxis_axbody / midAxis_axeab;
        // No change to thickness
        minorAxis_axeab = minorAxis_axbody;

        span_a_axeab = 2f * majorAxis_axeab;
        thickness_b_axeab = 2f * minorAxis_axeab;
        chord_c_axeab = 2f * midAxis_axeab;

        // Work out aero parameters of equivalent body
        aspectRatio_axeab = majorAxis_axeab / midAxis_axeab;
        thicknessToChordRatio_bOverc_axeab = minorAxis_axeab / midAxis_axeab;
        camberRatio_axeab = camberRatio_axbody * chord_c_axbody / chord_c_axeab;


        // Profile area is the projection in the wind direction
        // This isn't really related to the EAB but it needs to be done after wind is resolved
        profileArea = Vector3.Scale(areaVector, windVelocity_normalised_axbody).magnitude;
    }

    public void GetAerodynamicCoefficients_8()
    {
        // Prandtyl Theory
        // Clamp lower value to min AR of 2. Otherwise lift curve slope gets lower than sin 2 alpha which is non physical
        aspectRatio_clamped_axeab = Mathf.Clamp(aspectRatio_axeab / (2f + aspectRatio_axeab), 0, 1f);

        // This value needs checking for thickness to chord ratio of 1

        // Empirical correction to account for viscous effects across all thickness to chord ratios
        thicknessCorrection_axeab = Mathf.Exp(-thicknessToChordRatio_bOverc_axeab * thicknessToChordRatio_bOverc_axeab * 6f);

        // Emperical relation to allow for viscous effects
        // This could do with being in radians!
        stallAngle = stallAngleMin + (stallAngleMax - stallAngleMin) * Mathf.Exp(-aspectRatio_axbody / 2f);

        // Lifting line theory
        liftCurveSlope = 2f * Mathf.PI * aspectRatio_clamped_axeab * thicknessCorrection_axeab;

        // Zero lift angle is set based on the amount of camber. This is physics based
        alpha_0 = -camberRatio_axbody;

        // Lift before and after stall
        CL_preStall = liftCurveSlope * (alpha - alpha_0);
        CL_postStall = 0.5f * CZmax * thicknessCorrection_axeab * Mathf.Sin(2f * (alpha - alpha_0));

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
        eab_CoP_chordwise = 0.5f * midAxis_axeab * cosAlpha * cosAlpha;

        // Pitching moment because lift is applied at the centre of the body
        CM_delta = CL * eab_CoP_chordwise * cosAlpha / midAxis_axeab;
        CM = CM_0 + CM_delta;

        // Shear stress coefficients
        CD_shear_0aoa = 2f * Cf_linear;
        CD_shear_90aoa = thicknessToChordRatio_bOverc_axeab * 2f * Cf_linear;

        // Pressure coefficients
        CD_pressure_0aoa = thicknessToChordRatio_bOverc_axeab * CD_roughSphere;
        CD_pressure_90aoa = CD_normalFlatPlate - thicknessToChordRatio_bOverc_axeab * (CD_normalFlatPlate - CD_roughSphere);

        // An area correction factor is included for the pressure coefficient but is ommitted for the shear coefficient
        // This is because CD_shear_90aoa << CD_pressure_90aoa
        CD_profile = CD_shear_0aoa + thicknessToChordRatio_bOverc_axeab * CD_pressure_0aoa + (CD_shear_90aoa + CD_pressure_90aoa - CD_shear_0aoa - thicknessToChordRatio_bOverc_axeab * CD_pressure_0aoa) * sinAlpha * sinAlpha;
        CD_induced = (1f / (Mathf.PI * aspectRatio_axeab)) * CL * CL;
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
        float rotationalPressure_x = rho * chord_c_axbody * chord_c_axbody * chord_c_axbody * angularVelocitySquared_withDirection_axbody.x;
        float rotationalPressure_y = rho * span_a_axbody * span_a_axbody * span_a_axbody * angularVelocitySquared_withDirection_axbody.y;
        float rotationalPressure_z = rho * span_a_axbody * span_a_axbody * span_a_axbody * angularVelocitySquared_withDirection_axbody.z;

        // Pressure torques
        pressureTorque_axbody.x = (1f / 128f) * CD_normalFlatPlate * (areaVector.y / 2f) * rotationalPressure_x;
        pressureTorque_axbody.y = (1f / 128f) * CD_normalFlatPlate * (areaVector.z / 2f) * rotationalPressure_y;
        pressureTorque_axbody.z = (1f / 128f) * CD_normalFlatPlate * (areaVector.y / 2f) * rotationalPressure_z;

        // Shear stress torques
        shearStressTorque_axbody.x = (1f / 128f) * Cf_x_rotational * ellipsoidSurfaceArea * rotationalPressure_x;
        shearStressTorque_axbody.y = (1f / 128f) * Cf_y_rotational * ellipsoidSurfaceArea * rotationalPressure_y;
        shearStressTorque_axbody.z = (1f / 128f) * Cf_z_rotational * ellipsoidSurfaceArea * rotationalPressure_z;

        // Blending drag effects based on axis ratios
        dampingTorque_axbody.x = (1 - lambda_x) * pressureTorque_axbody.x + lambda_x * shearStressTorque_axbody.x;
        dampingTorque_axbody.y = (1 - lambda_y) * pressureTorque_axbody.y + lambda_y * shearStressTorque_axbody.y;
        dampingTorque_axbody.z = (1 - lambda_z) * pressureTorque_axbody.z + lambda_z * shearStressTorque_axbody.z;

        // Damping opposes the velocity of the object
        dampingTorque_axbody *= -1f;
    }

    public Vector3 crossResult;
    public void GetAerodynamicForces_11()
    {
        float qS = dynamicPressure * planformArea;

        // Negative inside there because angular velocity of the body is opposite to circulation
        rotationalMagnusLiftForce_axbody = -rho * Vector3.Cross(Vector3.Scale(volumeVector, angularVelocity_axbody), windVelocity_axbody);

        Vector3 liftDirection = Vector3.Cross(windVelocity_normalised_axbody, angleOfAttackRotationVector);

        liftForce_axbody = CL * qS * liftDirection;
        dragForce_axbody = CD * dynamicPressure * profileArea * windVelocity_normalised_axbody;
        crossResult = Vector3.Cross(windVelocity_normalised_axbody, windVelocityAlongLiftingPlane_normalised_axbody).normalized;
        momentDueToLift_axbody = CM * qS * chord_c_axeab * crossResult;

        resultantAerodynamicForce_axbody = liftForce_axbody + dragForce_axbody + rotationalMagnusLiftForce_axbody;
        resultantAerodynamicForce_axearth = bodyAxesTransform.TransformDirection(resultantAerodynamicForce_axbody);

        resultantAerodynamicMoment_axbody = momentDueToLift_axbody + dampingTorque_axbody;
        resultantAerodynamicMoment_axearth = bodyAxesTransform.TransformDirection(resultantAerodynamicMoment_axbody);
    }

    public void ApplyAerodynamicForces_12()
    {
        rb.AddForce(resultantAerodynamicForce_axearth);
        rb.AddTorque(resultantAerodynamicMoment_axearth);
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
        externalWind_axearth = new Vector3(0, -Mathf.Sin(alpha_rad), Mathf.Cos(alpha_rad));

        GetLocalWind_3();
        GetDynamicPressure_4();
        GetReynoldsNumber_5();
        GetAeroAngles_6();
    }

    public void SetAlpha_deg_3_to_6(float alpha_deg)
    {
        // Assumes the body has zero velocity
        float alpha_rad = Mathf.Deg2Rad * alpha_deg;
        externalWind_axearth = new Vector3(0, -Mathf.Sin(alpha_rad), Mathf.Cos(alpha_rad));

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
            rb.drag = 0;
        }


        if (!bodyAxesTransform)
        {
            GameObject go = new GameObject("Reference Axes");
            go.transform.parent = transform;
            bodyAxesTransform = go.transform;
            bodyAxesTransform.localScale = Vector3.one;
        }
        // Not sure which is better for this - we treat centre of mass and centre of geometry as the same thing...
        bodyAxesTransform.localPosition = Vector3.zero;

        GetEllipsoid_1_to_2();
    }


    void Awake()
    {
        Initialise();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (dynamicallyVariableShape)
        {
            GetEllipsoid_1_to_2();
        }
        CalculateAerodynamics_3_to_11();
        ApplyAerodynamicForces_12();
    }

    private void OnDrawGizmosSelected()
    {
        if (bodyAxesTransform)
        {
            
            // Drag - Red       Lift - Green        Wind - Blue
            Gizmos.color = Color.red;
            Vector3 dragForce_axearth = bodyAxesTransform.TransformDirection(dragForce_axbody);
            Gizmos.DrawLine(transform.position, transform.position + dragForce_axearth);
            Gizmos.color = Color.green;
            Vector3 liftForce_axearth = bodyAxesTransform.TransformDirection(liftForce_axbody);
            Gizmos.DrawLine(transform.position, transform.position + liftForce_axearth);
             
            // Wind vector
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position - windVelocity_axearth);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + resultantAerodynamicForce_axearth);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + bodyAxesTransform.TransformDirection(angleOfAttackRotationVector));

            //Gizmos.color = Color.yellow;
            //Vector3 liftRotationalWorld = referenceAxesTransform.TransformDirection(rotationalMagnusLiftForce);
            //Gizmos.DrawLine(transform.position, transform.position + liftRotationalWorld);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.Cross(windVelocityAlongLiftingPlane_axbody, Vector3.down));
        }
    }
}
