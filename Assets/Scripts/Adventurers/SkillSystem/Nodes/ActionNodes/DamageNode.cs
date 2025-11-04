using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Damage node - applies damage to target actor(s)
/// </summary>
[System.Serializable]
public class DamageNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string TARGET_INPUT = "target_in";
    private const string AMOUNT_INPUT = "amount_in";
    private const string DAMAGE_DEALT_OUTPUT = "damage_dealt_out";
    
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private bool useInputAmount = false;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private bool canCrit = false;
    [SerializeField] private float critMultiplier = 2f;
    [SerializeField] private bool outputDamageDealt = false;
    
    public DamageNode() : base()
    {
        SetTitle("Damage");
        SetDescription("Applies damage to target");
        damageAmount = 10f;
        useInputAmount = false;
        damageType = DamageType.Physical;
        canCrit = false;
        critMultiplier = 2f;
        outputDamageDealt = false;
    }
    
    public DamageNode(Vector2 pos) : base(pos)
    {
        SetTitle("Damage");
        SetDescription("Applies damage to target");
        damageAmount = 10f;
        useInputAmount = false;
        damageType = DamageType.Physical;
        canCrit = false;
        critMultiplier = 2f;
        outputDamageDealt = false;
    }
    
    public float DamageAmount
    {
        get => damageAmount;
        set
        {
            if (value < 0) value = 0;
            damageAmount = value;
        }
    }
    
    public bool UseInputAmount
    {
        get => useInputAmount;
        set
        {
            if (useInputAmount != value)
            {
                useInputAmount = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public DamageType DamageType
    {
        get => damageType;
        set => damageType = value;
    }
    
    public bool CanCrit
    {
        get => canCrit;
        set => canCrit = value;
    }
    
    public float CritMultiplier
    {
        get => critMultiplier;
        set
        {
            if (value < 1) value = 1;
            critMultiplier = value;
        }
    }
    
    public bool OutputDamageDealt
    {
        get => outputDamageDealt;
        set
        {
            if (outputDamageDealt != value)
            {
                outputDamageDealt = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public override List<NodePin> GetInputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_INPUT, "Execute", PinType.Execution, PinDirection.Input),
            new NodePin(TARGET_INPUT, "Target", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Actor)
        };
        
        if (useInputAmount)
        {
            pins.Add(new NodePin(AMOUNT_INPUT, "Amount", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT, "Complete", PinType.Execution, PinDirection.Output)
        };
        
        if (outputDamageDealt)
        {
            pins.Add(new NodePin(DAMAGE_DEALT_OUTPUT, "Damage Dealt", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
}

[System.Serializable]
public enum DamageType
{
    Physical,
    Magic,
    Fire,
    Ice,
    Lightning,
    Poison,
    True
}

