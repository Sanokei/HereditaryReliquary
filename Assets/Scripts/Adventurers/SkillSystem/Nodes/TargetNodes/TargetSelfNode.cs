using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Target Self node - targets the skill performer
/// </summary>
[System.Serializable]
public class TargetSelfNode : SkillGraphNode
{
    private const string ACTOR_OUTPUT = "actor_out";
    
    public TargetSelfNode() : base()
    {
        SetTitle("Target Self");
        SetDescription("Targets the skill performer");
    }
    
    public TargetSelfNode(Vector2 pos) : base(pos)
    {
        SetTitle("Target Self");
        SetDescription("Targets the skill performer");
    }
    
    public override List<NodePin> GetInputPins()
    {
        return new List<NodePin>();
    }
    
    public override List<NodePin> GetOutputPins()
    {
        return new List<NodePin>
        {
            new NodePin(ACTOR_OUTPUT, "Actor", PinType.Data, PinDirection.Output, NodePinHelper.PinDataTypes.Actor)
        };
    }
}

