using UnityEngine;

/// <summary>
/// Rock obstacle that blocks ship movement through collision
/// </summary>
[RequireComponent(typeof(Collider))]
public class Rock : MonoBehaviour
{
    [Header("Rock Settings")]
    
    [Tooltip("Bounce force when ship hits rock")]
    [SerializeField] private float bounceForce = 5f;
    
    private void OnCollisionEnter(Collision collision)
    {
        Ship ship = collision.gameObject.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            // Apply bounce force away from rock
            Vector3 bounceDirection = (collision.transform.position - transform.position).normalized;
            bounceDirection.y = 0f; // Keep it horizontal
            ship.Rigidbody.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Ship ship = other.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            // Push ship away from rock
            Vector3 pushDirection = (other.transform.position - transform.position).normalized;
            pushDirection.y = 0f;
            ship.Rigidbody.AddForce(pushDirection * bounceForce, ForceMode.Impulse);
        }
    }
}

