using UnityEngine;

/// <summary>
/// Skill variant that implements cooldown functionality
/// Inherit from this or add ICooldownSkill to your custom Skill implementation
/// </summary>
[CreateAssetMenu(fileName = "New Skill (With Cooldown)", menuName = "Adventurers/Skill With Cooldown")]
public class SkillWithCooldown : Skill, ICooldownSkill
{
    [Header("Cooldown")]
    [SerializeField] private float cooldownDuration = 5f;
    
    private float cooldownEndTime = 0f;
    
    public bool IsCoolingDown => Time.time < cooldownEndTime;
    
    public float RemainingCooldown => IsCoolingDown ? Mathf.Max(0f, cooldownEndTime - Time.time) : 0f;
    
    public float CooldownDuration => cooldownDuration;
    
    public void StartCooldown()
    {
        cooldownEndTime = Time.time + cooldownDuration;
    }
    
    public override void Perform(IActor performer)
    {
        if (IsCoolingDown)
        {
            Debug.LogWarning($"{SkillName} is on cooldown. Remaining: {RemainingCooldown:F1}s");
            return;
        }
        
        base.Perform(performer);
        StartCooldown();
    }
}

