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
public class Skill : ScriptableObject, ISkill, ICooldownSkill
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
    
    [Header("Cooldown")]
    [SerializeField] private bool requiresCooldown = true;
    [SerializeField] private float cooldownDuration = 5f;
    
    // Cooldown state (runtime only, not serialized)
    private float cooldownEndTime = 0f;
    
    // ISkill implementation
    public string SkillName => skillName;
    public string Description => description;
    
    // ICooldownSkill implementation
    public bool IsCoolingDown => requiresCooldown && Time.time < cooldownEndTime;
    public float RemainingCooldown => IsCoolingDown ? Mathf.Max(0f, cooldownEndTime - Time.time) : 0f;
    public float CooldownDuration => cooldownDuration;
    
    public void StartCooldown()
    {
        if (requiresCooldown)
        {
            cooldownEndTime = Time.time + cooldownDuration;
        }
    }
    
    /// <summary>
    /// Validates that the skill can be performed (checks cooldown and null performer)
    /// Returns true if validation passes, false otherwise
    /// </summary>
    protected virtual bool ValidatePerform(IActor performer)
    {
        // Check cooldown if enabled
        if (requiresCooldown && IsCoolingDown)
        {
            Debug.LogWarning($"{skillName} is on cooldown. Remaining: {RemainingCooldown:F1}s");
            return false;
        }
        
        // Check if performer is null
        if (performer == null)
        {
            Debug.LogError($"{skillName}: Performer is null!");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Performs the skill with the given actor
    /// Override this method in derived classes to implement custom skill behavior
    /// </summary>
    public virtual void Perform(IActor performer)
    {
        // Validate before performing
        if (!ValidatePerform(performer))
        {
            return;
        }
        
        
        // Start cooldown after performing
        StartCooldown();
        
        Debug.Log($"{skillName} performed by {performer.Name}");
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
        
        // Cooldown info
        if (requiresCooldown)
        {
            var cooldownLabel = new Label($"Cooldown: {cooldownDuration}s");
            cooldownLabel.style.marginTop = 5;
            rootElement.Add(cooldownLabel);
        }
        
        return rootElement;
    }

#endif
}