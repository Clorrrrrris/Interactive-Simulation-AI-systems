using UnityEngine;

public class Pinball : MonoBehaviour
{
    public float speed;
    public Vector3 velocity;
    public Vector3 acceleration;
    public float timeScale;
    public float jiggleStrength;
    private BallCollider ballCollider;

    public float maxSpeed = 30f;

    void Start()
    {
        velocity = new Vector3(0, 0, -speed);
        ballCollider = GetComponent<BallCollider>();
    }

    void Update()
    {
        // if table jiggle
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Vector3 noise = new Vector3(
                Random.Range(-jiggleStrength, jiggleStrength),
                0f, 
                Random.Range(-jiggleStrength, jiggleStrength)
            );
            velocity += noise;
        }

        velocity += acceleration * Time.deltaTime * timeScale;
        //LimitVelocity();
        transform.position += velocity * Time.deltaTime * timeScale;

        // velocity change after collided with objects
        velocity = ballCollider.ResolveCollisions(transform.position, velocity);    
        //LimitVelocity();
    }

    // avoid ball move too fast
    void LimitVelocity()
    {
        Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
        float currentSpeed = horizontalVel.magnitude;
        
        if (currentSpeed > maxSpeed)
        {
            velocity.x = (velocity.x / currentSpeed) * maxSpeed;
            velocity.z = (velocity.z / currentSpeed) * maxSpeed;
        }
    }
}