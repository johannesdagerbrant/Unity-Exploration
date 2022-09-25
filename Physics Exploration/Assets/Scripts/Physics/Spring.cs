using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Spring : MonoBehaviour
{
    public GameObject refObj;
    public float gravity = 9.81f;
    public float mass = 1.0f;
    public float tension = 1.0f;
    public float damping = 1.0f;


    private Vector3 targetPos;
    private Vector3 simulatedPos;
    private Vector3 velocity;

    private Vector3 force;
    private Vector3 delta;
    private float energy;
    private float v;
    private float dt;
    private float inv_mass;

    // Start is called before the first frame update
    void Start()
    {
        simulatedPos = transform.position;
        velocity = new Vector3();
    }

    // Update is called once per frame
    void Update()
    {
        dt = Time.deltaTime;
        targetPos = refObj.transform.position;
        inv_mass = 1.0f / mass;

        // Apply and integrate forces
        force = Vector3.down * mass * gravity;
        simulatedPos += velocity * dt + force * 0.5f * dt * dt;
        velocity += force * dt;

        // Spring constrain
        delta = simulatedPos - targetPos;
        velocity -= delta * inv_mass * tension * dt;

        // Damping
        energy = 0.5f * mass * velocity.sqrMagnitude;
        energy *= damping;
        v = Mathf.Sqrt(energy / (0.5f * mass));
        velocity -= (velocity / velocity.magnitude) * v * dt;

        // Set simulated position
        transform.position = simulatedPos;
    }
}
