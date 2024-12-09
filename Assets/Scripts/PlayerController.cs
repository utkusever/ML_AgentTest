using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public Transform playerSpawnLocation;

    [Header("Settings")]
    public string targetTag = "target";
    public float upwardForceScaler = 100f;

    new Rigidbody rigidbody;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    public void Reset()
    {
        //reset position and velocity
        transform.position = playerSpawnLocation.position;
        rigidbody.velocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        //take key input
        if (Input.GetKey(KeyCode.Space))
        {
            Fly();
        }

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

    }

    void HandleTargetHit()
    {
        //hit a target, so let the GameManager know
        gameManager.TargetHit();
    }

    void Fly()
    {
        //add up force to rigid body
        rigidbody.AddForce(Vector3.up * upwardForceScaler);
    }
}
