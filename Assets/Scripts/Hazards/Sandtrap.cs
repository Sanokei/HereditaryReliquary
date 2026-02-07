using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sandtrap that increases friction/drag on ships passing through
/// </summary>
[RequireComponent(typeof(Collider))]
public class Sandtrap : MonoBehaviour
{
    [Header("Sandtrap Settings")]
    [Tooltip("Friction multiplier (higher = more friction)")]
    [SerializeField] private float frictionMultiplier = 3f;
    
    [Tooltip("Drag multiplier for ship movement")]
    [SerializeField] private float dragMultiplier = 2f;
    
    [Tooltip("Visual effect radius")]
    [SerializeField] private float effectRadius = 2f;
    
    private HashSet<Ship> shipsInTrap = new HashSet<Ship>();
    private Dictionary<Ship, float> originalDragValues = new Dictionary<Ship, float>();
    
    private void OnTriggerEnter(Collider other)
    {
        Ship ship = other.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            shipsInTrap.Add(ship);
            
            // Store original drag if not already stored
            if (!originalDragValues.ContainsKey(ship))
            {
                originalDragValues[ship] = ship.WaterDrag;
            }
            
            // Apply increased drag
            ship.WaterDrag *= dragMultiplier;
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        Ship ship = other.GetComponent<Ship>();
        if (ship != null && ship.Rigidbody != null)
        {
            // Apply friction force opposite to velocity
            Vector3 velocity = ship.Rigidbody.linearVelocity;
            Vector3 frictionForce = -velocity.normalized * frictionMultiplier * velocity.magnitude;
            frictionForce.y = 0f; // Only horizontal friction
            ship.Rigidbody.AddForce(frictionForce, ForceMode.Force);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        Ship ship = other.GetComponent<Ship>();
        if (ship != null)
        {
            // Restore original drag
            if (originalDragValues.ContainsKey(ship))
            {
                ship.WaterDrag = originalDragValues[ship];
                originalDragValues.Remove(ship);
            }
            
            shipsInTrap.Remove(ship);
        }
    }
    
    private void OnDestroy()
    {
        // Restore drag for all ships when sandtrap is destroyed
        foreach (var ship in shipsInTrap)
        {
            if (ship != null && originalDragValues.ContainsKey(ship))
            {
                ship.WaterDrag = originalDragValues[ship];
            }
        }
        shipsInTrap.Clear();
        originalDragValues.Clear();
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, effectRadius);
    }
}

