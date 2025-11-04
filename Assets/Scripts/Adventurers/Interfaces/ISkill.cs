/// <summary>
/// Base interface for all skills that can be performed
/// </summary>
public interface ISkill
{
    /// <summary>
    /// Performs the skill with the given actor
    /// </summary>
    void Perform(IActor performer);
    
    /// <summary>
    /// Name of the skill
    /// </summary>
    string SkillName { get; }
    
    /// <summary>
    /// Description of the skill
    /// </summary>
    string Description { get; }
}

