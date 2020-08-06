# E|A|S
/* 
//for further inquiries contact me: jeroen@seads.network

Loads source (file, data sheet etc) and read iterations. Each iteration contains the type of modules required.
Comparison of what is needed vs what exists in the current build.
The rule sequence (probably) depends on the module type needed as they may rely on each other. But all rule sequences probably start with the geometry rule.

Each individual rule adds or removes modules from a temporary list. This list is continously compared to the buildList.



List overview:

* templist = available spaces, reduced subsequently (never increases)
* locationlist = available geometry spaces
* buildlist = actual list of used vector 3 locations. Location entered after passing last rule script.

Rule overview:

ruleGeometry: first rule since its least restrictive. ALL locations always adhere to this rule.
ruleSpace: checks if the 'physical' location for a new module is available.
ruleAsteroidvolume: Holds values for a spherical asteroid.
ruleAsteroidsurface: Holds values for the asteroid surface but only within the scope of accesible location
ruleConesurface: Holds values for the modules on outer surface of cone. Used for shielding.

Stochasticity can be applied during two moments in cycle:
1. Go randomly through the list of given modules without giving preference to which type generates first.
2. At the very end of the processing cycle after all rules are applied and when few locations are available.  

! each rule must check if location is already in buildlist so the sequence does not go through entire rule list unnecessarily (if .. contains)

IMPORTANT: 
derive maximum amount of locations from 'Mod Radius'= asteroid radius. asteroid size function to be added
Issue: Unity adding decimals in object locations vs vector3 integer locations in lists. May be solved by using normalized locations.

!!! TO DO: verification of occupied locations

*/
