using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// a hummingbird machine learning agent
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")] [SerializeField]
    private float moveForce;

    [Tooltip("Speed to pitch up or down")] [SerializeField]
    private float pitchSpeed;

    [Tooltip("Speed to rotate around the up axis")] [SerializeField]
    private float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")] [SerializeField]
    private Transform beakTip;

    [Tooltip("Agent's Cam")] [SerializeField]
    private GameObject agentCamera;

    public GameObject AgentCamera => agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")] [SerializeField]
    private bool trainingMode;

    //the rb of the agent
    [SerializeField] private Rigidbody myRigidbody;

    //the flower area that the agent in
    [SerializeField] private FlowerArea flowerArea;

    //the nearest flower to the agent
    private Flower nearestFlower;

    //allows for smoother pitch changes
    private float smoothPitchChange = 0f;

    //allows for smoother yaw changes
    private float smoothYawChange = 0f;

    // max angle that the bird can pitch up or down
    private const float MaxPitchAngle = 80f;

    // max distance from the beak tip to accept nectar collision
    private const float BeakTipRadius = 0.008f;

    // whether the agent is frozen (not flying)
    private bool frozen;

    /// <summary>
    /// the amount of nectar the agent has obtained this episode
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    /// init the agent
    /// </summary>
    public override void Initialize()
    {
        // if not in training mode, no max step, play forever
        if (!trainingMode) MaxStep = 0;
    }

    /// <summary>
    ///  reset the agent when an episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            // only reset flowers in training when there is one agent per area
            flowerArea.ResetFlowers();
        }

        // reset nectar obtained
        NectarObtained = 0;

        // zero out velocities so that movement stops before a new episode begins
        myRigidbody.velocity = Vector3.zero;
        myRigidbody.angularVelocity = Vector3.zero;

        // default to spawning in front of a flower
        bool inFrontOffFlower = true;
        if (trainingMode)
        {
            //Spawn in front of flower 50% of the time during training
            inFrontOffFlower = Random.value > 0.5f;
        }

        // move agent to a new random position
        MoveToSafeRandomPosition(inFrontOffFlower);

        // recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();
    }

    /// <summary>
    /// called when and action is received from either the player input or the neural network
    /// vectorAction[i] represents:
    /// index = 0: move vector x (+1 = right, -1 = left)
    /// index = 1: move vector y (+1 = up, -1 = down)
    /// index = 2: move vector z (+1 = forward, -1 = backward)
    /// index = 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// index = 4: yaw angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        var vectorAction = actions.ContinuousActions;
        // dont take actions if frozen
        if (frozen) return;
        //calculate movement vector
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        // add force in the direction of the move vector
        myRigidbody.AddForce(move * moveForce);
        //get the current rotation
        Vector3 rotationVector = this.transform.rotation.eulerAngles;
        // calculate pitch and yaw rotation
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        // calculate smooth rotation changes 
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
        // calculate new pitch and yaw based on smoothed values
        // clamp pitch to avoid flipping upside down
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;

        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);
        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        // apply the new rotation
        this.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor"> The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // if nearest flower is null, observe an empty array and return early
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        // observe the agent's local rotation (4 observations)
        sensor.AddObservation(this.transform.localRotation.normalized);

        // get a vector from the beak tip to the nearest flower
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        // observe a normalized vector pointing to the nearest flower (3 observation)
        sensor.AddObservation(toFlower.normalized);

        // observe a dot product that indicates whether the beak tip is in front of the flower (1 observation)
        // (+1 means that the beak tip is directly in front of the flower, -1 means directly behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        // observe a dot product that indicates whether the beak tip is pointing toward the flower (1 observation)
        // (+1 means that the beak tip is pointing directly of the flower, -1 means directly away)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        // observe the relative distance from the beak tip to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        // 10 total observations
    }

    /// <summary>
    /// When behaviour Type is set to "Heuristic Only" on the agent's Behaviour Parameters
    /// this function will be called. Its return values will be fed into
    /// <see cref="OnActionReceived(ActionBuffers)"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;

        //create placeholders for all movement/turning
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // convert keyboard inputs to movement and turning
        // All values should be between -1 and +1

        // forward/backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        // left/right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        // up/down
        if (Input.GetKey(KeyCode.LeftShift)) up = transform.up;
        else if (Input.GetKey(KeyCode.LeftCommand)) up = -transform.up;

        // pitch up/down 
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        // turn left/right
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        // combine the movement vectors
        Vector3 combined = (forward + left + up).normalized;

        //Add the 3 movement values, pitch and yaw to actionsOut array
        actions[0] = combined.x;
        actions[1] = combined.y;
        actions[2] = combined.z;
        actions[3] = pitch;
        actions[4] = yaw;
    }

    /// <summary>
    /// prevent the agent from moving taking actions
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        myRigidbody.Sleep();
    }

    /// <summary>
    /// resume agent movement and actions
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        myRigidbody.WakeUp();
    }

    /// <summary>
    /// Move the agent to a safe random position (i.e. does not collide with nothing)
    /// If in front of flower also point the beak at the flower
    /// </summary>
    /// <param name="inFrontOffFlower"> whether to choose a spot in front of a flower </param>
    private void MoveToSafeRandomPosition(bool inFrontOffFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; // Prevent and infinite loop
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // loop until a safe position is found or we run out of attempts
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOffFlower)
            {
                // pick a random flower
                Flower randomFlower = flowerArea.Flowers[Random.Range(0, flowerArea.Flowers.Count)];

                // position 10 to 20 cm in front of the flower
                float distanceFromFlower = Random.Range(0.1f, 0.2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                // point beak at flower (bird's head is center of transform)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                // pick a random height from the ground 
                float height = Random.Range(1.2f, 2.5f);

                // pick a random radius from center of the area
                float radius = Random.Range(2f, 7f);

                // pick a random direction rotated around the y axis
                Quaternion direction = Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f);

                // combine height, radius and direction to pick a potential position
                potentialPosition = flowerArea.transform.position + Vector3.up * height +
                                    direction * Vector3.forward * radius;
                // choose and set random starting pitch and yaw
                float pitch = Random.Range(-60f, 60f);
                float yaw = Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);
            //safe position has been found if no colliders are overlapped
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "could not find a safe position to spawn");
        // set the position and rotation
        this.transform.position = potentialPosition;
        this.transform.rotation = potentialRotation;
    }

    /// <summary>
    /// update the nearest flower to the agent
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (var flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                //No current nearest flower and this flower has nectar, so set to this flower
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                // calculate distance to this flower and distance to the current nearest flower
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower =
                    Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                // if current nearest flower is empty or this flower is closer, update the nearest flower
                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }


    /// <summary>
    ///  called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// called when the agent's collider stay in a trigger collider
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    ///  Handles when the agent's collider enters or stays in a trigger collider
    /// </summary>
    /// <param name="other"></param>
    private void TriggerEnterOrStay(Collider collider)
    {
        // check if agent is colliding with nectar 
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            // check if the closest collision point is close to the beak tip
            // Note: a collision with anything but the beak tip should not count

            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                // look up the flower for this nectar collider
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                // attempt tı take .01 nectar
                // note : this is per fixed timestep, meaning it happens every .02 second 50x per second
                float nectarReceived = flower.Feed(0.01f);

                // keep track of nectar obtained
                NectarObtained += nectarReceived;

                if (trainingMode)
                {
                    // Calculate reward for getting nectar
                    float bonus = 0.02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized,
                        -nearestFlower.FlowerUpVector.normalized));
                    AddReward(0.01f + bonus);
                }

                // if flower is empty, update the nearest flower
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    ///  called when the agent collides with something solid
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // collided with the area boundary, give a negative reward
            AddReward(-0.5f);
        }
    }

    private void Update()
    {
        // draw a line from beak tip to the nearest flower 
        if (nearestFlower)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
        }
    }

    private void FixedUpdate()
    {
        // avoids scenario where nearest flower nectar is stolen by opponent and not updated
        if (nearestFlower && !nearestFlower.HasNectar)
        {
            UpdateNearestFlower();
        }
    }
}