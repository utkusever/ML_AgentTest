using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages a collection of flower plants and attached flowers
/// </summary>
public class FlowerArea : MonoBehaviour
{
    //the diameter of the area where the agent and flowers can be
    // used for observing relatie distance from agent to flower
    public const float AreaDiameter = 20f;

    // list of all flower plants in this flower area
    private List<GameObject> flowerPlants;

    // a lookup dictionary for looking up a flower from a nectar collider
    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    /// <summary>
    /// list of all flowers in the flower area
    /// </summary>
    public List<Flower> Flowers { get; private set; }

    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    private void Start()
    {
        //find all flowers that are children of this Gameobject/Transform
        FindChildFlowers(this.transform);
    }

    /// <summary>
    /// recursively finds all flowers and flower plants that are children of a parent transform
    /// </summary>
    private void FindChildFlowers(Transform transform)
    {
        foreach (Transform child in transform)
        {
            if (child.CompareTag("flower_plant"))
            {
                // found flower plant add to list
                flowerPlants.Add(child.gameObject);
                // look for flowers within the flower plant
                FindChildFlowers(child);
            }
            else
            {
                // not a flower plant, look for Flower component
                var flower = child.GetComponent<Flower>();
                if (flower)
                {
                    // found a flower, add to flowers list
                    Flowers.Add(flower);
                    // add the nectar collider to the lookup dictionary
                    nectarFlowerDictionary.Add(flower.NectarCollider, flower);
                }
                else
                {
                    //flower component not found, so check children
                    FindChildFlowers(child);
                }
            }
        }
    }

    /// <summary>
    /// reset flowers and the flower plants
    /// </summary>
    public void ResetFlowers()
    {
        //rotate each flower plant around the Y axis and subtly around X and Z
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRot = Random.Range(-5f, 5f);
            float yRot = Random.Range(-180f, 180f);
            float zRot = Random.Range(-5f, 5f);

            flowerPlant.transform.localRotation = Quaternion.Euler(xRot, yRot, zRot);
        }

        //reset each flower
        foreach (var flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    /// <summary>
    /// Gets the <see cref="Flower"/> that a nectar collider belongs to
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }
}