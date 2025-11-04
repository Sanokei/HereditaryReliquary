using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Skill Tree", menuName = "Adventurers/Skill Tree")]
public class SkillTree : ScriptableObject
{
    [Header("Tree Information")]
    public string treeName;
    public SkillType baseSkillType;
    
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
    
    public void AddPrerequisite(string fromNodeId, string toNodeId)
    {
        var toNode = GetNode(toNodeId);
        if (toNode != null && !toNode.prerequisites.Contains(fromNodeId))
        {
            toNode.prerequisites.Add(fromNodeId);
        }
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

