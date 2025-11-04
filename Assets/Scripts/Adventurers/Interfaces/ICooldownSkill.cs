/// <summary>
/// Optional interface for skills that have cooldown mechanics
/// </summary>
public interface ICooldownSkill
{
    /// <summary>
    /// Whether the skill is currently cooling down
    /// </summary>
    bool IsCoolingDown { get; }
    
    /// <summary>
    /// Remaining cooldown time in seconds
    /// </summary>
    float RemainingCooldown { get; }
    
    /// <summary>
    /// Total cooldown duration in seconds
    /// </summary>
    float CooldownDuration { get; }
    
    /// <summary>
    /// Starts the cooldown timer
    /// </summary>
    void StartCooldown();
}

