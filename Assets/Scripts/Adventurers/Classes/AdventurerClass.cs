using UnityEngine;

/// <summary>
/// Enumeration of adventurer class types
/// </summary>
public enum AdventurerClassType
{
    Brute,
    Mage,
    Rogue,
    Warrior,
    Ranger,
    Cleric,
    Paladin,
    Assassin
}

/// <summary>
/// Base class for adventurer classes
/// Provides common properties and methods for all adventurer classes
/// </summary>
[System.Serializable]
public class AdventurerClass
{
    [Header("Class Information")]
    public AdventurerClassType classType;
    public string className;
    [TextArea(2, 4)]
    public string classDescription;
    
    [Header("Class Stats")]
    public int baseHealth = 100;
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseSpeed = 5;
    
    /// <summary>
    /// Gets the class type
    /// </summary>
    public AdventurerClassType ClassType => classType;
    
    /// <summary>
    /// Gets the class name
    /// </summary>
    public string ClassName => className;
    
    /// <summary>
    /// Constructor for creating an adventurer class
    /// </summary>
    public AdventurerClass(AdventurerClassType type)
    {
        classType = type;
        className = type.ToString();
        InitializeClass();
    }
    
    /// <summary>
    /// Initializes class-specific properties
    /// Override in derived classes for custom initialization
    /// </summary>
    protected virtual void InitializeClass()
    {
        // Base implementation - can be overridden
    }
    
    /// <summary>
    /// Gets class-specific bonuses or modifiers
    /// Override in derived classes for custom bonuses
    /// </summary>
    public virtual float GetClassBonus(string bonusType)
    {
        return 0f;
    }
}

