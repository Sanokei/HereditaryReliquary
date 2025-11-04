using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SkillNode
{
    public string id;
    public Skill skill; // Public for editor access, still using Skill as the concrete type for ScriptableObject reference
    public Vector2 position;
    public List<string> prerequisites = new List<string>(); // IDs of prerequisite nodes
    
    public SkillNode()
    {
        id = System.Guid.NewGuid().ToString();
        position = Vector2.zero;
    }
    
    public SkillNode(Vector2 pos)
    {
        id = System.Guid.NewGuid().ToString();
        position = pos;
    }
    
    /// <summary>
    /// Gets the skill as ISkill interface
    /// </summary>
    public ISkill GetSkill() => skill;
    
    /// <summary>
    /// Gets the skill as the concrete Skill type
    /// </summary>
    public Skill GetSkillAsset() => skill;
    
    /// <summary>
    /// Sets the skill asset
    /// </summary>
    public void SetSkill(Skill newSkill) => skill = newSkill;
}

