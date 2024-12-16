using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class CollisionChecker : MonoBehaviour
{
    [SerializeField] private Balancer balancerAgent;
    [SerializeField] private GameManagerBalancer gameManagerBalancer;

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("boundary"))
        {
            balancerAgent.AddRewardToAgent(-1f);
            gameManagerBalancer.GameReset();
        }
    }
}