using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Apply Status node - applies status effects/buffs/debuffs to target
/// </summary>
[System.Serializable]
public class ApplyStatusNode : SkillGraphNode
{
    private const string EXEC_INPUT = "exec_in";
    private const string EXEC_OUTPUT = "exec_out";
    private const string TARGET_INPUT = "target_in";
    private const string DURATION_INPUT = "duration_in";
    private const string STATUS_APPLIED_OUTPUT = "status_applied_out";
    
    [SerializeField] private string statusName = "Buff";
    [SerializeField] private StatusEffectType statusType = StatusEffectType.Buff;
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool useInputDuration = false;
    [SerializeField] private int stackCount = 1;
    [SerializeField] private bool canStack = false;
    [SerializeField] private int maxStacks = 1;
    [SerializeField] private bool outputStatusApplied = false;
    
    public ApplyStatusNode() : base()
    {
        SetTitle("Apply Status");
        SetDescription("Applies status effect to target");
        statusName = "Buff";
        statusType = StatusEffectType.Buff;
        duration = 5f;
        useInputDuration = false;
        stackCount = 1;
        canStack = false;
        maxStacks = 1;
        outputStatusApplied = false;
    }
    
    public ApplyStatusNode(Vector2 pos) : base(pos)
    {
        SetTitle("Apply Status");
        SetDescription("Applies status effect to target");
        statusName = "Buff";
        statusType = StatusEffectType.Buff;
        duration = 5f;
        useInputDuration = false;
        stackCount = 1;
        canStack = false;
        maxStacks = 1;
        outputStatusApplied = false;
    }
    
    public string StatusName
    {
        get => statusName;
        set => statusName = value;
    }
    
    public StatusEffectType StatusType
    {
        get => statusType;
        set => statusType = value;
    }
    
    public float Duration
    {
        get => duration;
        set
        {
            if (value < 0) value = 0;
            duration = value;
        }
    }
    
    public bool UseInputDuration
    {
        get => useInputDuration;
        set
        {
            if (useInputDuration != value)
            {
                useInputDuration = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public int StackCount
    {
        get => stackCount;
        set
        {
            if (value < 1) value = 1;
            stackCount = value;
        }
    }
    
    public bool CanStack
    {
        get => canStack;
        set => canStack = value;
    }
    
    public int MaxStacks
    {
        get => maxStacks;
        set
        {
            if (value < 1) value = 1;
            maxStacks = value;
        }
    }
    
    public bool OutputStatusApplied
    {
        get => outputStatusApplied;
        set
        {
            if (outputStatusApplied != value)
            {
                outputStatusApplied = value;
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
        
        if (useInputDuration)
        {
            pins.Add(new NodePin(DURATION_INPUT, "Duration", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>
        {
            new NodePin(EXEC_OUTPUT, "Complete", PinType.Execution, PinDirection.Output)
        };
        
        if (outputStatusApplied)
        {
            pins.Add(new NodePin(STATUS_APPLIED_OUTPUT, "Status Applied", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Bool));
        }
        
        return pins;
    }
}

[System.Serializable]
public enum StatusEffectType
{
    Buff,
    Debuff,
    Dot,
    Hot,
    Stun,
    Slow,
    Speed,
    Shield
}

