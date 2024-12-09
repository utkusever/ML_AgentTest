using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a single flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full")]
    public Color fullFlowerColor = new Color(1f, 0, 0.3f);

    [Tooltip("The color when the flower is empty")]
    public Color emptyFlowerColor = new Color(0.5f, 0f, 1f);

    [SerializeField] private Collider nectarCollider;

    public Collider NectarCollider => nectarCollider;

    [SerializeField] private Collider flowerCollider;
    private Material flowerMat;
    private Material flowerMatToApply;
    /// <summary>
    /// A vector pointing straight out of the flower
    /// </summary>
    public Vector3 FlowerUpVector
    {
        get { return nectarCollider.transform.up; }
    }

    /// <summary>
    /// The center position of the nectar collider
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get { return nectarCollider.transform.position; }
    }

    /// <summary>
    /// amount of remaining nectar in the flower
    /// </summary>
    public float NectarAmount { get; private set; }

    /// <summary>
    /// whether the flower has any nectar remaining
    /// </summary>
    public bool HasNectar
    {
        get { return NectarAmount > 0; }
    }

    private void Awake()
    {
        flowerMat = this.GetComponent<MeshRenderer>().material;
    }

    /// <summary>
    /// Attemps to remove nectar from the flower
    /// </summary>
    /// <param name="amount">The amount of nectar to remove </param> 
    /// <returns></returns>
    public float Feed(float amount)
    {
        //Track how much nectar was successfully taken
        float nectarTaken = Mathf.Clamp(amount, 0, NectarAmount);
        NectarAmount -= nectarTaken;
        if (NectarAmount <= 0)
        {
            // No nectar remaining
            NectarAmount = 0;
            //disable the flower and nectar colliders
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);
            //Change color of the flower
            flowerMat.SetColor("_Color", emptyFlowerColor);
        }

        // return the amount of nectar that was taken
        return nectarTaken;
    }

    /// <summary>
    /// Resets the flower
    /// </summary>
    public void ResetFlower()
    {
        //Refill the nectar
        NectarAmount = 1f;
        //Enable the flower 
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);
        //Reset color
        flowerMat.SetColor("_Color", fullFlowerColor);
    }
}