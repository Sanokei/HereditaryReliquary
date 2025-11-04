using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Heal node - applies healing to target actor(s)
/// </summary>
[System.Serializable]
public class HealNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string TARGET_INPUT = "target_in";
    private const string AMOUNT_INPUT = "amount_in";
    private const string HEALING_DONE_OUTPUT = "healing_done_out";
    
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private bool useInputAmount = false;
    [SerializeField] private HealingType healingType = HealingType.Instant;
    [SerializeField] private bool canOverheal = false;
    [SerializeField] private float maxHealPercent = 1f;
    [SerializeField] private bool outputHealingDone = false;
    
    public HealNode() : base()
    {
        SetTitle("Heal");
        SetDescription("Applies healing to target");
        healAmount = 10f;
        useInputAmount = false;
        healingType = HealingType.Instant;
        canOverheal = false;
        maxHealPercent = 1f;
        outputHealingDone = false;
    }
    
    public HealNode(Vector2 pos) : base(pos)
    {
        SetTitle("Heal");
        SetDescription("Applies healing to target");
        healAmount = 10f;
        useInputAmount = false;
        healingType = HealingType.Instant;
        canOverheal = false;
        maxHealPercent = 1f;
        outputHealingDone = false;
    }
    
    public float HealAmount
    {
        get => healAmount;
        set
        {
            if (value < 0) value = 0;
            healAmount = value;
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
    
    public HealingType HealingType
    {
        get => healingType;
        set => healingType = value;
    }
    
    public bool CanOverheal
    {
        get => canOverheal;
        set => canOverheal = value;
    }
    
    public float MaxHealPercent
    {
        get => maxHealPercent;
        set
        {
            if (value < 0) value = 0;
            if (value > 1) value = 1;
            maxHealPercent = value;
        }
    }
    
    public bool OutputHealingDone
    {
        get => outputHealingDone;
        set
        {
            if (outputHealingDone != value)
            {
                outputHealingDone = value;
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
        
        if (outputHealingDone)
        {
            pins.Add(new NodePin(HEALING_DONE_OUTPUT, "Healing Done", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
}

[System.Serializable]
public enum HealingType
{
    Instant,
    OverTime,
    Percentage
}

