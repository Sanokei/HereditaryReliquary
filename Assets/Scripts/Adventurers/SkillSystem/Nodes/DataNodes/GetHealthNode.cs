using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Get Health node - gets the health value of an actor
/// </summary>
[System.Serializable]
public class GetHealthNode : SkillGraphNode
{
    private const string ACTOR_INPUT = "actor_in";
    private const string HEALTH_OUTPUT = "health_out";
    private const string MAX_HEALTH_OUTPUT = "max_health_out";
    private const string HEALTH_PERCENT_OUTPUT = "health_percent_out";
    
    [SerializeField] private HealthType healthType = HealthType.Current;
    [SerializeField] private bool outputMaxHealth = false;
    [SerializeField] private bool outputHealthPercent = false;
    
    public GetHealthNode() : base()
    {
        SetTitle("Get Health");
        SetDescription("Gets the health value of an actor");
        healthType = HealthType.Current;
        outputMaxHealth = false;
        outputHealthPercent = false;
    }
    
    public GetHealthNode(Vector2 pos) : base(pos)
    {
        SetTitle("Get Health");
        SetDescription("Gets the health value of an actor");
        healthType = HealthType.Current;
        outputMaxHealth = false;
        outputHealthPercent = false;
    }
    
    public HealthType HealthType
    {
        get => healthType;
        set => healthType = value;
    }
    
    public bool OutputMaxHealth
    {
        get => outputMaxHealth;
        set
        {
            if (outputMaxHealth != value)
            {
                outputMaxHealth = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public bool OutputHealthPercent
    {
        get => outputHealthPercent;
        set
        {
            if (outputHealthPercent != value)
            {
                outputHealthPercent = value;
                ValidateAndCleanupConnections();
            }
        }
    }
    
    public override List<NodePin> GetInputPins()
    {
        return new List<NodePin>
        {
            new NodePin(ACTOR_INPUT, "Actor", PinType.Data, PinDirection.Input, NodePinHelper.PinDataTypes.Actor)
        };
    }
    
    public override List<NodePin> GetOutputPins()
    {
        var pins = new List<NodePin>();
        
        switch (healthType)
        {
            case HealthType.Current:
                pins.Add(new NodePin(HEALTH_OUTPUT, "Current Health", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float));
                break;
            case HealthType.Max:
                pins.Add(new NodePin(HEALTH_OUTPUT, "Max Health", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float));
                break;
            case HealthType.Percent:
                pins.Add(new NodePin(HEALTH_OUTPUT, "Health Percent", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float));
                break;
        }
        
        if (outputMaxHealth && healthType != HealthType.Max)
        {
            pins.Add(new NodePin(MAX_HEALTH_OUTPUT, "Max Health", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float));
        }
        
        if (outputHealthPercent && healthType != HealthType.Percent)
        {
            pins.Add(new NodePin(HEALTH_PERCENT_OUTPUT, "Health Percent", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float));
        }
        
        return pins;
    }
}

[System.Serializable]
public enum HealthType
{
    Current,
    Max,
    Percent
}

