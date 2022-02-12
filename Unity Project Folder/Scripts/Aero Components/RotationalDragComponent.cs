using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationalDragComponent : AerodynamicComponent
{
    // Drag due to the angular velocity of the body
    public Vector3 dampingTorque_bodyFrame, pressureTorque_bodyFrame, shearStressTorque_bodyFrame, momentDueToLift_bodyFrame;   // (Nm)
    public float CD_normalFlatPlate = 1.2f;             // (dimensionless)
    public float reynoldsNum_x_rotational, reynoldsNum_y_rotational, reynoldsNum_z_rotational; // (dimensionless)
    public float Cf_x_rotational, Cf_y_rotational, Cf_z_rotational;                            // (dimensionless)
    public Vector3 directionalAngularVelocitySquared_bodyFrame;
    public float rotationalPressure_x, rotationalPressure_y, rotationalPressure_z;

    public override void RunModel(AeroBody aeroBody)
    {

        // Rotational uses the circumference of the body
        reynoldsNum_x_rotational = Mathf.Abs(Mathf.PI * aeroBody.rho * aeroBody.aeroBodyFrame.angularWindVelocity.x * aeroBody.aeroBody.midAxis * aeroBody.aeroBody.majorAxis / aeroBody.mu);
        reynoldsNum_y_rotational = Mathf.Abs(Mathf.PI * aeroBody.rho * aeroBody.aeroBodyFrame.angularWindVelocity.y * aeroBody.aeroBody.majorAxis * aeroBody.aeroBody.majorAxis / aeroBody.mu);
        reynoldsNum_z_rotational = Mathf.Abs(Mathf.PI * aeroBody.rho * aeroBody.aeroBodyFrame.angularWindVelocity.z * aeroBody.aeroBody.majorAxis * aeroBody.aeroBody.majorAxis / aeroBody.mu);

        // Shear coefficient
        Cf_x_rotational = reynoldsNum_x_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_x_rotational, 1f / 7f);
        Cf_y_rotational = reynoldsNum_y_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_y_rotational, 1f / 7f);
        Cf_z_rotational = reynoldsNum_z_rotational == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNum_z_rotational, 1f / 7f);


        // Scale by the direction here to preserve the signs of the angular velocity
        directionalAngularVelocitySquared_bodyFrame = Vector3.Scale(Vector3.Scale(aeroBody.aeroBodyFrame.angularWindVelocity, aeroBody.aeroBodyFrame.angularWindVelocity), aeroBody.aeroBodyFrame.angularWindVelocity_normalised);

        rotationalPressure_x = 0.5f * aeroBody.rho * aeroBody.aeroBody.midAxis * aeroBody.aeroBody.midAxis * aeroBody.aeroBody.midAxis * directionalAngularVelocitySquared_bodyFrame.x;
        rotationalPressure_y = 0.5f * aeroBody.rho * aeroBody.aeroBody.majorAxis * aeroBody.aeroBody.majorAxis * aeroBody.aeroBody.majorAxis * directionalAngularVelocitySquared_bodyFrame.y;
        rotationalPressure_z = 0.5f * aeroBody.rho * aeroBody.aeroBody.majorAxis * aeroBody.aeroBody.majorAxis * aeroBody.aeroBody.majorAxis * directionalAngularVelocitySquared_bodyFrame.z;

        // Pressure torques
        pressureTorque_bodyFrame.x = CD_normalFlatPlate * (aeroBody.areaVector.y / 2f) * rotationalPressure_x;
        pressureTorque_bodyFrame.y = CD_normalFlatPlate * (aeroBody.areaVector.z / 2f) * rotationalPressure_y;
        pressureTorque_bodyFrame.z = CD_normalFlatPlate * (aeroBody.areaVector.y / 2f) * rotationalPressure_z;

        // Shear stress torques
        shearStressTorque_bodyFrame.x = Cf_x_rotational * aeroBody.ellipsoidSurfaceArea * rotationalPressure_x;
        shearStressTorque_bodyFrame.y = Cf_y_rotational * aeroBody.ellipsoidSurfaceArea * rotationalPressure_y;
        shearStressTorque_bodyFrame.z = Cf_z_rotational * aeroBody.ellipsoidSurfaceArea * rotationalPressure_z;

        // Blending drag effects based on axis ratios
        dampingTorque_bodyFrame.x = (1 - aeroBody.lambda_x) * pressureTorque_bodyFrame.x + aeroBody.lambda_x * shearStressTorque_bodyFrame.x;
        dampingTorque_bodyFrame.y = (1 - aeroBody.lambda_y) * pressureTorque_bodyFrame.y + aeroBody.lambda_y * shearStressTorque_bodyFrame.y;
        dampingTorque_bodyFrame.z = (1 - aeroBody.lambda_z) * pressureTorque_bodyFrame.z + aeroBody.lambda_z * shearStressTorque_bodyFrame.z;

        // Compute the resulting force and moment
        resultantForce_bodyFrame = Vector3.zero;
        resultantMoment_bodyFrame = -dampingTorque_bodyFrame;
    }
}
