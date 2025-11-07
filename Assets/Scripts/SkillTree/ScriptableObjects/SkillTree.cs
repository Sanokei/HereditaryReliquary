using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Skill Tree", menuName = "Adventurers/Skill Tree")]
public class SkillTree : ScriptableObject
{   
    [Header("Nodes")]
    public List<SkillNode> nodes = new List<SkillNode>();
    
    public SkillNode GetNode(string id)
    {
        return nodes.Find(n => n.id == id);
    }
    
    public void AddNode(SkillNode node)
    {
        if (!nodes.Contains(node))
        {
            nodes.Add(node);
        }
    }
    
    public void RemoveNode(string id)
    {
        // Remove node
        nodes.RemoveAll(n => n.id == id);
        
        // Remove references to this node from other nodes
        foreach (var node in nodes)
        {
            node.prerequisites.Remove(id);
        }
    }
    
    /// <summary>
    /// Checks if adding a prerequisite would create a cycle in the skill tree
    /// </summary>
    /// <param name="fromNodeId">The node that would become a prerequisite</param>
    /// <param name="toNodeId">The node that would require the prerequisite</param>
    /// <returns>True if adding this prerequisite would create a cycle</returns>
    public bool WouldCreateCycle(string fromNodeId, string toNodeId)
    {
        // We want to add: fromNodeId -> toNodeId (toNodeId will require fromNodeId)
        // This means: toNodeId.prerequisites will include fromNodeId
        
        // A cycle would occur if:
        // 1. toNodeId already requires fromNodeId (directly or indirectly) - creates forward cycle
        // 2. fromNodeId already requires toNodeId (directly or indirectly) - creates bidirectional cycle (A <-> B)
        
        // Example 1: A already requires B, we try to add B -> A
        //   - Current: A.prerequisites = [B] (A requires B)
        //   - Adding: B.prerequisites = [A] (B requires A)
        //   - Result: A <-> B (cycle!)
        //   - Check 1: Does A already require B? Yes!
        //   - Check 2: Does B already require A? No (not yet), but after adding it would
        
        // Example 2: B already requires A, we try to add A -> B  
        //   - Current: B.prerequisites = [A] (B requires A)
        //   - Adding: A.prerequisites = [B] (A requires B)
        //   - Result: A <-> B (cycle!)
        //   - Check 1: Does A already require B? No
        //   - Check 2: Does B already require A? Yes!
        
        HashSet<string> visited = new HashSet<string>();
        
        // Check if toNodeId already requires fromNodeId
        bool toNodeRequiresFromNode = HasPathTo(toNodeId, fromNodeId, visited);
        
        // Check if fromNodeId already requires toNodeId (prevents A <-> B)
        visited.Clear();
        bool fromNodeRequiresToNode = HasPathTo(fromNodeId, toNodeId, visited);
        
        return toNodeRequiresFromNode || fromNodeRequiresToNode;
    }
    
    /// <summary>
    /// Checks if there's a path from startNodeId to targetNodeId by following prerequisites
    /// If startNodeId has prerequisite X, and X has prerequisite Y, and Y == targetNodeId,
    /// then there's a path: startNodeId requires X, X requires Y (targetNodeId)
    /// </summary>
    private bool HasPathTo(string startNodeId, string targetNodeId, HashSet<string> visited)
    {
        // Prevent infinite loops in case of existing cycles
        if (visited.Contains(startNodeId))
        {
            return false;
        }
        
        // If we found the target, there's a path
        if (startNodeId == targetNodeId)
        {
            return true;
        }
        
        visited.Add(startNodeId);
        
        // Check all prerequisites of startNodeId
        // If startNodeId requires X, and X (or X's prerequisites) can reach targetNodeId, then there's a path
        var node = GetNode(startNodeId);
        if (node != null && node.prerequisites != null)
        {
            foreach (var prereqId in node.prerequisites)
            {
                if (HasPathTo(prereqId, targetNodeId, visited))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if adding a prerequisite would make any existing prerequisites redundant
    /// </summary>
    /// <param name="fromNodeId">The node that would become a prerequisite</param>
    /// <param name="toNodeId">The node that would require the prerequisite</param>
    /// <returns>List of prerequisite IDs that would become redundant, or null if none</returns>
    public List<string> WouldMakePrerequisitesRedundant(string fromNodeId, string toNodeId)
    {
        var toNode = GetNode(toNodeId);
        if (toNode == null || toNode.prerequisites == null || toNode.prerequisites.Count == 0)
        {
            return null;
        }
        
        List<string> redundantPrereqs = new List<string>();
        HashSet<string> visited = new HashSet<string>();
        
        // Check each existing prerequisite of toNodeId
        foreach (var existingPrereqId in toNode.prerequisites)
        {
            // Skip if it's the same as what we're adding
            if (existingPrereqId == fromNodeId)
            {
                continue;
            }
            
            // A prerequisite becomes redundant if:
            // 1. fromNodeId can reach existingPrereqId through prerequisites (fromNodeId -> ... -> existingPrereqId)
            //    Example: A -> C exists, we add B -> C, and B -> A exists, then A -> C is redundant
            //    Because B -> A -> C means C already requires A through B
            // OR
            // 2. existingPrereqId can reach fromNodeId through prerequisites (existingPrereqId -> ... -> fromNodeId)
            //    Example: A -> C exists, we add B -> C, and A -> B exists, then A -> C is redundant
            //    Because A -> B -> C means C already requires A through B
            
            visited.Clear();
            bool fromNodeReachesExisting = HasPathTo(fromNodeId, existingPrereqId, visited);
            
            visited.Clear();
            bool existingReachesFromNode = HasPathTo(existingPrereqId, fromNodeId, visited);
            
            if (fromNodeReachesExisting || existingReachesFromNode)
            {
                redundantPrereqs.Add(existingPrereqId);
            }
        }
        
        return redundantPrereqs.Count > 0 ? redundantPrereqs : null;
    }
    
    /// <summary>
    /// Adds a prerequisite, but only if it wouldn't create a cycle. Automatically removes redundant prerequisites.
    /// </summary>
    /// <param name="fromNodeId">The node that becomes a prerequisite</param>
    /// <param name="toNodeId">The node that requires the prerequisite</param>
    /// <returns>True if the prerequisite was added, false if it would create a cycle</returns>
    public bool AddPrerequisite(string fromNodeId, string toNodeId)
    {
        // Prevent self-loops
        if (fromNodeId == toNodeId)
        {
            return false;
        }
        
        var toNode = GetNode(toNodeId);
        if (toNode == null)
        {
            return false;
        }
        
        // Check if already exists
        if (toNode.prerequisites != null && toNode.prerequisites.Contains(fromNodeId))
        {
            return true; // Already exists, consider it successful
        }
        
        // Check if it would create a cycle
        if (WouldCreateCycle(fromNodeId, toNodeId))
        {
            Debug.LogWarning($"Cannot add prerequisite {fromNodeId} -> {toNodeId}: This would create a cycle!");
            return false;
        }
        
        // Check if it would make prerequisites redundant and remove them automatically
        var redundantPrereqs = WouldMakePrerequisitesRedundant(fromNodeId, toNodeId);
        if (redundantPrereqs != null && redundantPrereqs.Count > 0)
        {
            // Remove redundant prerequisites automatically
            foreach (var redundantId in redundantPrereqs)
            {
                toNode.prerequisites.Remove(redundantId);
                Debug.Log($"Removed redundant prerequisite {redundantId} -> {toNodeId} (superseded by {fromNodeId} -> {toNodeId})");
            }
        }
        
        // Add the prerequisite
        if (toNode.prerequisites == null)
        {
            toNode.prerequisites = new List<string>();
        }
        toNode.prerequisites.Add(fromNodeId);
        return true;
    }
    
    public void RemovePrerequisite(string fromNodeId, string toNodeId)
    {
        var toNode = GetNode(toNodeId);
        if (toNode != null)
        {
            toNode.prerequisites.Remove(fromNodeId);
        }
    }
}

