﻿/* 
//for further inquiries contact me: jeroen@seads.network
Loads source (file, data sheet etc) and read iterations. Each iteration contains the type of modules required.
Comparison of what is needed vs what exists in the current build.
The rule sequence (probably) depends on the module type needed. But all rule sequences probably start with the geometry rule.

Each individual rule adds or removes modules from a temporary list. This list is continously compared to the buildList.
tempList may be removed later in development so that the buildList will be used to compare.

note: Vector3 is a Unity type holding values for (X,Y,Z)

List overview:

* locationlist = A permanent list of available location within geometry space. Loaded from 'Locations' script during Start.
* templist = A short-lived memorization of available spaces used to pass information to a relaylist and purged immediately after.
* relayList = Passing eligible locations from one rule to the other. Within a rule method, it is generally flushed and repopulation by the templist.
* buildlist = actual list of used vector 3 locations +  meta descriptions (step, module type). The buildList is using a custom datatype {(x,y,z),step,modType} called 'moduleClass'.

Rule overview:

ruleGeometry: first rule since its least restrictive and enables first population of the relayList. ALL locations always adhere to this constraint.
ruleSpace: checks if the 'physical' location for a new module is available.
ruleAsteroidvolume: Holds location values (vector3) within a spherical asteroid form.
ruleAsteroidsurface: Holds values for the asteroid surface but only within the scope of accesible location
ruleConesurface: Holds values for the modules on outer surface of cone. Used for shielding.

Stochasticity can be applied during two moments in cycle:
1. Go randomly through the list of given modules without giving preference to which type generates first.
2. At the very end of the processing cycle after all rules are applied and when few locations are available.  

! each rule must check if location is already in buildlist so the sequence does not go through entire rule list unnecessarily (if .. contains)


BuildList data : List<Vector3,Vector3>\
<Location,( step,mod type ,--  )>
step= 'step' from data
type= 'mod type' of module, found in enum modType

IMPORTANT: 
derive maximum amount of locations from 'Mod Radius'= asteroid radius. asteroid size function to be added
Issue: Unity adding decimals in object locations vs vector3 integer locations in lists. May be solved by using normalized locations.

!!! TO DO: verification

!! COMPARE RELAY LISTS, not other

*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
//using moduleClass;

public class mods : MonoBehaviour
{

    private int modRadius;
    public float unitSize;// ONLY used for instantiation!
    public Vector3 checkValue;

    public GameObject instantiatedMod1;// 'EVEN' layer
    public GameObject mod0;// more variations to add later
    public GameObject mod2;// etc.


    // lists

    public List<Vector3> locationList;// list of all mod locations within geometry, loaded at Start
    public List<GameObject> modObjects = new List<GameObject>();// needed for object reference (destroy)
    public List<Vector3> asteroidVolume = new List<Vector3>();// all locations within asteroid geometry only
    public List<Vector3> asteroidSurface = new List<Vector3>();// locations on surface of asteroid only
    //public List<Vector3> buildList = new List<Vector3>();// OBSOLETE: to removed
    public List<moduleClass> BuildList = new List<moduleClass>();// contains adresses/locations of actual mods, read/write. to be replaced by moduleclass


    //var buildList = new List<moduleClass>();// to replace buildlist


    List<Vector3> relayList = new List<Vector3>();// Pass on available locations between rules, while going through sequence of rules. Cleared and repopulated inside single rule application, passing info to next rule.
    List<Vector3> tempList = new List<Vector3>();//  Short-lived list used within single rule. Copy locations from rule after AND-AND operation, paste in relayList.Cleared between rules, so does not pass info to next rule.
    //List<GameObject> tempObj = new List<GameObject>();//

    List<int> tempData = new List<int>();// contains one row of data from source. Mockdata or actual data.

    // rules
    bool space;// must be true to pass
    bool geometry;// must be true to pass
    bool encapsulated;//must be false to pass

    // objects
    Vector3 origin = new Vector3(0, 0, 0);// serves as a reference. Might be removed later.
    private GameObject[] delObj;// enables as a reference needed to destroy objects

    int step = 10;
    int counter = 0;//
    int selection;// used to select index from list
    string type;// used to pass module type name to build list.

    enum modType { generic, mining, oreStorage, processing, refinedStorage, printingBot, manufacturing, equipStorage, assemblyBot, habitat, lifeSupport };
    //modType modSelection;

    int modSelection;
    int modEnum = System.Enum.GetValues(typeof(modType)).Length;
    // rule sequences
    //enum ruleMining { loadGeometry, asteroidVol };// methods in enum example

    private int data_Generic, data_Mining, data_OreStorage, data_Processing, data_RefinedStorage, data_PrintingBot, data_Manufacturing, data_EquipStorage, data_AssemblyBot, data_Habitat, data_LifeSupport;


    void Start()
    {


        // get variables from 'locations' script
        locations getVariable = GetComponent<locations>();
        // unitSize = getVariable.unitSize;// get unitSize from 'locations' script

        modRadius = getVariable.modRadius;// get modRadius from 'locations' script

        var buildList = new List<moduleClass>();// to replace buildlist

        // get location geometry from 'locations' script
        locationList = new List<Vector3>(GetComponent<locations>().modGeometry);
        // List<Vector3> getLocation = GetComponent<locations>().modGeometry;// alternative
        // locationList = GetComponent<locations>().modGeometry;//alternative

        // yield return new WaitForEndOfFrame();// alternative for loading mods.cs after location.cs. Locations script runs first (via settings)

        loadGeometry();// // loads geometry list from  'Locations' script. AND loads location in relayList as least restrictive location availability
        asteroidVol();// create asteroid volume locations. Static for now. Remove available locations when excavating.
        asteroidsurface();// create asteroid surface locations. Static for now. Remove available locations when excavating.

        mockdata();// Loads single row of provided mockdata,  step 10
                   //seedMods();
                   // instantiateGlobalvolume();// only for testing

        Debug.Log("  START: locationList.Count:" + locationList.Count);
        Debug.Log("  START: asteroidVolume.Count:" + asteroidVolume.Count);
        Debug.Log("  START: asteroidSurface.Count:" + asteroidSurface.Count);
        Debug.Log("  START: relayList.Count:" + relayList.Count);
        Debug.Log("  START: tempData.Count:" + tempData.Count);
        Debug.Log("  START: unitSize:" + unitSize);

        tempList.Clear();
        //load 
        processing();
        instantiateBuildlist();// all (BuildList) mods instantiation. Must be called at the end of processing only ! (will instantiate if placed cases)
        // another way of single mod instantiation

    }

    // Update is called once per frame
    void Update()
    {
        getKey();
    }



    void processing()
    {
        // first pass has mining priority for seed islands
        //loop through data row (= one step in mockdata)
        //Each element will go through cases and rules
        //within data row order matters but for now loop through column, one type at the time until row is done
        // tempData = temporary datalist
        //{ generic, mining, oreStorage, processing, refinedStorage, printingBot, manufacturing, equipStorage, assemblyBot, habitat, lifeSupport };

        step = 10;// only fixed value for using one row of mockdata
        int maxValue = tempData.Max();
        //Debug.Log("maxValue : " + maxValue);
        int dataCount = tempData.Count;
        int sum = tempData.Sum();
        //Debug.Log("sum :" + sum);


        //Loop entire tempData list (maxValue) times so that each element can be reduced to 0. 
        for (int l = 0; l < (maxValue); l++)
        {
            for (int caseLoop = 0; caseLoop < dataCount; caseLoop++)//loop through tempData (11 elements in Mockdata)
            {
                int element = tempData[caseLoop];

                if (element > 0)// value not further reduced below 0
                {
                    modSelection = caseLoop;
                    cases();
                    // relayList.Clear();
                    tempData[caseLoop] = tempData[caseLoop] - 1;// reduce each element in list to 0
                }
                // first check if already in buildList!
            }

            // check tempData list
            // for (int check = 0; check < tempData.Count; check++)//loop through tempData, take one element of each type
            // {
            //     Debug.Log("tempData[check] : " + tempData[check]);
            // }
        }

    }

    void cases()
    {

        switch (modSelection)
        {

            /*case 0:
                modType test = modType.generic;
                // case modType.generic:
                // Debug.Log("modType.generic");
                // break;
                break;
                */

            case 1:
                type = "m.mining";
                Debug.Log(type);
                ruleGeometry();
                ruleAsteroidsurface();
                //  instantiateOnemod();
                addtoBuilt(step, type);
                break;

            case 2:
                type = "m.oreStorage";
                Debug.Log(type);

                ruleGeometry();

                foreach (moduleClass tempvar in BuildList) if (tempvar.modType == "m.mining")
                    {

                        foreach (Vector3 tempv3 in relayList)
                        {
                            float distance = (Vector3.Distance(tempv3, tempvar.posv3));


                            //  if (tempList.Contains(tempvar.posv3)  == false)
                            //   {
                            if ((distance > 0 && distance <= 1))
                            {
                                Debug.Log("===================>" + distance);

                                tempList.Add(tempv3);

                                // instantiatedMod1 = Instantiate(mod1, new Vector3(tempv3.x * unitSize, tempv3.y * unitSize, tempv3.z * unitSize), Quaternion.identity);

                            }
                            //   }
                        }
                    }
                purgeandReload();

                addtoBuilt(step, type);

                break;

            case 3:

                Debug.Log(" processing ");
                break;

            case 4:

                Debug.Log("refinedStorage ");
                break;

            case 5:

                Debug.Log("printingBot ");
                break;

            case 6:

                Debug.Log(" manufacturing ");
                break;

            case 7:

                Debug.Log(" equipStorage");
                break;

            case 8:

                Debug.Log("assemblyBot ");
                break;

            case 9:
                ruleGeometry();

                Debug.Log("habitat ");
                break;

            case 10:

                Debug.Log("lifeSupport ");
                break;

            case 11:

                Debug.Log(" ");
                break;


            default:
                // no mods selected
                break;
        }
    }


    // RULE METHODS #####################################################################################################################################################

    // random selection in RelayList
    void randomSelection()
    {

        // //repeat count in relaylist
        // int relayCount = relayList.Count;

        // for (int i = 0; i < relayCount; i++)
        // {
        //     Vector3 randomLocation = relayList[Random.Range(0, relayList.Count)];

        //     // check if in buildlist
        //     foreach (Vector3 tempv3 in buildList)
        //     {
        //         if (tempv3 == randomLocation)//
        //         {
        //             relayList.Remove(tempv3);// and find another available location, shrinks relay list
        //         }
        //         else
        //         {
        //             buildList.Add(tempv3);
        //             relayList.Clear();
        //             break;// return
        //         }
        //     }
        // }
        // Debug.Log("No module location was available");

    }

    void ruleGeometry()// geometry is least restrictive, so it populates the relaylist first (it doesn't alway need to but is a practical way)
    {

        foreach (Vector3 tempv3 in locationList)
        {
            relayList.Add(tempv3);//    
        }

    }

    void ruleAsteroidvolume()
    {


    }


    // creates list of addresses on surface of spherical asteroid within distance d of origin
    // if surface is static, better to load during start()
    void ruleAsteroidsurface()
    {
        //     asteroidSurface  relayList

        foreach (Vector3 tempv3 in relayList)
        {
            // tempv3 is in asteroidSurface
            if (asteroidSurface.Contains(tempv3))
            {
                tempList.Add(tempv3);//    ITS EMPTY
            }
        }

        purgeandReload();
    }

    // void ruleSpace()// If 0, must return. Combine with random assignment. Rule to check if found locations are already occupied or not, checked with -or between- every rule
    // {
    //     foreach (Vector3 tempv3 in buildList)
    //     {
    //         if (relayList.Contains(tempv3))//buildlist and relay are mutualy exclusive. No vector3 can exist in both
    //         {
    //             relayList.Remove(tempv3);
    //             // and find another available location

    //         }// also check if no space available restart or end

    //     }
    //     tempList.Clear();
    // }


    // MODELLING METHODS: cone geometry, asteroid volume, asteroid surface, ########################################################################################

    // loads geometry list from  'Locations' script. AND loads location in relayList as least restrictive location. 
    void loadGeometry()
    {
        // locationList = GetComponent<locations>().modGeometry;//both work
        locationList = new List<Vector3>(GetComponent<locations>().modGeometry);//both work

        foreach (Vector3 tempv3 in locationList)
        {
            relayList.Add(tempv3);
        }
    }


    // creates list of addresses within spherical asteroid by simply adding addresses within radial distance 'volumeDistance'
    void asteroidVol()
    {
        foreach (Vector3 tempv3 in locationList)
        {
            float volumeDistance = Vector3.Distance(origin, tempv3);
            if (volumeDistance <= (1 * modRadius))//
            {
                asteroidVolume.Add(tempv3);// 
                                           //   instantiatedMod1 = Instantiate(mod1, new Vector3(tempv3.x, tempv3.y, tempv3.z), Quaternion.identity);
                                           //  instantiatedMod1.tag = "geometryAsteroid";

            }
        }
    }


    void asteroidsurface()
    {
        // distance var
        foreach (Vector3 tempv3 in asteroidVolume)
        {
            float volumeDistance = Vector3.Distance(origin, tempv3);
            if (volumeDistance >= (modRadius) - (1))// To have all addresses available to maximum, radials need to be fully 'occupied'.
            {
                asteroidSurface.Add(tempv3);// 
                                            // instantiatedMod1 = Instantiate(mod1, new Vector3(tempv3.x, tempv3.y, tempv3.z), Quaternion.identity);
                                            // Debug.Log("check value: " + tempv3.x / 14.1f + "-" + tempv3.y / 14.1f + "-" + tempv3.z / 14.1f);// check values. must be integers
            }
        }
    }


    // OTHER METHODS #############################################################################################################################################

    // clears relayList values and populates with new tempList values. Then purges tempList. Used between rules.
    void purgeandReload()
    {
        relayList.Clear();// clears (removes invalid locations) and repopulates with new locations

        foreach (Vector3 tempv3 in tempList)
        {
            relayList.Add(tempv3);

        }

        tempList.Clear();
    }


    // [Serializable]

    // class built to store in list
    public class moduleClass
    {
        public int step { get; set; }
        public string modType { get; set; }
        public Vector3 posv3 { get; set; }

    }

    // adds one random module from relaylist to the Build List
    // void addtoBuildlist(int tStep, string tType)
    void addtoBuilt(int tStep, string tType)
    {
        //Debug.Log("_______________________________addtoBuilt ");
        tStep = step;
        tType = type;

        Vector3 randomV3 = relayList[Random.Range(0, relayList.Count)];//selecting a random location
                                                                       // repeat (relaylist count) times, if location already exists


        BuildList.Add(new moduleClass
        {
            step = 10,
            modType = tType,
            posv3 = randomV3,
        });


        //Debug.Log("buildList.Count: " + buildList.Count);
        purgeandReload();

    }

    // instantiates entire build list. Only to be used at the end of processing all modules

    void instantiateBuildlist()
    {
        Debug.Log("___________________________________________buildList.Count: " + BuildList.Count);
        // tempData vector3 = typeof (Vector3).built;
        foreach (moduleClass tempvar in BuildList)
        {
            Vector3 tempv3 = tempvar.posv3;

            instantiatedMod1 = Instantiate(mod0, new Vector3(tempv3.x * unitSize, tempv3.y * unitSize, tempv3.z * unitSize), Quaternion.identity);
            instantiatedMod1.tag = tempvar.modType;// type string = name of module type

            modObjects.Add(instantiatedMod1);// adding to module object reference list
        }
    }




    // generates list of seed mods or islands of seedmods
    void seedMods()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 seedVector = new Vector3(0, 0, 1.0f * i);

            instantiatedMod1 = Instantiate(mod0, seedVector, Quaternion.identity);
            //buildList.Add(seedVector);//
            relayList.Add(seedVector);//
        }
        Debug.Log("NuildList.Count: " + BuildList.Count);
    }

    void clearMods()
    {
        Debug.Log("=clearMods()=");//
        delObj = GameObject.FindGameObjectsWithTag("m.generic");// note PLURAL vs SINGULAR (one in each frame)
        for (int i = 0; i < modObjects.Count; i++)
        {
            Destroy(delObj[i]);// old notation
        }
        BuildList.Clear();
    }


    void mockdata()
    {
        // { generic, mining, oreStorage, processing, refinedStorage, printingBot, manufacturing, equipStorage, assemblyBot, habitat, lifeSupport }
        // STEP 10 sample 

        data_Generic = 0;
        data_Mining = 5;
        data_OreStorage = 5;
        data_Processing = 1;
        data_RefinedStorage = 7;
        data_PrintingBot = 1;
        data_Manufacturing = 1;
        data_EquipStorage = 1;
        data_AssemblyBot = 11;
        data_Habitat = 1;
        data_LifeSupport = 1;

        int[] mockdata = { 0, 5, 8, 1, 7, 1, 1, 1, 11, 1, 1 };// 

        // add mockdata to tempData list
        for (int l = 0; l < mockdata.Length; l++)
        {

            tempData.Add(mockdata[l]);

        }
        Debug.Log("tempData.Count:" + tempData.Count);

        // // verify tempData list
        // for (int l = 0; l < tempData.Count; l++)
        // {
        //     Debug.Log("tempData.Count:" + tempData[l]);
        //     Debug.Log("mockdata[l]:" + (mockdata[l]));
        //
        //     modType value = (modType)l;
        //     Debug.Log("value:" + value);
        // }
    }


    // Methods below are for testing purposes only.########################################################################################################

    void instantiateRelaylist()
    {
        foreach (Vector3 tempv3 in relayList)
        {
            instantiatedMod1 = Instantiate(mod0, new Vector3(tempv3.x * unitSize, tempv3.y * unitSize, tempv3.z * unitSize), Quaternion.identity);
            //instantiatedMod1.tag = "m.generic";
            modObjects.Add(instantiatedMod1);// adding to module object reference list
        }
    }

    void instantiateOnemod() //  
    {

        Vector3 tempv3 = relayList[Random.Range(0, relayList.Count)];//selecting a random location 
        // TO DO: verification
        instantiatedMod1 = Instantiate(mod0, new Vector3(tempv3.x * unitSize, tempv3.y * unitSize, tempv3.z * unitSize), Quaternion.identity);
        //instantiatedMod1.tag = "m.generic";
        modObjects.Add(instantiatedMod1);// adding to module object reference list
        Debug.Log("_________________instantiateOnemod(): ");
    }

    // Visualize global geometric volume constraints:
    void instantiateGlobalvolume()
    {
        Debug.Log("instantiateMods()" + locationList.Count);//
        for (int i = 0; i < locationList.Count; i++)
        {
            Vector3 tempv3 = (locationList[i]);
            instantiatedMod1 = Instantiate(mod0, new Vector3(locationList[i].x * unitSize, locationList[i].y * unitSize, locationList[i].z * unitSize), Quaternion.identity);
            //Debug.Log("test:" + i + modList[i].x + modList[i].y + modList[i].z);
        }
    }

    // Other  methods #####################################################################################################################################

    // modType { generic, mining, oreStorage, processing, refinedStorage, printingBot, manufacturing, equipStorage, assemblyBot, habitat, lifeSupport };
    void getKey()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("key1 pressed");
            //renderGlobalvolume();
            // modSelection = 0;
            //modSelection = modType.mining;

        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("key2 pressed");
            //modSelection = oreStorage;
            //instantiateMods();
            //modSelection = 1;

        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("key3 pressed");
        }



        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Debug.Log("Delete pressed");
            //clearMods();
        }
    }
}






