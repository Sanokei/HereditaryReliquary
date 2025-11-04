using UnityEngine;

/// <summary>
/// Example skill effect that heals
/// </summary>
[System.Serializable]
public class HealEffect : SkillEffect
{
    [SerializeField] private float healAmount = 15f;
    
    public override void Apply(IActor performer, IActor target = null)
    {
        IActor targetActor = target ?? performer;
        
        if (targetActor != null)
        {
            // Apply heal logic here
            Debug.Log($"{performer.Name} heals {targetActor.Name} for {healAmount} HP");
        }
    }
    
    public override string GetDescription()
    {
        return $"Heals for {healAmount} HP";
    }
}

