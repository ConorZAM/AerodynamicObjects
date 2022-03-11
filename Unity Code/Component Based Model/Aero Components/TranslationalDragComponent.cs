using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslationalDragComponent : AerodynamicComponent
{
    // Drag
    public float CD;                                    // (dimensionless)
    public float CD_profile;          // (dimensionless)
    public float CD_pressure_0aoa, CD_pressure_90aoa;   // (dimensionless)
    public float CD_shear_0aoa, CD_shear_90aoa;         // (dimensionless)
    public float CD_normalFlatPlate = 1.2f;             // (dimensionless)
    public float CD_roughSphere = 0.5f;                 // (dimensionless)
    public float reynoldsNum_linear, Cf_linear;         // (dimensionless)

    public override void RunModel(AeroBody aeroBody)
    {
        // Linear - only care about the direction of flow, not resolving into axes
        // Bill says that really we should be looking at linear reynolds number in each axis separately
        // Linear uses diameter of the body - note we use the EAB chord as wind is resolved along this direction
        reynoldsNum_linear = aeroBody.rho * aeroBody.aeroBodyFrame.windVelocity.magnitude * aeroBody.EAB.chord_c / aeroBody.mu;

        // Shear coefficient
        Cf_linear = reynoldsNum_linear == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_linear, 1f / 7f);

        // Shear stress coefficients
        CD_shear_0aoa = 2f * Cf_linear;
        CD_shear_90aoa = aeroBody.EAB.thicknessToChordRatio_bOverc * 2f * Cf_linear;

        // Pressure coefficients
        CD_pressure_0aoa = aeroBody.EAB.thicknessToChordRatio_bOverc * CD_roughSphere;
        CD_pressure_90aoa = CD_normalFlatPlate - aeroBody.EAB.thicknessToChordRatio_bOverc * (CD_normalFlatPlate - CD_roughSphere);

        // An area correction factor is included for the pressure coefficient but is ommitted for the shear coefficient
        // This is because CD_shear_90aoa << CD_pressure_90aoa
        CD_profile = CD_shear_0aoa + aeroBody.EAB.thicknessToChordRatio_bOverc * CD_pressure_0aoa + (CD_shear_90aoa + CD_pressure_90aoa - CD_shear_0aoa - aeroBody.EAB.thicknessToChordRatio_bOverc * CD_pressure_0aoa) * aeroBody.sinAlpha * aeroBody.sinAlpha;
        CD = CD_profile;

        resultantForce_bodyFrame = -CD * aeroBody.dynamicPressure * aeroBody.profileArea * aeroBody.aeroBodyFrame.windVelocity_normalised;
        resultantMoment_bodyFrame = Vector3.zero;

        resultantForce_earthFrame = aeroBody.TransformDirectionBodyToEarth(resultantForce_bodyFrame);
        forcePointOfAction_earthFrame = aeroBody.transform.position;
    }
}
