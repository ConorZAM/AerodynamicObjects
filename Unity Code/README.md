# Unity Aerodynamics Model

The Unity aerodynamics model currently has all variables and functions set to public so that validation experiments may probe the model easily.

The model is split into functions which are ordered 1-12 in the order which they should be called. This is because some functions compute values used by later functions.
The functions are:
1. **GetBodyDimensions** - Uses the scale of the attached transform object to determine span, thickness and chord of the ellipsoid. Then creates a transform object which aligns (x, y, z) with (span, thickness, chord).
2. **GetEllipsoidProperties** - Computes wing properties such as AR and thickness to chord ratio. Also computes the surface area, volume and inertia of the ellipsoid body.
3. **GetLocalWind** - Uses the velocity of the RigidBody component and an "externalWind" vector to determine the wind in local axes, relative to (span, thickness, chord).
4. **GetDynamicPressure** - Computes 0.5 * rho * v^2
5. **GetReynoldsNumber** - Computes the Reynolds number for the body for linear and rotational flow separately.
6. **GetAeroAngles** - Computes the angle of attack and angle of sideslip due to the local wind. Also gets the rotation vectors for the two angles.
7. **GetEquivalentAerodynamicBody** - Resolves the ellipsoid axes into the sideslip, thus removing it from consideration in further calculations.
8. **GetAerodynamicCoefficients** - Computes CL, CM and CD for the body based on angle of attack and the resolved dimensions
9. **GetShearStressCoefficients** - Computes Cf for both linear and rotational flow
10. **GetDampingTorques** - Computes the shear and pressure damping torques
11. **GetAerodynamicForces** - Computes lift, magnus lift, drag and moment due to lift and camber
12. **ApplyAerodynamicForces** - Applies all aerodynamic forces and moments to the attached RigidBody component
