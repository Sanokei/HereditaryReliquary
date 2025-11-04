using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Get Stat node - gets a stat value from an actor
/// </summary>
[System.Serializable]
public class GetStatNode : SkillGraphNode
{
    private const string ACTOR_INPUT = "actor_in";
    private const string STAT_OUTPUT = "stat_out";
    
    [SerializeField] private string statName = "Strength";
    
    public GetStatNode() : base()
    {
        SetTitle("Get Stat");
        SetDescription("Gets a stat value from an actor");
        statName = "Strength";
    }
    
    public GetStatNode(Vector2 pos) : base(pos)
    {
        SetTitle("Get Stat");
        SetDescription("Gets a stat value from an actor");
        statName = "Strength";
    }
    
    public string StatName
    {
        get => statName;
        set => statName = value;
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
        return new List<NodePin>
        {
            new NodePin(STAT_OUTPUT, "Stat Value", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Float)
        };
    }
}

