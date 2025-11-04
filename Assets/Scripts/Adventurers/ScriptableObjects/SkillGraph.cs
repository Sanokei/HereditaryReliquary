using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SkillGraph represents a node-based skill definition that can be edited visually
/// and executed at runtime. Each skill will have its own SkillGraph.
/// </summary>
[CreateAssetMenu(fileName = "New Skill Graph", menuName = "Adventurers/Skill Graph")]
public class SkillGraph : ScriptableObject
{
    [Header("Nodes")]
    [SerializeField] private List<SkillGraphNode> nodes = new List<SkillGraphNode>();
    
    [Header("Entry Point")]
    [SerializeField] private string entryNodeId; // ID of the node where execution starts
    
    /// <summary>
    /// Gets all nodes in this graph
    /// </summary>
    public List<SkillGraphNode> Nodes
    {
        get
        {
            // Ensure connection caches are built when accessing nodes
            RebuildAllConnectionCaches();
            return nodes;
        }
    }
    
    /// <summary>
    /// Rebuilds connection caches for all nodes in the graph
    /// This should be called when the graph is loaded or modified
    /// </summary>
    public void RebuildAllConnectionCaches()
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                node.RebuildCacheIfNeeded();
            }
        }
    }
    
    /// <summary>
    /// Gets the entry node ID (where execution starts)
    /// </summary>
    public string EntryNodeId => entryNodeId;
    
    /// <summary>
    /// Gets the entry node
    /// </summary>
    public SkillGraphNode GetEntryNode()
    {
        if (string.IsNullOrEmpty(entryNodeId))
            return null;
            
        return nodes.Find(n => n.Id == entryNodeId);
    }
    
    /// <summary>
    /// Gets a node by its ID
    /// </summary>
    public SkillGraphNode GetNode(string id)
    {
        return nodes.Find(n => n.Id == id);
    }
    
    /// <summary>
    /// Adds a node to the graph
    /// </summary>
    public void AddNode(SkillGraphNode node)
    {
        if (node != null && !nodes.Contains(node))
        {
            nodes.Add(node);
            node.RebuildCacheIfNeeded();
            
            // If this is the first node, make it the entry point
            if (nodes.Count == 1 && string.IsNullOrEmpty(entryNodeId))
            {
                entryNodeId = node.Id;
            }
        }
    }
    
    /// <summary>
    /// Removes a node from the graph
    /// </summary>
    public void RemoveNode(string id)
    {
        var node = GetNode(id);
        if (node != null)
        {
            // Remove all connections to/from this node
            foreach (var otherNode in nodes)
            {
                if (otherNode != node)
                {
                    otherNode.RemoveConnectionsToNode(id);
                }
            }
            
            nodes.Remove(node);
            
            // If we removed the entry node, set a new one (first node) or clear it
            if (entryNodeId == id)
            {
                entryNodeId = nodes.Count > 0 ? nodes[0].Id : null;
            }
        }
    }
    
    /// <summary>
    /// Sets the entry node for this graph
    /// </summary>
    public void SetEntryNode(string nodeId)
    {
        if (GetNode(nodeId) != null)
        {
            entryNodeId = nodeId;
        }
    }
    
    /// <summary>
    /// Adds a connection between two nodes
    /// </summary>
    public bool AddConnection(string fromNodeId, string fromPinId, string toNodeId, string toPinId)
    {
        var fromNode = GetNode(fromNodeId);
        if (fromNode == null)
            return false;
        
        return fromNode.AddConnection(fromPinId, this, toNodeId, toPinId);
    }
    
    /// <summary>
    /// Removes a connection between two nodes
    /// </summary>
    public bool RemoveConnection(string fromNodeId, string fromPinId, string toNodeId, string toPinId)
    {
        var fromNode = GetNode(fromNodeId);
        if (fromNode == null)
            return false;
        
        return fromNode.RemoveConnection(fromPinId, toNodeId, toPinId);
    }
    
    /// <summary>
    /// Graph name for display purposes
    /// </summary>
    public string GraphName => name;
    
    /// <summary>
    /// Graph description
    /// </summary>
    public string Description => string.Empty;
}

