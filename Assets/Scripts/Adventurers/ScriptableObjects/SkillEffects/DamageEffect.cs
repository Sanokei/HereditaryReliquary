using UnityEngine;

/// <summary>
/// Example skill effect that deals damage
/// </summary>
[System.Serializable]
public class DamageEffect : SkillEffect
{
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private bool targetSelf = false;
    
    public override void Apply(IActor performer, IActor target = null)
    {
        IActor targetActor = targetSelf ? performer : target ?? performer;
        
        if (targetActor != null)
        {
            // Apply damage logic here
            Debug.Log($"{performer.Name} deals {damageAmount} damage to {targetActor.Name}");
        }
    }
    
    public override string GetDescription()
    {
        string target = targetSelf ? "self" : "target";
        return $"Deals {damageAmount} damage to {target}";
    }
}

