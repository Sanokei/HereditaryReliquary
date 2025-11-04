using UnityEngine;

/// <summary>
/// Brute class - Tank-focused class that excels at drawing enemy attention
/// Specializes in taunt abilities and high defense
/// </summary>
[System.Serializable]
public class BruteClass : AdventurerClass
{
    [Header("Brute-Specific Stats")]
    [Tooltip("Additional health bonus for Brutes")]
    public int healthBonus = 50;
    
    [Tooltip("Additional defense bonus for Brutes")]
    public int defenseBonus = 10;
    
    [Tooltip("Taunt effectiveness multiplier")]
    [Range(1f, 3f)]
    public float tauntEffectiveness = 1.5f;
    
    public BruteClass() : base(AdventurerClassType.Brute)
    {
        className = "Brute";
        classDescription = "A fierce warrior who excels at drawing enemy attention and protecting allies through intimidation and resilience.";
    }
    
    protected override void InitializeClass()
    {
        base.InitializeClass();
        
        // Brute-specific initialization
        baseHealth += healthBonus;
        baseDefense += defenseBonus;
        
        // Brutes are naturally tanky
        baseHealth = Mathf.Max(baseHealth, 150);
        baseDefense = Mathf.Max(baseDefense, 15);
    }
    
    public override float GetClassBonus(string bonusType)
    {
        switch (bonusType.ToLower())
        {
            case "taunt":
            case "taunteffectiveness":
                return tauntEffectiveness;
            case "defense":
            case "defensebonus":
                return defenseBonus;
            case "health":
            case "healthbonus":
                return healthBonus;
            default:
                return base.GetClassBonus(bonusType);
        }
    }
}

