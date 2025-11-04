using UnityEngine;

/// <summary>
/// Utility class for performing skills with proper interface checking
/// </summary>
public static class SkillPerformer
{
    /// <summary>
    /// Attempts to perform a skill, checking for cooldowns and other conditions
    /// </summary>
    /// <param name="skill">The skill to perform</param>
    /// <param name="performer">The actor performing the skill</param>
    /// <returns>True if the skill was performed successfully, false otherwise</returns>
    public static bool Perform(ISkill skill, IActor performer)
    {
        if (skill == null || performer == null)
        {
            Debug.LogWarning("Cannot perform skill: skill or performer is null");
            return false;
        }
        
        // Check cooldown if skill implements ICooldownSkill
        if (skill is ICooldownSkill cooldownSkill && cooldownSkill.IsCoolingDown)
        {
            Debug.Log($"{skill.SkillName} is on cooldown. Remaining: {cooldownSkill.RemainingCooldown:F1}s");
            return false;
        }
        
        // Perform the skill
        skill.Perform(performer);
        
        // Start cooldown if applicable
        if (skill is ICooldownSkill cdSkill)
        {
            cdSkill.StartCooldown();
        }
        
        return true;
    }
}

