using UnityEngine;

/// <summary>
/// Whirlpool that pulls ships in and rotates them
/// </summary>
[RequireComponent(typeof(Collider))]
public class Whirlpool : MonoBehaviour
{
    [Header("Whirlpool Settings")]
    [Tooltip("Pull strength towards center")]
    [SerializeField] private float pullStrength = 10f;
    
    [Tooltip("Rotation speed (degrees per second)")]
    [SerializeField] private float rotationSpeed = 90f;
    
    [Tooltip("Maximum distance for pull effect")]
    [SerializeField] private float maxPullDistance = 5f;
    
    [Tooltip("Visual rotation speed")]
    [SerializeField] private float visualRotationSpeed = 45f;
    
    private void FixedUpdate()
    {
        // Rotate the visual representation
        transform.Rotate(0, visualRotationSpeed * Time.fixedDeltaTime, 0);
    }
    
    private void OnTriggerStay(Collider other)
    {
        Ship ship = other.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            Vector3 toCenter = transform.position - ship.transform.position;
            toCenter.y = 0f; // Keep it horizontal
            float distance = toCenter.magnitude;
            
            if (distance < maxPullDistance && distance > 0.1f)
            {
                // Pull towards center
                float pullForce = pullStrength * (1f - (distance / maxPullDistance));
                Vector3 pullDirection = toCenter.normalized;
                ship.Rigidbody.AddForce(pullDirection * pullForce, ForceMode.Force);
                
                // Apply rotation
                ship.Rigidbody.AddTorque(Vector3.up * rotationSpeed * Mathf.Deg2Rad, ForceMode.Force);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxPullDistance);
    }
}

