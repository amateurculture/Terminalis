Welcome to Gravity Engine.

The recommended starting sequence is: <LINK>


On-line documentation/tutorial videos:
http://nbodyphysics.com/blog/gravity-engine-doc-1-3-2/

Docs for script elements:
http://nbodyphysics.com/gravityengine/html/

Support: nbodyphysics@gmail.com

Gravity Engine 4.0
==================

Enhancements:
-------------

1) ManualShipControl object for click-to-drag velocity/orbit changes

2) OrbitPoint to place an object at the specified location on an orbit. 

3) Add a mechanism to register callbacks for components that need to run code once when GE has started
   ge.AddGEStartCallback()

4) TransferShip to perform a general any orbit to any orbit transfer or rendezvous.

5) HohmannGeneral class to provide a single class for any type of Hohmann xfer or rendezvous (including phasing orbit
   determination)

6) CreateTransferWithPhasing method for LambertUniversal to allow rdvs/intercept computation.

7) Ground track on a rotating planet. 

Bugs:
-----
1) ShipOnOffRails. If started with ship off rails bug with GE OffRails not checking body was already off rails. GE became confused. 

2) Fix logic in GravityState UpdateOnRails() and fixed body counting.

3) Change PatchedConicSOI so there is no entry de-rate. 

4) Ensure OrbitData SetOrbit() checks for KeplerSequence (Fix from Petri)

5) Ensure rotation is always inited in OrbitUniversal init.

6) GE.SetPositionDoubleV3 now updates game objects if GE is paused. 

7) Support GE reposition in:
		LambertDemoController, LambertPointController, HohmannController, CircXferTester,
        CircNonPlanarRdvsTester and OrbitSegment 
        (Changes in controllers, OrbitUiniversal and ManeuverRenderer). Correct sign error in GE.UnmapFromScene()

8) Re-computes per frame is actually a re-compute factor. When too big it results in trajectories longer than 
   specified time. Add a Max check. 

9) Fix trajectory update jitter by moving Trajectory to Update() [from fixed update].

10) OrbitUniversal init in Kepler mode does a first evolve to setup pos and vel. 

11) InitFromCoe in OrbitUniversal also does an init of vel. 

12) LambertDemoController: Defined too many ellipses in array (needed to exclude spaceship ellipse). Solves null ptr
    issue for certain selection sequences. 

Refactor:
---------
1) GE.GetVelocity -> use gravity state

2) Change ge.AddBody to allow adding a body when GE is paused. 

3) Remove Nutils.AngleFromSinCos. 

4) Remove frame1hack from FreeReturnGeneric and use a GEStart callback.

Gravity Engine 3.2 (Nov 2019)
================================

1) Add ability to set position on a fixed object. (Helpful for NBody ships attached to an OrbitPoint)

2) Add VelocityForPhase() to OrbitUniversal.

Development Impacting Changes:
==============================
1) OrbitPredictors that are setup via scripting will now require that SetNbody() be called as part of their setup. See
   examples in AddDeleteTester and RandomPlanets sample.

Enhancements:
-------------
1) Add example of time of flight to peri/apo. Add slight tweak to OrbitU timeOfFlight to return positive numbers for elliptical orbits. 
- ensure OrbitData is updated each frame (even though in this case it is not strictly required)

2) Add BodyOnRails() to convert/return a body to on-rails operation. (See Scenes/Demos/ShipOnOffRails and ShipOnOffRailsController).

3) Move to Unity 2018.3 for reference build

4) Improve editor display for KeplerSequence and OrbitU when scene running. (The OrbitU shown is the
  first one in the sequence and this can be a bit confusing.)

5) Add settime command to GEConsole. 

6) Refactor OrbitPredictor to use OrbitUniversal internally. Stop using OrbitData. Orbit params can be retrieved by using
   OrbitPredictor.GetOrbitUniversal()

7) Refactor OrbitSegment as per 6.

8) Add a field to OrbitPredictor: hyperDisplayRadius to allow control over how much of hyper orbit is shows. (Typical use
  as per FreeReturn/FreeReturnGeneric where moon SOI is used.)

9) Add simple inclination projection for OrbitPredictor (assumes projection plane through origin)

10) Add circular non-coplanar rendezvous, with intermediate orbit for phasing. See the scene CircNonPlanarRendezvous. Currently
    works for destination orbit outside initial orbit only.

11) If there is a KeplerSequence with an OrbitU that is off-rails (contradiction) then resolve in 
    favour of the off-rails and log an error to alert the developer. 

12) Add code to AddDeleteTester to support addition of dust balls.

13) Add OrbitUniversal.GetPositionForRadius()

14) Implement LambertBattin. This is a Lambert transfer that allows the points r1 and r2 to be 180 degrees apart (LambertUniversal 
    reports an error for this case). Created some simple unit tests to check LU vs LB for basic sanity. Use this in FreeReturnGeneric scene.

15) Create a FreeReturnGeneric scene. This is an on-rails scene that allows a user to explore free return trajectories. 
	Scenes/MiniGames/Scenes/FreeReturn/FRGeneric and FRGenericOrbitalUnits

16) Add dust ball to the AddDeleteTester scene

Bugs:
-----

1) Fix CalcTau() in OrbitData. Needed to use scaled period and (Horrifyingly!) fix error in the equation. 

2) Fix initial values in free return on rails scene.

3) Fix OneStageEngine.GetThrottlePercent()

4) Update "fixedBodiesInIntegrator" when remove a fixed body.

5) Bug with display of scaled p in inspector. Bug was in value of p used in OrbitU. Fixed in ApplyScale()

6) [Petri] Jump in position/velocity as go on rails

7) Fix bug in InitFromRVT - was not doing relative position correctly (updated r not r0). 

8) KeplerSequence needs to adjust Kepler depth when orbit changes to new center object. So does OrbitU.SetNewCenter()

9) Free return On rails. Issue with orbit predictor center not changing when reverse back in time from exit SOI to earlier. 

10) Fix OmegaU in OrbitUniversal RVtoCOE when circular inclined. 

11) Fix bug on Orbit Predictor on free fall path.

12) AddDeleteTester needed some changes due to the fact OrbitPredictor now uses OrbitU.

13) RandomPlanets needed orbit predictor SetNBody() fix.

14) Ensure FreeReturn picks a TLI burn going in the same direction as the ship. 

15) Fix manuever time precision in GravityState:Evolve()

16) Fix orbit segment target in LambertToPointController.

17) Ensure OrbitSegment uses destination game object if there is one. 

18) Correct moon mass (was 10x too small) in EarthMoonXfer, FreeReturnMoon scenes.

Testing
-------
1) Check if OrbitU p update breaks anything with orbit setup. 

2) Slight glitch sometimes in ShipOnOffRails?? 



Gravity Engine 3.1 (Aug 2019)
=============================

Features:
1) Add DOUBLE_ELLIPSE to allow double precision entry of ellipse semi-major axis and angles. 

2) Add KeplerSequence: RemoveManeuvers(), RemoveFutureSegements() and RemoveSegementsAfterTime()

3) Add circularize and raise orbit to the FreeReturn family of scenes. 

4) Allow OrbitData to init from an OrbitUniversal.

5) Add KeplerSequence debug info to editor script and GEconsole. 

6) Implement GetPeriod(), GetPerigee() in OrbitUniversal.

7) Add Docking mini-game

8) Add CircInclAndAN scene and a transfer CircularizeInclinationAndAN class to do a plane change/ascending node transfer for same
size circular orbits. 

BUGS:
1) Fix on-rails Hohmann xfer from outer to inner

2) Add a function to KeplerSequence GetCurrentOrbit() so current orbit params can be retreived. 

3) Ensure ApplyImpulse works on an object with a KeplerSequence (will not be time-reversible)

4) Fix ApplyScale and OrbitUniversal

5) {TODO} Fix NBody not intialized warning using OrbitEllipse gizmo in Editor. 

6) FreeReturn not showing out-bound ellipse correctly. Update TOF parameter. Still need to fix issue
   with showing full length of oubound hyperbola.

7) Fix typo in GE editor (Trajectory Prediction)

8) Allow OrbitRenderer to support OrbitUniversal.

9) Fix issue with orbit predictor for omega_uc when circular inclined. (OrbitData.RVtoCOEwrapper)


Changes:
1) Change FreeReturnController to always use 180 degree phase for departure point.

2) Remove redundant update positions for particles and massless objects

3) Refactor internally to keep array of gameNBodies and not gameObjects. Removes a bunch of pointless GetComponent<> calls. 

4) Add Mathd.Clamp() inside Acos and Asin in OrbitUtils to prevent numerical glitches from causing NaN. Unit tests ok. 

Gravity Engine 3.0 (June 2019)
==============================

A big thanks to the recent beta testers: Christian Rosick, Christophe Canon, John Watson, Vitaliy Zaverchuk 
(and others who I failed to find in my email search). 

Features:
1) "On-Rails"/KeplerSequence
- set time for scene (Demos/SolarSystemInner-SetTime) to jump time forward backward

2) OrbitUniversal
- a generic double precision orbit compnent for elliptical, parabolic and hyperbolic orbits
- allow impulse when in Kepler mode
- additional options to specify an orbit
- use in FreeReturn with SOI handoff
- use in free return with KeplerSequence (FreeReturnOnRails)
[Makes use of algortihms from Vallado, Fundamentals of Astrodynamics and Applicatons. By far my most-used
 book about orbital mechanics!]

3) Launch Window computation (Hohmann transfer LaunchTimes() and Demos/Scenes/SolarSystem/SolarSystemLaunchWindow)

4) Time between points in orbit (OrbitUtils.TimeOfFlight(), OrbitUniversal.TimeOfFlight() and Demos/TimeOfFlight)

5) Add calculation of maneuver positions to HohmannXfer. (Demos/OrbitSamples/HohmannXfer)

Refactoring:
------------

1) Change IPatchedConicChange to pass NBody (not game object). Correspondng changes in classes using the interface.

2) Rename TransferCalcController to FreeReturnController.

3) Allow FreeReturnController to also start with an OrbitUniversal on spaceship. 

4) Move more gravity state add/delete code into GravityState (from GE).

5) Fix some of the internal naming (EvolveToTime) in GE to be correct (actually was evolving by Dt).

6) Put FindC2C3 in OrbitUtils.

7) Rename IFixedOrbit.IsFixed() to IsOnRails()

8) OrbitPredictor onEnable/onDisable now controls line renderer

9) OrbitData period is now in phys time.

10) Add a warning on NBodyCollision if there is a NBody on the same object. (NBodyCollision should be on
    a child of an NBody).

11) Change PatchedConicSOI to use FixedUpdate directly. Remove book-keeping from GE.

12) Expose the ecc_vec from OrbitData to allow determination of phase from UI input (as angle to the ecc_vec).

13) Re-arrange nesting of scenes in Demos and Mini-games

BUG FIXES:
----------

1) Orbit ellipse was using GetScenePosition() for center body offset. Corrected to GetPhysicsPosition()

2) Fixed AddDeleteTester to not try and remove the parent object twice for massive bodies. (Fixes a warning)

3) Fix OrbitData omega_angular definition. (Equation was reciprocal!)

4) Fix book-keeping with FixedBody indexes. On delete they were not updated to new index values. Refactor 
   index keeping out in favour of an NBofy ref. [Credit to John Chen for finding the bug.]

5) KeplerMode with trajectory enabled caused weird jumping. Fix in GE.

6) Ensure moon destroy when parent deleted in AddDeleteTest. 

7) CircularizeXfer: Bug using wrong velocity incorss product (resulting in switch in direction around the
   circle for some positions in the orbit)

8) Fix retrograde Hohmann transfers. (Removed old Ellipse determination code in OrbitData in favour of
RVtoCOE in OrbitUtils.)

9) TransferCalc (used in PlanetMoonXfer and EarthMoonTransfer) use OrbitData SetOrbitForVelocity and not
   SetOrbit. Fixes bug in rendezvous to inner orbit. 

10) OrbitMGController: Fix controller state bug in trajectory prediction toggle on second transfer.


Gravity Engine 2.3
==================

Features:
---------
1) OrbitHyperSegment: display a symmetric hyperbola segment (used in free return visualization)

2) Add StartInSingleStep mode to GEConsole.

3) OrbitHyper has support for Kepler mode.

4) Add FreeReturn calculations.

5) Add GEExternalAcceleration and hook into all integrators to allow atmosphere models (+ existing rocket engines)
to apply external forces to an NBody.
- this resulted in a change to rocket engines to return the current inertial mass in the acceleration calculation

6) Create EarthAtmosphere to model drag affects as a function of height and speed.

7) Add EarthRocket to hold rocket and atmosphere acceleration. 

8) Modify LaunchToOrbit scene so dropped stages now encounter air resistance. 

9) Changed RocketEngine base class
- added throttle engine control
- changed implementing classes (MultiStageRocket) to track empty fuel since can no longer assume 100% thrust
- removed the time parameter from the GetFuel() method
- corresponding changes in LaunchUI and ShipInfo scripts to support mini-game scene

10) LaunchController
- class that applies pitch and throttle settings from curves in inspector 
- allows reproducible launch to orbit and tuning of ascent path and final orbit


Bugs:
-----
1) Objects added when paused would not be added on a resume. 

2) ApplyImpulse when OptimizeMassless=false resulted in a divide by zero. 

3) Bug fix in OrbitRenderer. Move center body to after OrbitHyper null check.

4) Fix issues with trajectory predicition when using the Hemite integrator.

5) Ensure Inactivate/Activate applies to trajectory state when present. 

6) Fix NBodyEditor to move NBody when the initialPhysPosition is changed in the inspector. 



Gravity Engine 2.2 (Nov 6, 2018)
================================

Features:
1) Add code for Lambert transfer to a point. 

2) Add a ManeuverRenderer to see velocity changes for Lambert maneuvers. 

3) Add OrbitSegment renderer to show which way a Lambert transfer will go (short/long)

4) Add GetMass() function to retrieve internal physics engine mass. 

5) Convert PatchedConicXfer internals to double

Bugs:
1) Fix FixedObject to take position from transform when units = DIMENSIONLESS

2) Position/velocity precalc when parent object has a Kepler OrbitEllipse when application is playing. 

3) Implement deltaV for LambertMinEnergy and LambertUniversal.

4) Remove LambertMinEnergy, since LambertUniversal does this as first pass and this removes duplicate code. 
(LambertMinEnergy had a bug and it did not make sense to fix it.)

5) Issue #23 (Exception on Kepler object on start). Breakage due to (2) but also likely existed in some
cases before this. Depended on order of object insertion. 

6) Correct gizmo display of hill sphere in editor view when in scaled units. 
[Issue #24]

7) Fix loop convergence check in LambertUniversal code.


Gravity Engine 2.1 Sept. 11,2018
================================

New:
1) Add support for a selective (per body) custom force.

2) Add a full solar system scene. (My solar system includes Pluto!)


Bug Fixes:
1) Allow editor float field drag by removing DelayedFloatField from: 
	EllipseBaseEditor
	NBodyEditor
	RandomPlanetEditor
	SolarBodyEditor
	SolarSystemEditor

2) Fix delete of binary planets and moons in AddDeleteTester. Add a Chaos Monkey checkbox for random testing. 

3) Fix adjustment to eccentricity in SolarSystemBody (could not be changed in the editor)

4) Modify LambertDemoController to ensure key presses are limited to scene states that are relevent. 

5) Fix outer to inner Lambert transfer. Sone issues remain

6) Adjust parameters in the Tutorial 6. 
- reduce thrust
- uncheck align motion with trajectory

7) Fix collision detection bug. 

Known Issues:
=============
1) Lambert Xfers
Outer to inner transfers sometimes fail to match the inner orbit
In some cases the selected direction on the transfer ellipse is not optimal (wrong direction)

2) AddDeleteTester
When the "Run Chaos Monkey" is checked get sporadic AABB and isFinite errors in short bursts. 
This is related to the OrbitPredictors and is under investigation.  


Gravity Engine 2.0
August 12, 2018
================

Lambert Orbital Transfers 
Lunar Course Corrections
GEConsole command additions
Add massless option to FrameRate tester
Support Gravity State evolution on an independent thread

Fixes:
- hyperbola course predition fixes
- ordering of updates for nested Kepler objects



On-line documentation/tutorial videos:
http://nbodyphysics.com/blog/gravity-engine-doc-1-3-2/

Docs for script elements:
http://nbodyphysics.com/gravityengine/html/

Support: nbodyphysics@gmail.com

