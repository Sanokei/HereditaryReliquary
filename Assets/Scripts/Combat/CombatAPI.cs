using System.Collections.Generic;

/// <summary>
/// Static API layer for combat operations
/// Provides easy access to combat functionality for skills and other systems
/// </summary>
public static class CombatAPI
{
    /// <summary>
    /// Gets all enemies of the specified actor
    /// </summary>
    public static List<IActor> GetEnemies(IActor actor)
    {
        if (actor == null) return new List<IActor>();
        
        var manager = CombatManager.Instance;
        if (manager == null) return new List<IActor>();
        
        return manager.GetEnemies(actor);
    }
    
    /// <summary>
    /// Gets all allies of the specified actor (excluding self)
    /// </summary>
    public static List<IActor> GetAllies(IActor actor)
    {
        if (actor == null) return new List<IActor>();
        
        var manager = CombatManager.Instance;
        if (manager == null) return new List<IActor>();
        
        return manager.GetAllies(actor);
    }
    
    /// <summary>
    /// Gets all actors currently in combat
    /// </summary>
    public static List<IActor> GetAllCombatants()
    {
        var manager = CombatManager.Instance;
        if (manager == null) return new List<IActor>();
        
        return manager.GetAllCombatants();
    }
    
    /// <summary>
    /// Checks if an actor is on the player's team
    /// </summary>
    public static bool IsPlayerTeam(IActor actor)
    {
        return actor != null && actor.IsPlayerTeam;
    }
    
    /// <summary>
    /// Checks if an actor is on the enemy team
    /// </summary>
    public static bool IsEnemyTeam(IActor actor)
    {
        return actor != null && !actor.IsPlayerTeam;
    }
    
    /// <summary>
    /// Forces an actor to target another actor for a duration
    /// Duration of 0 means permanent until cleared manually
    /// </summary>
    public static void ForceTarget(IActor source, IActor target, float duration = 0f)
    {
        if (source == null || target == null) return;
        
        var manager = CombatManager.Instance;
        if (manager == null) return;
        
        manager.ForceTarget(source, target, duration);
    }
    
    /// <summary>
    /// Clears forced targeting for an actor
    /// </summary>
    public static void ClearForcedTarget(IActor actor)
    {
        if (actor == null) return;
        
        var manager = CombatManager.Instance;
        if (manager == null) return;
        
        manager.ClearForcedTarget(actor);
    }
    
    /// <summary>
    /// Gets the current combat manager instance
    /// </summary>
    public static CombatManager GetCurrentCombat()
    {
        return CombatManager.Instance;
    }
    
    /// <summary>
    /// Registers an actor for combat
    /// </summary>
    public static void RegisterActor(IActor actor)
    {
        if (actor == null) return;
        
        var manager = CombatManager.Instance;
        if (manager == null) return;
        
        manager.RegisterActor(actor);
    }
    
    /// <summary>
    /// Unregisters an actor from combat
    /// </summary>
    public static void UnregisterActor(IActor actor)
    {
        if (actor == null) return;
        
        var manager = CombatManager.Instance;
        if (manager == null) return;
        
        manager.UnregisterActor(actor);
    }
}

