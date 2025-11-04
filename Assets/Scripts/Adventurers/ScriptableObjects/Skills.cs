using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine.UIElements;
#endif

[System.Serializable]
public enum SkillType
{
    Combat,
    Magic,
    Stealth,
    Crafting,
    Social,
    Survival
}

[CreateAssetMenu(fileName = "New Skill", menuName = "Adventurers/Skill")]
public class Skill : ScriptableObject, ISkill
#if UNITY_EDITOR
    , IGUIVisualizable
#endif
{
    [Header("Basic Information")]
    [SerializeField] private string skillName;
    [TextArea(3, 5)]
    [SerializeField] private string description;
    public SkillType skillType;
    public Sprite icon;
    
    [Header("Skill Points")]
    [Range(1, 100)]
    public int skillPointsRequired = 1;
    
    [Header("Skill Graph")]
    [SerializeField, HideInInspector] private SkillGraph skillGraph;
    
    [Header("Effects (Legacy - Deprecated)")]
    [SerializeField] private SkillEffect[] effects = new SkillEffect[0];
    
    // ISkill implementation
    public string SkillName => skillName;
    public string Description => description;
    
    /// <summary>
    /// Performs the skill with the given actor
    /// </summary>
    public virtual void Perform(IActor performer)
    {
        // Use graph-based system if available
        if (skillGraph != null)
        {
            SkillGraphExecutor.Execute(skillGraph, performer);
            Debug.Log($"{skillName} performed by {performer.Name}");
            return;
        }
        
        // Fall back to legacy effects system
        if (effects == null || effects.Length == 0)
        {
            Debug.Log($"{skillName} performed by {performer.Name}, but has no effects or graph.");
            return;
        }
        
        foreach (var effect in effects)
        {
            if (effect != null)
            {
                effect.Apply(performer);
            }
        }
        
        Debug.Log($"{skillName} performed by {performer.Name}");
    }
    
    /// <summary>
    /// Gets the skill graph
    /// </summary>
    public SkillGraph GetSkillGraph() => skillGraph;
    
    /// <summary>
    /// Sets the skill graph
    /// </summary>
    public void SetSkillGraph(SkillGraph graph)
    {
        skillGraph = graph;
    }
    
    /// <summary>
    /// Gets all effects of this skill (legacy system)
    /// </summary>
    [System.Obsolete("Use SkillGraph instead. This method is for backward compatibility only.")]
    public SkillEffect[] GetEffects() => effects;
    
    /// <summary>
    /// Sets the effects for this skill (legacy system)
    /// </summary>
    [System.Obsolete("Use SkillGraph instead. This method is for backward compatibility only.")]
    public void SetEffects(SkillEffect[] newEffects)
    {
        effects = newEffects;
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Creates a GUI representation of this skill
    /// </summary>
    public virtual VisualElement CreateGUI()
    {
        return CreateDefaultGUI();
    }
    
    /// <summary>
    /// Creates the default GUI for this skill
    /// </summary>
    protected VisualElement CreateDefaultGUI()
    {
        var rootElement = new VisualElement();
        rootElement.name = "SkillGUI";
        
        // Skill name
        var nameLabel = new Label(skillName);
        nameLabel.style.fontSize = 16;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        rootElement.Add(nameLabel);
        
        // Description
        if (!string.IsNullOrEmpty(description))
        {
            var descLabel = new Label(description);
            descLabel.style.marginTop = 5;
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            rootElement.Add(descLabel);
        }
        
        // Skill points required
        var pointsLabel = new Label($"Skill Points Required: {skillPointsRequired}");
        pointsLabel.style.marginTop = 5;
        rootElement.Add(pointsLabel);
        
        // Skill Graph
        if (skillGraph != null)
        {
            var graphContainer = new VisualElement();
            graphContainer.name = "GraphContainer";
            graphContainer.style.marginTop = 10;
            
            var graphHeader = new Label("Skill Graph:");
            graphHeader.style.fontSize = 14;
            graphHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            graphContainer.Add(graphHeader);
            
            var graphLabel = new Label($"• {skillGraph.GraphName} ({skillGraph.Nodes.Count} nodes)");
            graphLabel.style.marginLeft = 10;
            graphContainer.Add(graphLabel);
            
            rootElement.Add(graphContainer);
        }
        
        // Effects (Legacy)
        if (effects != null && effects.Length > 0)
        {
            var effectsContainer = new VisualElement();
            effectsContainer.name = "EffectsContainer";
            effectsContainer.style.marginTop = 10;
            
            var effectsHeader = new Label("Effects:");
            effectsHeader.style.fontSize = 14;
            effectsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            effectsContainer.Add(effectsHeader);
            
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    if (TryCreateEffectGUI(effect, out var effectGUI))
                    {
                        effectsContainer.Add(effectGUI);
                    }
                    else
                    {
                        // Fallback to description
                        var effectLabel = new Label($"• {effect.GetDescription()}");
                        effectLabel.style.marginLeft = 10;
                        effectsContainer.Add(effectLabel);
                    }
                }
            }
            
            rootElement.Add(effectsContainer);
        }
        
        return rootElement;
    }
    
    /// <summary>
    /// Attempts to create GUI for a skill effect
    /// </summary>
    protected virtual bool TryCreateEffectGUI(SkillEffect effect, out VisualElement effectGUI)
    {
#if UNITY_EDITOR
        if (effect is ISkillEffectVisualizable visualizable)
        {
            effectGUI = visualizable.CreateGUI();
            return effectGUI != null;
        }
#endif
        
        effectGUI = null;
        return false;
    }
#endif
}
