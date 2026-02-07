using UnityEngine;

/// <summary>
/// Current that pushes ships in a specific direction
/// </summary>
[RequireComponent(typeof(Collider))]
public class Current : MonoBehaviour
{
    [Header("Current Settings")]
    [Tooltip("Direction of the current (normalized)")]
    [SerializeField] private Vector3 currentDirection = Vector3.forward;
    
    [Tooltip("Force strength of the current")]
    [SerializeField] private float currentStrength = 5f;
    
    [Tooltip("Visualize current direction")]
    [SerializeField] private bool showDirectionGizmo = true;
    
    private void OnTriggerStay(Collider other)
    {
        Ship ship = other.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            // Normalize direction and keep it horizontal
            Vector3 direction = currentDirection.normalized;
            direction.y = 0f;
            direction.Normalize();
            
            // Apply current force
            Vector3 force = direction * currentStrength;
            ship.Rigidbody.AddForce(force, ForceMode.Force);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (showDirectionGizmo)
        {
            Vector3 direction = currentDirection.normalized;
            direction.y = 0f;
            direction.Normalize();
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, direction * 2f);
            
            // Draw arrow head
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 arrow1 = direction * 2f - right * 0.3f;
            Vector3 arrow2 = direction * 2f + right * 0.3f;
            Gizmos.DrawLine(transform.position + direction * 2f, transform.position + arrow1);
            Gizmos.DrawLine(transform.position + direction * 2f, transform.position + arrow2);
        }
    }
}

