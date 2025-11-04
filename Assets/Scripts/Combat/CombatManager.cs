using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages active combat state, tracking all actors and their targeting
/// Singleton pattern for easy access throughout the game
/// </summary>
public class CombatManager : MonoBehaviour
{
    private static CombatManager _instance;
    
    /// <summary>
    /// Singleton instance of CombatManager
    /// </summary>
    public static CombatManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CombatManager");
                _instance = go.AddComponent<CombatManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    // Track all actors in combat
    private List<IActor> playerTeamActors = new List<IActor>();
    private List<IActor> enemyTeamActors = new List<IActor>();
    
    // Track forced targeting (for taunt/aggro effects)
    private Dictionary<IActor, ForcedTarget> forcedTargets = new Dictionary<IActor, ForcedTarget>();
    
    /// <summary>
    /// Internal class to track forced targeting with duration
    /// </summary>
    private class ForcedTarget
    {
        public IActor target;
        public float duration;
        public float startTime;
        
        public ForcedTarget(IActor target, float duration)
        {
            this.target = target;
            this.duration = duration;
            this.startTime = Time.time;
        }
        
        public bool IsExpired => duration > 0f && (Time.time - startTime) >= duration;
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    private void Update()
    {
        // Clean up expired forced targets
        var expiredKeys = forcedTargets.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            forcedTargets.Remove(key);
            if (key is ITargetable targetable)
            {
                targetable.ClearTarget();
            }
        }
    }
    
    /// <summary>
    /// Registers an actor for combat
    /// </summary>
    public void RegisterActor(IActor actor)
    {
        if (actor == null) return;
        
        if (actor.IsPlayerTeam)
        {
            if (!playerTeamActors.Contains(actor))
                playerTeamActors.Add(actor);
        }
        else
        {
            if (!enemyTeamActors.Contains(actor))
                enemyTeamActors.Add(actor);
        }
    }
    
    /// <summary>
    /// Unregisters an actor from combat
    /// </summary>
    public void UnregisterActor(IActor actor)
    {
        if (actor == null) return;
        
        playerTeamActors.Remove(actor);
        enemyTeamActors.Remove(actor);
        forcedTargets.Remove(actor);
    }
    
    /// <summary>
    /// Gets all enemies of the specified actor
    /// </summary>
    public List<IActor> GetEnemies(IActor actor)
    {
        if (actor == null) return new List<IActor>();
        
        return actor.IsPlayerTeam ? enemyTeamActors.ToList() : playerTeamActors.ToList();
    }
    
    /// <summary>
    /// Gets all allies of the specified actor
    /// </summary>
    public List<IActor> GetAllies(IActor actor)
    {
        if (actor == null) return new List<IActor>();
        
        var allies = actor.IsPlayerTeam ? playerTeamActors.ToList() : enemyTeamActors.ToList();
        allies.Remove(actor); // Remove self from allies list
        return allies;
    }
    
    /// <summary>
    /// Gets all actors currently in combat
    /// </summary>
    public List<IActor> GetAllCombatants()
    {
        var all = new List<IActor>();
        all.AddRange(playerTeamActors);
        all.AddRange(enemyTeamActors);
        return all;
    }
    
    /// <summary>
    /// Forces an actor to target another actor for a duration
    /// </summary>
    public void ForceTarget(IActor source, IActor target, float duration = 0f)
    {
        if (source == null || target == null) return;
        
        // Set the target
        source.CurrentTarget = target;
        
        // If source implements ITargetable, use its ForceTarget method
        if (source is ITargetable targetable)
        {
            targetable.ForceTarget(target, duration);
        }
        
        // Track forced targeting if duration is specified
        if (duration > 0f)
        {
            forcedTargets[source] = new ForcedTarget(target, duration);
        }
        else
        {
            forcedTargets[source] = new ForcedTarget(target, 0f);
        }
    }
    
    /// <summary>
    /// Clears forced targeting for an actor
    /// </summary>
    public void ClearForcedTarget(IActor actor)
    {
        if (actor == null) return;
        
        forcedTargets.Remove(actor);
        
        if (actor is ITargetable targetable)
        {
            targetable.ClearTarget();
        }
        else
        {
            actor.CurrentTarget = null;
        }
    }
    
    /// <summary>
    /// Checks if an actor is currently being forced to target someone
    /// </summary>
    public bool IsTargetForced(IActor actor)
    {
        return actor != null && forcedTargets.ContainsKey(actor);
    }
    
    /// <summary>
    /// Clears all combat state (useful for resetting between battles)
    /// </summary>
    public void ClearCombat()
    {
        playerTeamActors.Clear();
        enemyTeamActors.Clear();
        forcedTargets.Clear();
    }
}

