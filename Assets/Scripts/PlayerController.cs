using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PlayerController : Agent
{
    [Header("References")] public GameManager gameManager;
    public Transform playerSpawnLocation;
    public float targetYPosition;

    [Header("Settings")] public string targetTag = "target";
    public float upwardForceScaler = 100f;

    new Rigidbody rigidbody;

    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        Reset();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.DiscreteActions;
        //take key input
        actions[0] = 0; // idle
        if (Input.GetKey(KeyCode.Space))
        {
            actions[0] = 1; // fly
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // player's y pos
        sensor.AddObservation(transform.position.y);
        //target's y pos
        sensor.AddObservation(targetYPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var actionsArray = actions.DiscreteActions;
        if (Mathf.FloorToInt(actionsArray[0]) == 1)
        {
            Fly();
        }
    }

    public void Reset()
    {
        //reset position and velocity
        transform.position = playerSpawnLocation.position;
        rigidbody.velocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        //raycast out the z direction
        Ray ray = new Ray(transform.position, transform.forward);
        Debug.DrawRay(transform.position, transform.forward, Color.red, 1f);

        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo))
        {
            if (hitInfo.transform.CompareTag(targetTag))
            {
                HandleTargetHit();
            }
        }

        if (transform.position.y > 10f)
        {
            AddReward(-0.1f);
        }
    }

    void HandleTargetHit()
    {
        AddReward(1f);
        //hit a target, so let the GameManager know
        gameManager.TargetHit();
    }

    void Fly()
    {
        //add up force to rigid body
        rigidbody.AddForce(Vector3.up * upwardForceScaler);
    }
}