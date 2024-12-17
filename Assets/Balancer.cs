using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class Balancer : Agent
{
    [SerializeField] private Transform ball;
    [SerializeField] private Rigidbody ballRB;
    [SerializeField] private float rotationSpeed;

    private float smoothRightChange = 0f;

    private float smoothUpChange = 0f;

    // max angle that the plane can left or right, up or down
    private const float MaxRotAngle = 45f;

    public void AddRewardToAgent(float value)
    {
        AddReward(value);
    }

    /// <summary>
    /// 0 = x rot
    /// 1 = z rot
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        var actionsOut = actions.ContinuousActions;

        float rightChange = actionsOut[0];
        float upChange = actionsOut[1];

        Vector3 rotationVector = this.transform.rotation.eulerAngles;

        // calculate smooth rotation changes 
        smoothRightChange = Mathf.MoveTowards(smoothRightChange, rightChange, 2f * Time.fixedDeltaTime);
        smoothUpChange = Mathf.MoveTowards(smoothUpChange, upChange, 2f * Time.fixedDeltaTime);

        // calculate new right rot and up rot based on smoothed values
        // clamp right and up to avoid flipping upside down

        float right = rotationVector.x + smoothRightChange * Time.fixedDeltaTime * rotationSpeed;
        // for quartenion.euler 
        if (right > 180f)
        {
            right -= 360;
        }

        right = Mathf.Clamp(right, -MaxRotAngle, MaxRotAngle);

        float up = rotationVector.z + smoothUpChange * Time.fixedDeltaTime * rotationSpeed;
        if (up > 180f)
        {
            up -= 360;
        }

        up = Mathf.Clamp(up, -MaxRotAngle, MaxRotAngle);
        this.transform.rotation = Quaternion.Euler(right, 0, up);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        float right = 0f;
        float up = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) up = 1;
        else if (Input.GetKey(KeyCode.DownArrow)) up = -1;

        if (Input.GetKey(KeyCode.LeftArrow)) right = -1;
        else if (Input.GetKey(KeyCode.RightArrow)) right = 1;

        actions[0] = right;
        actions[1] = up;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(ball.transform.position.x); // target ball x y z pos = 3 observation
        sensor.AddObservation(ball.transform.position.y);
        sensor.AddObservation(ball.transform.position.z);
        sensor.AddObservation(this.transform.localRotation); //balancer rot = 4 observation
    }

    private Vector3 stockPos;
    private Vector3 ballStockPos;

    public override void Initialize()
    {
        base.Initialize();
        stockPos = this.transform.position;
        ballStockPos = this.transform.position;
    }

    public override void OnEpisodeBegin()
    {
        // zero out velocities so that movement stops before a new episode begins
        ballRB.velocity = Vector3.zero;
        ballRB.angularVelocity = Vector3.zero;
        ball.transform.position = ballStockPos;
        this.transform.position = stockPos;
        this.transform.rotation = Quaternion.Euler(Vector3.zero);
        this.transform.rotation = Quaternion.Euler(Random.Range(-30f, 30f), 0, Random.Range(-30f, 30));
    }

    private float time = 0;

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        if (time >= 1f)
        {
            time = 0;
            AddRewardToAgent(0.1f);
        }
    }
}