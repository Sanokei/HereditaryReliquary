using UnityEngine;

/// <summary>
/// Team affiliation for combat
/// </summary>
public enum CombatTeam
{
    PlayerTeam,
    EnemyTeam
}

/// <summary>
/// Interface for actors that can perform skills
/// </summary>
public interface IActor
{
    string Name { get; }
    Transform Transform { get; }
    
    /// <summary>
    /// The team this actor belongs to (PlayerTeam or EnemyTeam)
    /// </summary>
    CombatTeam Team { get; }
    
    /// <summary>
    /// Whether this actor is on the player's team
    /// </summary>
    bool IsPlayerTeam { get; }
    
    /// <summary>
    /// The current target this actor is focusing on (can be null)
    /// </summary>
    IActor CurrentTarget { get; set; }
}

