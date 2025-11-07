using UnityEngine;
using System.Collections.Generic;
using SkillSystem.Core;

[System.Serializable]
public class SkillNode
{
    public string id;
    public SSSkill skill; // Public for editor access, still using Skill as the concrete type for ScriptableObject reference
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
    /// Gets the skill as SSSkill interface
    /// </summary>
    public SSSkill GetSkill() => skill;
    
    /// <summary>
    /// Sets the skill asset
    /// </summary>
    public void SetSkill(SSSkill newSkill) => skill = newSkill;
}

