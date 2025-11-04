using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for all nodes in a SkillGraph.
/// Each node represents a single operation, action, or data point in the skill execution flow.
/// </summary>
[System.Serializable]
public abstract class SkillGraphNode
{
    [SerializeField] protected string id;
    [SerializeField] protected Vector2 position;
    [SerializeField] protected string nodeTitle;
    [SerializeField] protected string nodeDescription;
    
    // Connections: Serialized as a list for Unity compatibility
    [SerializeField] protected List<PinConnection> pinConnections = new List<PinConnection>();
    
    // Runtime cache of connections (not serialized)
    protected Dictionary<string, List<NodeConnection>> connectionCache;
    
    /// <summary>
    /// Unique identifier for this node
    /// </summary>
    public string Id => id;
    
    /// <summary>
    /// Position of this node in the graph editor
    /// </summary>
    public Vector2 Position
    {
        get => position;
        set => position = value;
    }
    
    /// <summary>
    /// Display title for this node
    /// </summary>
    public virtual string Title => string.IsNullOrEmpty(nodeTitle) ? GetType().Name : nodeTitle;
    
    /// <summary>
    /// Description of what this node does
    /// </summary>
    public virtual string Description => nodeDescription;
    
    /// <summary>
    /// Default constructor - generates a unique ID
    /// </summary>
    protected SkillGraphNode()
    {
        id = System.Guid.NewGuid().ToString();
        position = Vector2.zero;
        pinConnections = new List<PinConnection>();
        connectionCache = new Dictionary<string, List<NodeConnection>>();
    }
    
    /// <summary>
    /// Constructor with position
    /// </summary>
    protected SkillGraphNode(Vector2 pos)
    {
        id = System.Guid.NewGuid().ToString();
        position = pos;
        pinConnections = new List<PinConnection>();
        connectionCache = new Dictionary<string, List<NodeConnection>>();
    }
    
    /// <summary>
    /// Rebuilds the connection cache from serialized data
    /// </summary>
    protected void RebuildConnectionCache()
    {
        if (connectionCache == null)
            connectionCache = new Dictionary<string, List<NodeConnection>>();
        else
            connectionCache.Clear();
        
        foreach (var pinConn in pinConnections)
        {
            if (!connectionCache.ContainsKey(pinConn.FromPinId))
            {
                connectionCache[pinConn.FromPinId] = new List<NodeConnection>();
            }
            connectionCache[pinConn.FromPinId].Add(new NodeConnection(pinConn.ToNodeId, pinConn.ToPinId));
        }
    }
    
    /// <summary>
    /// Ensures connection cache is built
    /// </summary>
    protected void EnsureConnectionCache()
    {
        if (connectionCache == null)
        {
            RebuildConnectionCache();
        }
    }
    
    /// <summary>
    /// Public method to ensure connection cache is built (for external use)
    /// </summary>
    public void RebuildCacheIfNeeded()
    {
        EnsureConnectionCache();
    }
    
    /// <summary>
    /// Gets all input pins (execution and data) for this node
    /// </summary>
    public abstract List<NodePin> GetInputPins();
    
    /// <summary>
    /// Gets all output pins (execution and data) for this node
    /// </summary>
    public abstract List<NodePin> GetOutputPins();
    
    /// <summary>
    /// Adds a connection from this node's output pin to another node's input pin
    /// Validates type compatibility before connecting
    /// </summary>
    public virtual bool AddConnection(string fromPinId, SkillGraph graph, string toNodeId, string toPinId)
    {
        if (string.IsNullOrEmpty(fromPinId) || graph == null || string.IsNullOrEmpty(toNodeId) || string.IsNullOrEmpty(toPinId))
            return false;
        
        EnsureConnectionCache();
        
        // Validate that fromPinId exists in output pins
        var outputPins = GetOutputPins();
        var fromPin = NodePinHelper.GetPinById(outputPins, fromPinId);
        if (fromPin == null)
            return false;
        
        // Get target node and validate input pin
        var targetNode = graph.GetNode(toNodeId);
        if (targetNode == null)
            return false;
        
        var inputPins = targetNode.GetInputPins();
        var toPin = NodePinHelper.GetPinById(inputPins, toPinId);
        if (toPin == null)
            return false;
        
        // Validate type compatibility
        if (!NodePinHelper.CanConnectPins(fromPin, toPin))
        {
            Debug.LogWarning($"Cannot connect pins: Type mismatch between {fromPin.Name} ({NodePinHelper.GetTypeDisplayName(fromPin.DataType)}) and {toPin.Name} ({NodePinHelper.GetTypeDisplayName(toPin.DataType)})");
            return false;
        }
        
        // Check if connection already exists
        if (pinConnections.Exists(pc => pc.FromPinId == fromPinId && pc.ToNodeId == toNodeId && pc.ToPinId == toPinId))
            return false;
        
        // Add to serialized list
        pinConnections.Add(new PinConnection { FromPinId = fromPinId, ToNodeId = toNodeId, ToPinId = toPinId });
        
        // Update cache
        if (!connectionCache.ContainsKey(fromPinId))
        {
            connectionCache[fromPinId] = new List<NodeConnection>();
        }
        connectionCache[fromPinId].Add(new NodeConnection(toNodeId, toPinId));
        
        return true;
    }
    
    /// <summary>
    /// Adds a connection without validation (for internal use or when validation is already done)
    /// </summary>
    protected bool AddConnectionInternal(string fromPinId, string toNodeId, string toPinId)
    {
        if (string.IsNullOrEmpty(fromPinId) || string.IsNullOrEmpty(toNodeId) || string.IsNullOrEmpty(toPinId))
            return false;
        
        EnsureConnectionCache();
        
        // Check if connection already exists
        if (pinConnections.Exists(pc => pc.FromPinId == fromPinId && pc.ToNodeId == toNodeId && pc.ToPinId == toPinId))
            return false;
        
        // Add to serialized list
        pinConnections.Add(new PinConnection { FromPinId = fromPinId, ToNodeId = toNodeId, ToPinId = toPinId });
        
        // Update cache
        if (!connectionCache.ContainsKey(fromPinId))
        {
            connectionCache[fromPinId] = new List<NodeConnection>();
        }
        connectionCache[fromPinId].Add(new NodeConnection(toNodeId, toPinId));
        
        return true;
    }
    
    /// <summary>
    /// Removes a connection from this node
    /// </summary>
    public virtual bool RemoveConnection(string fromPinId, string toNodeId, string toPinId)
    {
        EnsureConnectionCache();
        
        bool removed = pinConnections.RemoveAll(pc => pc.FromPinId == fromPinId && pc.ToNodeId == toNodeId && pc.ToPinId == toPinId) > 0;
        
        if (removed && connectionCache.ContainsKey(fromPinId))
        {
            connectionCache[fromPinId].RemoveAll(c => c.NodeId == toNodeId && c.PinId == toPinId);
        }
        
        return removed;
    }
    
    /// <summary>
    /// Removes all connections to a specific node (used when deleting a node)
    /// </summary>
    public virtual void RemoveConnectionsToNode(string nodeId)
    {
        EnsureConnectionCache();
        
        pinConnections.RemoveAll(pc => pc.ToNodeId == nodeId);
        
        // Rebuild cache
        RebuildConnectionCache();
    }
    
    /// <summary>
    /// Removes all connections from a specific pin
    /// </summary>
    public virtual void RemoveConnectionsFromPin(string fromPinId)
    {
        EnsureConnectionCache();
        
        pinConnections.RemoveAll(pc => pc.FromPinId == fromPinId);
        
        // Rebuild cache
        RebuildConnectionCache();
    }
    
    /// <summary>
    /// Gets all connections from a specific output pin
    /// </summary>
    public virtual List<NodeConnection> GetConnections(string fromPinId)
    {
        EnsureConnectionCache();
        
        if (connectionCache.ContainsKey(fromPinId))
        {
            return new List<NodeConnection>(connectionCache[fromPinId]);
        }
        return new List<NodeConnection>();
    }
    
    /// <summary>
    /// Gets all connections from this node
    /// </summary>
    public virtual Dictionary<string, List<NodeConnection>> GetAllConnections()
    {
        EnsureConnectionCache();
        
        var result = new Dictionary<string, List<NodeConnection>>();
        foreach (var kvp in connectionCache)
        {
            result[kvp.Key] = new List<NodeConnection>(kvp.Value);
        }
        return result;
    }
    
    /// <summary>
    /// Sets the title for this node
    /// </summary>
    public virtual void SetTitle(string title)
    {
        nodeTitle = title;
    }
    
    /// <summary>
    /// Sets the description for this node
    /// </summary>
    public virtual void SetDescription(string desc)
    {
        nodeDescription = desc;
    }
    
    /// <summary>
    /// Validates and removes stale connections to pins that no longer exist
    /// This should be called when a node's pins change dynamically (e.g., when conditional pins are toggled)
    /// </summary>
    public virtual void ValidateAndCleanupConnections()
    {
        EnsureConnectionCache();
        
        // Get current valid pin IDs
        var currentInputPins = GetInputPins();
        var currentOutputPins = GetOutputPins();
        var validInputPinIds = new HashSet<string>(currentInputPins.Select(p => p.Id));
        var validOutputPinIds = new HashSet<string>(currentOutputPins.Select(p => p.Id));
        
        // Remove connections from output pins that no longer exist
        var pinsToRemove = new List<string>();
        foreach (var kvp in connectionCache)
        {
            if (!validOutputPinIds.Contains(kvp.Key))
            {
                pinsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var pinId in pinsToRemove)
        {
            RemoveConnectionsFromPin(pinId);
        }
        
        // Remove connections to input pins that no longer exist
        // We need to check all nodes and remove connections to this node's invalid input pins
        // This is typically handled by the graph, but we can validate our own connections
        var connectionsToRemove = new List<PinConnection>();
        foreach (var pinConn in pinConnections)
        {
            // Check if the output pin still exists
            if (!validOutputPinIds.Contains(pinConn.FromPinId))
            {
                connectionsToRemove.Add(pinConn);
            }
        }
        
        foreach (var conn in connectionsToRemove)
        {
            pinConnections.Remove(conn);
        }
        
        // Rebuild cache after cleanup
        RebuildConnectionCache();
    }
}

/// <summary>
/// Represents a connection between two nodes (runtime, not serialized)
/// </summary>
public class NodeConnection
{
    public string NodeId;
    public string PinId;
    
    public NodeConnection() { }
    
    public NodeConnection(string nodeId, string pinId)
    {
        NodeId = nodeId;
        PinId = pinId;
    }
}

/// <summary>
/// Serializable representation of a connection between pins
/// </summary>
[System.Serializable]
public class PinConnection
{
    public string FromPinId;
    public string ToNodeId;
    public string ToPinId;
}

/// <summary>
/// Represents a pin on a node (input or output, execution or data)
/// </summary>
[System.Serializable]
public class NodePin
{
    public string Id;
    public string Name;
    public PinType Type;
    public PinDirection Direction;
    public System.Type DataType;
    
    public NodePin(string id, string name, PinType type, PinDirection direction, System.Type dataType = null)
    {
        Id = id;
        Name = name;
        Type = type;
        Direction = direction;
        DataType = dataType;
    }
}

/// <summary>
/// Type of pin (execution flow or data)
/// </summary>
public enum PinType
{
    Execution,  // Flow control - connects execution flow
    Data        // Data - passes values between nodes
}

/// <summary>
/// Direction of pin (input or output)
/// </summary>
public enum PinDirection
{
    Input,
    Output
}

