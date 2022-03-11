# Component-Based Aerodynamic Objects
Aerodynamic objects uses separate components for each part of the aerodynamics model. Some applications will not require full fidelity aerodynamics and so it would be efficient to only add the necessary components. Similarly, some components may not be suitable for the desired use if the user wants to implement their own dynamics.

Components derive from the base AerodynamicComponent class. AerodynamicComponent contains two virtual void functions:
- RunModel
- ApplyForces
Generally a component will only need to override the RunModel function to implement the computation of their resultant aerodynamic force and its point of action. However, if forces need to be applied in a different manner, then the ApplyForces function may also be overridden.

Components subscribe to two events on the AeroBody:
- runModelEvent
- applyForcesEvent
The computation of the forces and moments for each aerodynamic component is done separately to the application of the forces - this allows for validation tests to be performed without the need to apply the forces to the rigid body afterwards.
