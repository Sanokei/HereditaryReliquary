using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    // Win condition event
    public delegate void OnWinCondition();
    public static event OnWinCondition OnWinConditionEvent;
    [Header("Buoyancy Settings")]
    [SerializeField] private float waterLevel = 0f;
    [SerializeField] private float floatStrength = 30f; // Increased for stronger buoyancy
    [SerializeField] private float waterDrag = 0.99f;
    [SerializeField] private float buoyancyDamping = 0.2f; // Increased damping to reduce bouncing
    
    [Header("Wave Interaction")]
    [SerializeField] private float waveForceMultiplier = 1f;
    [SerializeField] private float waveSensitivity = 1f;
    
    [Header("Grid Tracking")]
    [SerializeField] private bool useBoundsForGridCells = true;
    [SerializeField] private Vector3 gridCellSize = Vector3.one;
    
    [Header("Goal Tracking")]
    [SerializeField] private bool pointTowardsGoal = true;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private string goalMarkerName = "GoalMarker";
    [SerializeField] private bool useLevelDataGoal = true;
    [SerializeField] private float rotationOffset = 90f; // Offset in degrees to correct boat orientation
    
    private Rigidbody rb;
    private Vector3 accumulatedWaveForce = Vector3.zero;
    private Collider shipCollider;
    private Bounds shipBounds;
    private Transform goalTransform;
    private Vector3? goalPosition;
    private bool hasWon = false; // Prevent multiple win triggers
    
    public float WaterLevel { get => waterLevel; set => waterLevel = value; }
    public float FloatStrength { get => floatStrength; set => floatStrength = value; }
    public float WaterDrag { get => waterDrag; set => waterDrag = value; }
    public Rigidbody Rigidbody => rb;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        shipCollider = GetComponent<Collider>();
        if (shipCollider == null)
        {
            shipCollider = GetComponentInChildren<Collider>();
        }
        
        // Calculate bounds from collider or renderer
        CalculateShipBounds();
    }
    
    void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.useGravity = true;
        FindGoal();
        
        // Ensure ship starts at correct water level
        // Position ship so its bottom will be at waterLevel (y=0)
        CalculateShipBounds();
        float shipBottom = shipBounds.min.y;
        float offsetFromBottom = transform.position.y - shipBottom;
        
        // Always position ship so its bottom is at water level
        // This ensures it floats correctly regardless of initial position
        float targetY = waterLevel + offsetFromBottom;
        
        // Always adjust position to ensure correct water level
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        
        // Reset velocity to prevent sinking
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.mass = Mathf.Max(0.1f, rb.mass); // Ensure mass isn't too high
        }
        
        // Recalculate bounds after positioning
        CalculateShipBounds();
        
        Debug.Log($"Ship positioned: Bottom={shipBounds.min.y:F2}, Center={transform.position.y:F2}, WaterLevel={waterLevel}");
    }
    
    void Update()
    {
        if (pointTowardsGoal)
        {
            RotateTowardsGoal();
        }
        
        // Check proximity to goal/island for win condition
        CheckWinCondition();
    }
    
    /// <summary>
    /// Checks if the boat is close enough to the island to trigger win condition
    /// </summary>
    private void CheckWinCondition()
    {
        if (hasWon)
            return;
        
        // Update goal position if we have a transform
        if (goalTransform != null)
        {
            goalPosition = goalTransform.position;
        }
        
        if (!goalPosition.HasValue)
            return;
        
        // Calculate distance to goal
        float distanceToGoal = Vector3.Distance(transform.position, goalPosition.Value);
        
        // Check if boat is close enough (within 2 units, or touching)
        if (distanceToGoal < 2f)
        {
            // Also check if boat bounds overlap with island bounds
            CalculateShipBounds();
            
            // Try to get island bounds
            if (goalTransform != null)
            {
                Collider islandCollider = goalTransform.GetComponent<Collider>();
                if (islandCollider != null)
                {
                    Bounds islandBounds = islandCollider.bounds;
                    if (shipBounds.Intersects(islandBounds))
                    {
                        TriggerWinCondition();
                    }
                }
                else
                {
                    // Fallback: use distance check
                    if (distanceToGoal < 1f)
                    {
                        TriggerWinCondition();
                    }
                }
            }
        }
    }
    
    void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyWaveForces();
    }
    
    void LateUpdate()
    {
        // Ensure ship doesn't sink below water level
        // This acts as a safety net in case buoyancy isn't strong enough
        CalculateShipBounds();
        float shipBottom = shipBounds.min.y;
        
        if (shipBottom < waterLevel - 0.1f)
        {
            // Ship is sinking below water level - force it back up
            float offsetFromBottom = transform.position.y - shipBottom;
            float targetY = waterLevel + offsetFromBottom;
            
            // Smoothly move towards target
            float currentY = transform.position.y;
            float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * 5f);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Reduce downward velocity
            if (rb != null && rb.linearVelocity.y < 0)
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y *= 0.5f; // Reduce downward velocity
                rb.linearVelocity = velocity;
            }
        }
    }
    
    /// <summary>
    /// Applies buoyancy force based on water level
    /// </summary>
    private void ApplyBuoyancy()
    {
        if (rb == null)
            return;
            
        // Calculate depth based on the bottom of the ship, not the center
        // This ensures the ship floats properly even when center is at water level
        CalculateShipBounds();
        float shipBottom = shipBounds.min.y;
        float shipCenterY = transform.position.y;
        float depth = waterLevel - shipBottom;
        
        // Target: ship bottom should be at waterLevel (y=0)
        // Calculate target center Y position based on ship height
        float shipHeight = shipBounds.max.y - shipBounds.min.y;
        if (shipHeight <= 0)
            shipHeight = 1f; // Fallback if height calculation fails
        
        float targetCenterY = waterLevel + (shipHeight * 0.5f);
        float offsetFromTarget = targetCenterY - shipCenterY;
        
        // Always apply correction force to maintain target position
        // This is the primary mechanism to keep ship at water level
        float correctionForce = offsetFromTarget * floatStrength * 3f;
        
        // Additional buoyancy when submerged
        float buoyancyForce = 0f;
        if (depth > 0)
        {
            // Ship is partially or fully submerged - apply additional buoyancy
            buoyancyForce = depth * floatStrength * 2f;
        }
        
        // Combine forces - always apply correction, add buoyancy if submerged
        float totalForce = correctionForce + buoyancyForce;
        
        // Apply force
        rb.AddForce(Vector3.up * totalForce, ForceMode.Force);
        
        // Apply damping to reduce oscillation
        if (Mathf.Abs(offsetFromTarget) < 1f)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y *= (1f - buoyancyDamping);
            rb.linearVelocity = velocity;
        }
        
        // Dampen horizontal movement in water
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        horizontalVelocity *= waterDrag;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        
        // Debug output (can be removed later)
        if (Mathf.Abs(offsetFromTarget) > 0.5f)
        {
            Debug.Log($"Ship buoyancy: Bottom={shipBottom:F2}, Target={targetCenterY:F2}, Current={shipCenterY:F2}, Force={totalForce:F2}");
        }
    }
    
    /// <summary>
    /// Applies accumulated wave forces to the boat
    /// </summary>
    private void ApplyWaveForces()
    {
        if (accumulatedWaveForce.magnitude > 0.01f)
        {
            rb.AddForce(accumulatedWaveForce * waveForceMultiplier, ForceMode.Force);
            accumulatedWaveForce = Vector3.zero; // Reset after applying
        }
    }
    
    /// <summary>
    /// Called by Wave objects to apply force to this boat
    /// </summary>
    public void ApplyWaveForce(Vector3 force)
    {
        accumulatedWaveForce += force * waveSensitivity;
    }
    
    /// <summary>
    /// Calculates the ship's bounds from collider or renderer
    /// </summary>
    private void CalculateShipBounds()
    {
        if (shipCollider != null)
        {
            shipBounds = shipCollider.bounds;
        }
        else
        {
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                shipBounds = renderer.bounds;
            }
            else
            {
                // Fallback to a default bounds
                shipBounds = new Bounds(transform.position, Vector3.one);
            }
        }
    }
    
    /// <summary>
    /// Gets the grid cells occupied by this boat based on its bounds
    /// </summary>
    public List<Vector3Int> GetOccupiedGridCells(Grid grid)
    {
        if (grid == null)
        {
            Debug.LogWarning("Ship: Grid is null, cannot calculate occupied cells.");
            return new List<Vector3Int>();
        }
        
        // Recalculate bounds in case the ship moved
        CalculateShipBounds();
        
        List<Vector3Int> occupiedCells = new List<Vector3Int>();
        
        if (useBoundsForGridCells)
        {
            // Get the bounds corners and find all cells within
            Vector3 min = shipBounds.min;
            Vector3 max = shipBounds.max;
            
            // Convert world bounds to grid cells
            Vector3Int minCell = grid.WorldToCell(min);
            Vector3Int maxCell = grid.WorldToCell(max);
            
            // Add all cells within the bounds
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, 0, z);
                    Vector3 cellCenter = grid.GetCellCenterWorld(cellPos);
                    
                    // Check if cell center is within ship bounds (XZ plane)
                    if (cellCenter.x >= min.x && cellCenter.x <= max.x &&
                        cellCenter.z >= min.z && cellCenter.z <= max.z)
                    {
                        occupiedCells.Add(cellPos);
                    }
                }
            }
        }
        else
        {
            // Use single cell at ship position
            Vector3Int cellPos = grid.WorldToCell(transform.position);
            occupiedCells.Add(new Vector3Int(cellPos.x, 0, cellPos.z));
        }
        
        // If no cells found, add at least the ship's position cell
        if (occupiedCells.Count == 0)
        {
            Vector3Int cellPos = grid.WorldToCell(transform.position);
            occupiedCells.Add(new Vector3Int(cellPos.x, 0, cellPos.z));
        }
        
        return occupiedCells;
    }
    
    /// <summary>
    /// Gets the world position bounds of the ship
    /// </summary>
    public Bounds GetBounds()
    {
        CalculateShipBounds();
        return shipBounds;
    }
    
    /// <summary>
    /// Finds the goal position in the scene
    /// </summary>
    private void FindGoal()
    {
        goalTransform = null;
        goalPosition = null;
        
        // First, try to find a goal marker GameObject
        GameObject goalMarker = GameObject.Find(goalMarkerName);
        if (goalMarker != null)
        {
            goalTransform = goalMarker.transform;
            goalPosition = goalTransform.position;
            return;
        }
        
        // If not found and useLevelDataGoal is enabled, try to get from LevelData
        if (useLevelDataGoal)
        {
            FindGoalFromLevelData();
        }
    }
    
    /// <summary>
    /// Attempts to find goal position from LevelData and Grid
    /// </summary>
    private void FindGoalFromLevelData()
    {
        // Try to find LevelBuilder which has the grid and level data
        GridBuilder.Core.LevelBuilder levelBuilder = FindFirstObjectByType<GridBuilder.Core.LevelBuilder>();
        if (levelBuilder != null)
        {
            // LevelBuilder should have access to the goal position
            // We'll need to check if there's a public method or property
            // For now, try to find the goal marker it created
            Transform goalMarkerTransform = levelBuilder.transform.Find(goalMarkerName);
            if (goalMarkerTransform != null)
            {
                goalTransform = goalMarkerTransform;
                goalPosition = goalTransform.position;
                return;
            }
        }
        
        // Try to find SplineGridContainer and LevelData
        GridBuilder.Core.SplineGridContainer gridContainer = FindFirstObjectByType<GridBuilder.Core.SplineGridContainer>();
        if (gridContainer != null && gridContainer.Grid != null)
        {
            // Try to find LevelData in resources or via reflection
            // This is a fallback - ideally the goal marker GameObject should exist
            UnityEngine.Object[] levelDataAssets = Resources.FindObjectsOfTypeAll(typeof(GridBuilder.Core.LevelData));
            if (levelDataAssets != null && levelDataAssets.Length > 0)
            {
                GridBuilder.Core.LevelData levelData = levelDataAssets[0] as GridBuilder.Core.LevelData;
                if (levelData != null)
                {
                    Vector3 goalWorldPos = gridContainer.Grid.GetCellCenterWorld(levelData.goalCell);
                    goalWorldPos.y = 0f;
                    goalPosition = goalWorldPos;
                }
            }
        }
    }
    
    /// <summary>
    /// Rotates the ship to face towards the goal
    /// </summary>
    private void RotateTowardsGoal()
    {
        // Update goal position if we have a transform (in case it moves)
        if (goalTransform != null)
        {
            goalPosition = goalTransform.position;
        }
        
        if (!goalPosition.HasValue)
        {
            // Try to find goal again if we lost it
            FindGoal();
            return;
        }
        
        Vector3 goalPos = goalPosition.Value;
        Vector3 shipPos = transform.position;
        
        // Calculate direction to goal (only on XZ plane)
        Vector3 direction = new Vector3(goalPos.x - shipPos.x, 0f, goalPos.z - shipPos.z);
        
        // Only rotate if we're not already facing the goal (or very close)
        if (direction.magnitude > 0.1f)
        {
            // Normalize direction
            direction.Normalize();
            
            // Create target rotation - LookRotation makes forward point along the direction
            // If the boat's forward is along Z, this should work. If it's along X, we need to adjust
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            
            // Apply rotation offset to correct boat orientation
            // This accounts for boats where the forward direction is not along the Z axis
            if (Mathf.Abs(rotationOffset) > 0.01f)
            {
                targetRotation = targetRotation * Quaternion.Euler(0, rotationOffset, 0);
            }
            
            // Smoothly rotate towards the goal
            if (rb != null)
            {
                // Use physics-based rotation for more natural movement
                Quaternion currentRotation = transform.rotation;
                Quaternion newRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
                
                // Apply rotation to rigidbody
                rb.MoveRotation(newRotation);
            }
            else
            {
                // Fallback to direct rotation if no rigidbody
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// Sets the goal position manually
    /// </summary>
    public void SetGoalPosition(Vector3 position)
    {
        goalPosition = position;
        goalTransform = null;
    }
    
    /// <summary>
    /// Sets the goal transform to track
    /// </summary>
    public void SetGoalTransform(Transform goal)
    {
        goalTransform = goal;
        if (goal != null)
        {
            goalPosition = goal.position;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if colliding with island/goal marker
        if (!hasWon && (other.name.Contains("GoalMarker") || other.name.Contains("island") || other.name.Contains("Island")))
        {
            TriggerWinCondition();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Also check for collision-based contact with island
        if (!hasWon && (collision.gameObject.name.Contains("GoalMarker") || collision.gameObject.name.Contains("island") || collision.gameObject.name.Contains("Island")))
        {
            TriggerWinCondition();
        }
    }
    
    /// <summary>
    /// Triggers the win condition event
    /// </summary>
    private void TriggerWinCondition()
    {
        if (hasWon)
            return;
        
        hasWon = true;
        OnWinConditionEvent?.Invoke();
    }
    
    /// <summary>
    /// Resets the win condition (useful for restarting levels)
    /// </summary>
    public void ResetWinCondition()
    {
        hasWon = false;
    }
    
    void OnValidate()
    {
        // Ensure values are reasonable
        floatStrength = Mathf.Max(0f, floatStrength);
        waterDrag = Mathf.Clamp01(waterDrag);
        buoyancyDamping = Mathf.Clamp01(buoyancyDamping);
        waveForceMultiplier = Mathf.Max(0f, waveForceMultiplier);
        waveSensitivity = Mathf.Max(0f, waveSensitivity);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
    }
}