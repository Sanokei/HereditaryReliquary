/// <summary>
/// Interface for actors that can be targeted and have targeting capabilities
/// Extends IActor with explicit targeting methods
/// </summary>
public interface ITargetable : IActor
{
    /// <summary>
    /// Sets the target for this actor
    /// </summary>
    void SetTarget(IActor target);
    
    /// <summary>
    /// Clears the current target
    /// </summary>
    void ClearTarget();
    
    /// <summary>
    /// Forces this actor to target another actor (for taunt/aggro effects)
    /// </summary>
    /// <param name="target">The actor to force target</param>
    /// <param name="duration">Duration in seconds (0 = permanent until cleared)</param>
    void ForceTarget(IActor target, float duration = 0f);
    
    /// <summary>
    /// Whether this actor is currently being forced to target someone
    /// </summary>
    bool IsTargetForced { get; }
}

