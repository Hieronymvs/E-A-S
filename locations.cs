using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// notes:
// store (kinds of ) mods in list
// 
//
public class locations : MonoBehaviour
{
    // Game Objects
    public GameObject mod1;// the object that will be instantiated

    // Public Variables
    public float unitSize = 1.0f;// unit diamater in meters
    public int modRadius = 10;
    public int radials = 3;//equals the resolution of the base
    public int tailMultiplier = 10;

    // private Form defining variables
    int rad = 0;// expanding temporary radial needed to determine x,y. expressed in units
    float alpha = 0.0f;// angle of rotation, needed to determine x,y
    float tX = 0.0f;// coordinates are not ints if unitsize is not an int.
    float tY = 0.0f;// temporary locations to be stored in 2d referene list

    float radiusXY;//distance center to new mod(x,y), in pixels
    float zLimit;// max value = modRadius * unitSize. // calculate limit with radiusXY, 0-PI/2 of Cosine (on x,y plane)


    // Storage
    public List<Vector2> baseList = new List<Vector2>();// stores all Vector2 (x,y) unique locations
    public List<GameObject> modObjects = new List<GameObject>();// stores all objects (Modules) and their properties.
    public List<Vector3> modGeometry = new List<Vector3>();// stores all Vector3 (x,y,z) unique locations

    private GameObject[] delObj;// enables reference to destroy object

    void Start()
    {
        //twoPositions();
        generateBaseshape();
    }

    void Update()
    {
    }


    void generateBaseshape()
    {
        // using  2d location reference list + 3d object reference and instantiate list. 
        // generating concentric and expanding location 'circles' 
        // radiusMod = in amount of units
        //pattern is generated as expanding concentric circles

        for (int rad = 0; rad < modRadius; rad++)//
        {
            for (int t = 0; t < radials; t++)
            {
                alpha = ((2.0f * Mathf.PI) / radials) * t;
                tX = Mathf.RoundToInt((Mathf.Cos(alpha) * rad));//
                tY = Mathf.RoundToInt((Mathf.Sin(alpha) * rad));// 

                Vector2 tempV = new Vector2(tX, tY);
                if (baseList.Contains(tempV))
                {
                    // Debug.Log("Duplicate x,y" + tempV);// Duplicates detected'. Vector will not be stored. 
                }
                else
                { // Store in list
                    baseList.Add(new Vector2(tX, tY)); //  adding to location reference list

                    radiusXY = Mathf.Sqrt(Mathf.Pow(tempV.x, 2) + Mathf.Pow(tempV.y, 2));

                    zLimit = (Mathf.Cos((Mathf.PI / 2.0f) / (modRadius)) * ((modRadius) - radiusXY) * tailMultiplier);// max value = modRadius * unitSize

                    for (int z = 0; z < zLimit * 0.4f; z++)// arbitrary design float .ModLength 0.0-1.0, zLimit max value = modRadius * unitSize
                    {
                        //instantiatedMod1 = Instantiate(mod1, new Vector3(tempV.x * unitSize, tempV.y * unitSize, z * unitSize), Quaternion.identity);
                        // modObjects.Add(instantiatedMod1);// adding to module object list

                        // values are rounded to exact multiples of unitsize (14.1) so that these values can be found
                        //Vector3 tempV3E = new Vector3(Mathf.Round((tempV.x * unitSize)*10)/10, Mathf.Round((tempV.y * unitSize)*10)/10, Mathf.Round((z *unitSize)*10)/10);
                        Vector3 tempV3E = new Vector3(Mathf.Round(tempV.x), Mathf.Round(tempV.y), Mathf.Round(z));
                        modGeometry.Add(tempV3E);//'EVEN' layer
                        Vector3 tempV3O = new Vector3((Mathf.Round((tempV.x) * 10) / 10) + (0.5f), (Mathf.Round((tempV.y) * 10) / 10) + (0.5f), (Mathf.Round((z) * 10) / 10) + (0.5f));
                        modGeometry.Add(tempV3O);// 'ODD' layer
                                                 // instantiatedMod1.tag = "modTag";

                    }
                }
            }
        }
        // Debug.Log("            base list count:" + baseList.Count);
        // Debug.Log("mods.Count (all GameObject):" + mods.Count);//
    }


    // destroys all objects and clear Mods list.
    void clearMods()
    {
        //Debug.Log("=clearMods()=");//
        delObj = GameObject.FindGameObjectsWithTag("modTag");// note PLURAL vs SINGULAR (one in each frame)
        for (int i = 0; i < modObjects.Count; i++)
        {
            Destroy(delObj[i]);// old notation

        }
        modObjects.Clear();
    }


    // calculate distance between two locations
    void twoPositions()
    {
        Vector3 tempPos1 = new Vector3(0, 0, 0);
        Vector3 tempPos2 = new Vector3(7.05f, 7.05f, 7.05f);

        float dist = Vector3.Distance(tempPos1, tempPos2);
        print("Distance to other: " + dist);
    }


}

