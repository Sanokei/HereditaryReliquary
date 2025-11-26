using UnityEngine;

public class Ship : MonoBehaviour
{
    public float WaterLevel{get;protected set;}
    public float FloatStrength{get;protected set;}
    public float WaterDrag{get;protected set;}
    
    [SerializeField] Rigidbody rb;
    void Awake()
    {
        WaterLevel = 0f;
        FloatStrength = 15f;
        WaterDrag = 0.99f;
    }
    void Start()
    {
        rb.useGravity = true;
    }
    
    void FixedUpdate()
    {
        float depth = WaterLevel - transform.position.y;
        
        if (depth > 0)
        {
            // Apply upward force proportional to depth
            rb.AddForce(depth * FloatStrength * Vector3.up);
            
            // Dampen movement in water
            rb.linearVelocity *= WaterDrag;
        }
    }
}