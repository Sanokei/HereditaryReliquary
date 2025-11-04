using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

/// <summary>
/// Base class for skill effects that can be applied when a skill is performed
/// </summary>
[System.Serializable]
public abstract class SkillEffect
{
    public abstract void Apply(IActor performer, IActor target = null);
    
    public abstract string GetDescription();
}

#if UNITY_EDITOR
/// <summary>
/// Skill effects can optionally implement IGUIVisualizable for custom GUI representation
/// </summary>
public interface ISkillEffectVisualizable
{
    VisualElement CreateGUI();
}
#endif

